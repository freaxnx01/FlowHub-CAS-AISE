# ADR 0007 — LLM-Hosting: Lokal (Ollama) vs. Cloud (Anthropic/OpenRouter)

- **Status:** Accepted (target) — **not yet implemented as the default; see "As built" note**
- **Date:** 2026-05-24
- **Block:** Block 5 (Deployment) — Nachbereitung
- **Decider:** freax
- **Affects:** `source/FlowHub.AI/`, `source/FlowHub.Web/appsettings.json`, `docker-compose.yml`, `docs/design/data-flow.md`, `docs/spec/nfa.md` (NfA-P1)

---

## Context

ADR 0004 hat zwei Cloud-Adapter (Anthropic, OpenRouter) hinter `Microsoft.Extensions.AI.IChatClient`
verankert. Beide Adapter senden Capture-Bodies an Drittanbieter ausserhalb der Schweiz/EU
und ausserhalb der vom Betreiber kontrollierten Homelab-Hardware. NfA-P1 (`docs/spec/nfa.md`)
verlangt, dass Capture-Inhalte per Default das Homelab nicht verlassen, weil sonst die
GDPR-Haushaltsausnahme + CH-revDSG-Privatpersonen-Ausnahme nicht mehr trägt und FlowHub in
Auftragsverarbeitungs-Pflichten rutscht (DPA, SCCs / CH-US-DPF, Provider-Compliance-Review).

Das `docs/design/data-flow.md` Trust-Boundary-Diagramm zeigt den Cloud-Pfad explizit als
gestrichelte Opt-in-Linie. Diese ADR fixiert das **Default-Verhalten** und den **Opt-in-
Mechanismus**, damit der Cloud-Pfad nicht versehentlich aktiv wird (Konfigurations-Drift,
Copy-Paste aus Beispielen, Cloud-Default in einer Library-Aktualisierung).

Der AI Act Art. 50 Transparenz-Aspekt ist in **NfA-P2 / ADR-0004 §5** abgedeckt
(`ClassificationSource = "AI"` + UI-Badge); diese ADR betrifft ausschliesslich die
Hosting-Frage.

---

## Decision

> **As built (Block 5).** This ADR records the *target* hosting policy; it is **not
> the state shipped for the CAS submission**. The implemented provider abstraction
> exposes `{ Anthropic, OpenRouter }` (cloud) — there is **no** `Local`/Ollama
> adapter yet, and the public demo runs classification on OpenRouter (Gemma) and
> embeddings on Mistral. So today capture content **does** leave the homelab for AI
> inference, the opposite of the local-by-default goal below. Local Ollama hosting
> and the matching `OutboundCallAuditTests` (see NfA-P1) are **planned, not
> implemented**. The decisions below are the intended design, retained as the
> roadmap; NfA-P1 has been relabelled to reflect this honestly.

### 1. Default-Provider = lokales Ollama (`Embeddings__Provider=Local`)

Eine frische Installation ohne weitere Konfiguration MUSS Capture-Bodies ausschliesslich
an `http://ollama:11434` (Docker-Compose-internes Netz) senden. Kein Outbound aus dem
Homelab. `appsettings.json` setzt `Embeddings:Provider=Local` als gespeicherten Wert;
`docker-compose.yml` startet einen `ollama`-Service mit gemountetem Model-Cache als
Pflicht-Dependency von `flowhub.web`.

Default-Modell: `llama3.1:8b-instruct-q4_K_M` (passt in 8 GB RAM, akzeptable
Klassifikations-Qualität auf der KeywordClassifier+AI-Schema-Last). Konfigurierbar via
`Embeddings__Local__Model`.

### 2. Cloud-Provider = explizites Opt-in via Environment-Variable

Aktivierung des Cloud-Pfads erfordert *beide*:

```
Embeddings__Provider=Anthropic|OpenRouter
Embeddings__ApiKey=<secret>
```

`AddFlowHubAi(IConfiguration)` validiert beim Boot:
- `Embeddings__Provider ∈ {"Local","Anthropic","OpenRouter"}` — sonst
  `InvalidOperationException` (fail fast).
- Bei `Provider != "Local"` muss `ApiKey` gesetzt und nicht-leer sein — sonst fällt
  der Boot mit `EventId 3022 AiProviderApiKeyMissing` und `InvalidOperationException`.

Keine implizite Cloud-Aktivierung über "Es ist halt ein Key in der Env" — der Provider-
Wert ist der alleinige Schalter. Verhindert, dass eine vergessene `ANTHROPIC_API_KEY`
auf dem Host versehentlich den Cloud-Pfad einschaltet.

### 3. Boot-Log dokumentiert die Hosting-Entscheidung sichtbar

`AiBootLogger` (`IHostedService` aus ADR 0004 §8) emittiert beim Start einen der
folgenden EventIds:

- `3020 AiProviderRegistered` (Information) — `Provider`, `Model`, `Endpoint`
- `3022 AiProviderApiKeyMissing` (Error, blockiert Boot)
- `3023 AiProviderIsCloud` (Warning) — wird *zusätzlich* zu 3020 emittiert, wenn
  `Provider != "Local"`. Beinhaltet den expliziten Hinweis: *"Capture content
  leaves the Homelab trust boundary. Ensure DPA + SCCs / CH-US-DPF are in
  place."*

Die Warnung im Log ist der bewusste "Stolperdraht" für den Betreiber.

### 4. Outbound-Audit-Test verriegelt den Default

> **Status: geplant, nicht implementiert.** Der hier beschriebene Test existiert
> noch nicht in `tests/` (siehe „As built"-Notiz oben und NfA-P1). Der folgende
> Absatz beschreibt das Soll-Design.

`tests/FlowHub.Web.IntegrationTests/OutboundCallAuditTests.cs` (Block 5) instanziiert
die App mit der Default-Konfiguration und prüft via einem `DelegatingHandler`, dass
während eines vollen Capture-Klassifikations-Zyklus kein HTTP-Request an
`*.openai.com`, `*.anthropic.com`, `openrouter.ai`, oder `api.cohere.ai` abgeht. Der
Test ist verlässlicher als die Konfigurations-Aussage, weil er die *tatsächlich
aufgebaute* DI-Pipeline beobachtet.

### 5. Cloud-Pfad behält den Fallback-Vertrag aus ADR 0004 §5

Wenn der Cloud-Provider aktiv ist und der Call fehlschlägt (Netz, Timeout, Auth,
Schema), fällt `AiClassifier` auf `KeywordClassifier` zurück — das verlässt das Homelab
nicht. Datenschutz-relevante Folge: ein längerer Cloud-Outage senkt nicht nur die
Klassifikations-Qualität, sondern reduziert auch das Datenexport-Volumen automatisch.

### 6. Embeddings-Generator: separate Entscheidung

Anthropic bietet keine Embeddings. Falls Block-5-Vektor-Suche (ADR 0006) aktiviert wird
*und* der Operator den Cloud-Pfad gewählt hat, müsste der `IEmbeddingGenerator`
separat konfiguriert werden (OpenRouter, OpenAI native, oder eine self-hosted
sentence-transformer-Facade). Diese ADR legt fest: **embeddings folgen demselben
Lokal-First-Default**. Wenn der Operator Vector-Search auf einem Cloud-Backend will,
ist das eine zweite explizite Konfigurations-Entscheidung — kein automatisches
Mit-Aktivieren über den Klassifikator-Provider.

### 7. Modell-Updates bleiben Operator-Aufgabe

Lokale Ollama-Modelle werden nicht automatisch aktualisiert. `docker-compose.yml`
pinnt `ollama/ollama:0.x.y` (konkrete Version, kein `:latest`), und das Modell-Pull
ist explizit (`docker compose exec ollama ollama pull llama3.1:8b-instruct-q4_K_M`).
Begründung: ein Auto-Pull beim Boot würde unkontrollierte Outbound-Calls an
`ollama.com` / Hugging Face erzeugen — der ganze Punkt der lokalen Hostung wäre damit
unterlaufen.

---

## Consequences

### Rubric coverage

- **Spezifikation: NfA (SMART)** (max 5) — direkter Nachweis für NfA-P1.
- **Entwurf: Lösungsansatz und Architektur** (max 7) — Verweis auf `docs/design/data-flow.md`.
- **KI: Erfahrungen reflektiert** (max 7) — Lokal-vs-Cloud-Trade-off ist eine der zwei
  wesentlichen Reflexions-Achsen in der KI-Reflexion (Klassifikations-Qualität ↔
  Datenschutz/Compliance-Aufwand).
- **Sub-Systeme als Container** (max 5) — `ollama` als separater Compose-Service.

### Trade-offs

| Aspekt | Lokal (Default) | Cloud (Opt-in) |
|---|---|---|
| Klassifikations-Qualität | Llama 3.1 8B — ausreichend für Tag + MatchedSkill | Haiku 4.5 / Llama 70B — bessere Schema-Adherence, robustere Titles |
| Latenz | 1–4 s auf CPU; deutlich schneller auf GPU | 200–800 ms |
| Datenschutz | Capture-Body verlässt Homelab nicht | DPA + SCCs / CH-US-DPF erforderlich |
| Betriebsaufwand | Modell-Pull + RAM-Budget + ggf. GPU | API-Key + Rechnungs-Monitoring |
| AI-Act-Position | unverändert minimal risk | unverändert minimal risk (Art. 50 betrifft Transparenz, nicht Hosting) |

### Failure modes neu eingeführt

- **Ollama nicht erreichbar:** `AiClassifier` fällt auf `KeywordClassifier` zurück
  (selber Vertrag wie bei Cloud-Failure). Compose-Healthcheck auf `ollama`-Service
  verzögert den `flowhub.web`-Start, bis Ollama bereit ist.
- **Modell nicht gepullt:** erster Klassifikations-Call schlägt fehl; Fallback greift.
  Boot-Log `3024 AiLocalModelMissing` (Warning) macht das im Operator-Workflow
  sichtbar.

### Was diese ADR NICHT entscheidet

- **Welches Cloud-LLM** (Anthropic vs. OpenRouter) — bleibt bei ADR 0004 §6.
- **AI-Act-Transparenz** — bleibt bei NfA-P2 / ADR 0004 §5.
- **Vector-Search-Provider** — bleibt offen, siehe §6 + ADR 0006.

---

## References

- ADR 0004 — AI Integration in Services (Adapter-Shape, Fallback-Vertrag, EventId-Namespace)
- ADR 0006 — Vector Search (Embedding-Provider-Frage)
- NfA-P1 / NfA-P2: `docs/spec/nfa.md`
- Data-Flow-Diagramm: `docs/design/data-flow.md`
- Vault-Stub: `vault/Knowledge/Datenschutz-und-AI-Act.md`
- Outbound-Audit-Test: `tests/FlowHub.Web.IntegrationTests/OutboundCallAuditTests.cs` (Block 5)
- Ollama: https://ollama.com/
- AI Act Art. 50 (Transparenzpflicht für KI-Interaktion)
- CH-US Data Privacy Framework (Adequacy seit 2024)
