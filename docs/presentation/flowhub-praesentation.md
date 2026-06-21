---
marp: true
theme: flowhub
paginate: true
footer: 'FlowHub · CAS AI-Assisted Software Engineering'
math: false
---

<!--
Sprechnotizen stehen in HTML-Kommentaren wie diesem und erscheinen im
Marp-Presenter-View (P drücken) bzw. im PDF-Notes-Export – nicht auf der Folie.
Zielzeit: ~5–6 Minuten — Stichworte sprechen, Notizen NICHT vorlesen.
Fokus liegt auf Teil 2 (Erfahrung · Harness · Learnings).
-->

<!-- _class: title -->
<!-- _paginate: false -->
<!-- _footer: '' -->

# FlowHub

## Eine KI-gestützte persönliche Inbox

**Andreas Imboden** · CAS AI-Assisted Software Engineering · FFHS FS26

*Das Projekt — und die Erfahrung, es mit KI zu bauen*

<!--
[~15 s] Begrüssung.
"FlowHub – mein CAS-Projekt. Kurz das WAS, dann ausführlich das interessantere WIE:
wie war es, das fast vollständig mit KI zu bauen?"
-->

---

## Das Problem — und die Idee

Der digitale Alltag produziert ständig Schnipsel: ein Film, ein Artikel, das Foto einer Quittung.

<div class="cols">
<div>

### Heute
Idee → welche App? → öffnen →
kategorisieren → ablegen
**5+ Schritte — oft vergessen**

</div>
<div>

### Mit FlowHub
Idee → **Telegram** → fertig
**1 Schritt — die KI übernimmt**

</div>
</div>

Ein **Telegram-Bot** als einziger Eingang: FlowHub **erkennt** den Input, **kategorisiert** ihn und **routet** ihn automatisch an den richtigen Self-Hosted-Service.

<!--
[~45 s] Problem + Lösung in einem.
"Jeder kennt das: schnell etwas merken wollen – aber welche App? Bis man entschieden hat,
ist der Gedanke weg. FlowHub dreht das um: ein einziger Eingang, ein Telegram-Bot. Ich
schicke etwas hin, die KI erkennt was es ist und legt es am richtigen Ort ab. Aus fünf
Schritten wird einer – und alles läuft self-hosted im eigenen Homelab, kein Cloud-SaaS."
-->

---

## Konkret: das Skill-System

Jeder Input wird einem **Skill** zugewiesen — Erkennung über Keywords,
URL-Muster und, wenn nötig, ein **LLM**.

| Input | Skill | landet in |
|---|---|---|
| heise.de-Artikel | ArticleSkill | **Wallabag** (read-later) ✅ |
| „The Imitation Game", share.google/… | MovieSkill | **Vikunja** (Watchlist) ✅ |
| nichts passt | GenericSkill | **Inbox** (PostgreSQL) ✅ |
| jellyfin.org „ausprobieren" | HomelabSkill | **Vikunja Kanban** · _geplant_ |
| Foto einer Quittung | DocumentSkill | **paperless-ngx** (DMS) · _geplant_ |

✅ heute live · _geplant_ = Roadmap (gleiche `ISkillIntegration`-Schnittstelle).

Unklar? → Der Bot **fragt mit 2–3 Optionen zurück** (Confidence-Score).

<!--
[~45 s] Ein, zwei Zeilen vorlesen, nicht alle.
"Ein heise-Artikel geht nach Wallabag, ein Filmtitel in die Vikunja-Watchlist – beides
läuft heute. Kanban und Dokumentenmanagement sind über dieselbe Skill-Schnittstelle
angelegt, das ist die Roadmap. Ist die KI unsicher, fragt der Bot kurz nach – das ist
der Confidence-Score in Aktion."
-->

---

<!-- _class: dense -->

## Unter der Haube: **Klassifikation → Anreicherung → Routing**

Zwei entkoppelte MassTransit-Consumer (ADR 0003), verbunden über Events:

```text
① CaptureCreated ─▶ CaptureEnrichmentConsumer
   • IClassifier → AiClassifier ── LLM, structured JSON ──┐ Fallback ↘ KeywordClassifier
   • Ergebnis: matched_skill · project · title · entities · tags
   • EnricherDispatcher → IEnricher[project]
        └ ZitateEnricher ── 2. LLM-Call: kurze Autor-Info
② CaptureClassified ─▶ SkillRoutingConsumer
   • ISkillIntegration[Name] → VikunjaSkillIntegration
        └ PUT /api/v1/projects/{Zitate}/tasks  →  ③ Completed (= Vikunja-Task-ID)
```

Stufe 1 klassifiziert (LLM, mit Keyword-Fallback) **und** reichert an; Stufe 2 routet zum Ziel-Dienst. Enricher leben in `FlowHub.AI/Enrichers` — **pro Bucket eine `IEnricher`-Klasse**, nur für Vikunja. **Zwei System-Prompts** ans LLM.

<!--
[~50 s] Der technische Kern in einem Bild.
"Zwei Stufen, über Events entkoppelt. Stufe 1 klassifiziert — die KI gibt strukturiertes
JSON zurück; fällt sie aus, übernimmt deterministisch der Keyword-Classifier, ein Capture
bleibt nie liegen. Nur Vikunja-Captures laufen durch einen bucket-spezifischen Enricher —
für Zitate ein zweiter LLM-Call. Stufe 2 löst die Skill-Integration per Name auf und schreibt
in Vikunja. Zwei getrennte System-Prompts — Klassifizieren und Anreichern, gleich im Wortlaut."
-->

---

## Beispiel: Capture ist **nur das Zitat**

<div class="cols">
<div>

**Eingang & Verarbeitung**

Telegram-Capture:
```text
Talk is cheap. Show me the code.
```

→ **AiClassifier** (LLM) erkennt das Zitat:
&nbsp;&nbsp;&nbsp;`matched_skill: Vikunja` · `project: Zitate`
&nbsp;&nbsp;&nbsp;`entities { quote, author: "Linus Torvalds" }`
→ **ZitateEnricher** (2. LLM-Call) → kurze **Autor-Info**
→ **VikunjaSkillIntegration** → `PUT …/tasks` → **Completed**

</div>
<div>

**Ergebnis — Vikunja-Task im Projekt „Zitate"**

> "Talk is cheap. Show me the code." — Linus Torvalds
>
> About Linus Torvalds: *<2–4-Satz-Info vom Modell>*

Den **Autor** liefert schon die Klassifikation (Modell-Wissen); die **Autor-Info** ergänzt der Enricher mit *„Never invent facts."* als Halluzinations-Stopper.

</div>
</div>

<!--
[~40 s] Ein Capture durch die ganze Kette.
"Der Capture ist nur das Zitat — kein Autor im Text. Der Klassifikator erkennt den berühmten
Spruch und füllt Autor und Projekt selbst. Der Enricher holt dann die kurze Autor-Info und
schreibt alles als Beschreibung in den Vikunja-Task im Projekt Zitate."
-->

---

<!-- _class: dense -->

## System-Prompt ① — **Klassifikation** (Zitat)

`AiClassifier` → `IChatClient.GetResponseAsync<AiClassificationResponse>` · *role: system* (`AiPrompts`)

<div class="small">

```text
You classify user-captured snippets for a personal knowledge tool called FlowHub.

For each capture, return:
- tags: 1–5 short lowercase tags describing the snippet
- matched_skill: which downstream skill should handle it. Choose exactly ONE:
    "Wallabag"  – the snippet is a URL or article worth saving for later reading
    "Vikunja"   – the snippet is a task, todo, OR a structured piece of content
                  that belongs in a Vikunja project (quote, movie, book, …)
    ""          – none of the above; it will be marked as Orphan
- project: when matched_skill="Vikunja", pick the best matching project from
  this list. If unsure, pick "Inbox".
    Available: Inbox, Zitate
  Leave empty otherwise.
- title: a 3–8 word title summarising the snippet (omit only if the snippet
         is itself shorter than 8 words)
- entities: optional structured fields the project may use, e.g.
    Zitate → {"quote": "...", "author": "..."}
    Movies → {"title": "...", "year": "..."}
  Omit if nothing applies.

Reply ONLY via the structured response schema. Never include explanations.
```

</div>

*role: user* → `Talk is cheap. Show me the code.` &nbsp;·&nbsp; <span class="small">Der Capture ist **nur das Zitat** — den `author` füllt das Modell aus eigenem Wissen.</span>

<!--
[~45 s] Was wirklich ans Modell geht — Wortlaut aus dem Code.
"Der System-Prompt definiert Rolle, die drei Skill-Optionen und das Ausgabeschema inklusive
der entities für Zitate. Die Projektliste 'Available:' wird zur Laufzeit aus den Vikunja-
Projekten gefüllt — in der Demo nur Inbox und Zitate. Die User-Nachricht ist hier nur das
nackte Zitat; den Autor ergänzt das Modell aus eigenem Wissen in die entities. Antwort
ausschliesslich über das strukturierte Schema, nie in Prosa."
-->

---

<!-- _class: dense -->

## System-Prompt ② — **Anreicherung** (Zitat)

`ZitateEnricher.FetchBioAsync` → zweiter LLM-Call · *role: system* (`ZitateEnricherPrompts`) · `Temperature 0.2` · `MaxOutputTokens 280`

```text
You enrich a quotation for a personal knowledge tool. You are given an author and their
quote. Write 2–4 factual sentences covering: who the author is (full name, life dates if
known, nationality, and their role or field), and — ONLY if you genuinely know it — roughly
when or in what context the quote was said or written (a year, decade, or occasion). If you
do not recognise the author, reply with an empty string. Never invent facts, dates, or
attributions.
```

*role: user* →
```text
Author: Linus Torvalds
Quote: "Talk is cheap. Show me the code."
```

<div class="small">

**Warum ein eigener Prompt?** Andere Aufgabe als Klassifikation → eigener, fokussierter Prompt. Input: nur `author` + `quote` aus den `entities` (der **Autor** wurde schon bei der Klassifikation erkannt; hier kommt die **Autor-Info** dazu). *„Never invent facts"* + Leerstring-bei-Unbekannt = **Halluzinations-Stopper**.

**Warum `Temperature 0.2`?** Niedrige Temperatur = wenig Zufall bei der Token-Wahl → **faktentreue, reproduzierbare** Antworten statt „kreativer" Variation — genau richtig für eine Autoren-Bio (zusammen mit *„Never invent facts"*). `MaxOutputTokens 280` deckelt die Länge.

</div>

<!--
[~40 s] Der zweite Prompt — bewusst getrennt.
"Anreicherung ist eine andere Aufgabe, also ein eigener, knapper System-Prompt. Er sieht nur
Autor und Zitat, nicht den ganzen Capture. Entscheidend ist die letzte Zeile: 'Never invent
facts' und bei Unbekannten ein Leerstring — das ist die Halluzinations-Stopper. Das Resultat
wird die Beschreibung des Vikunja-Tasks im Projekt Zitate."
-->

---

## Wiederfinden: semantische Suche

Nicht nur reinwerfen — auch **per Bedeutung wiederfinden**, nicht per Stichwort.

| Schritt | Was passiert |
|---|---|
| Query „alles zu Docker" | → Embedding (Mistral `mistral-embed`, 1024 Dim.) |
| pgvector-Suche | HNSW · Cosine-Distanz · Sub-ms bei < 1 Mio. Zeilen |
| Treffer | inhaltlich ähnliche Captures — auch ohne exaktes Wort |

**HNSW** = approximativer **Nächste-Nachbarn-Index** (sub-linear schnell) · **Cosine-Distanz** = inhaltliche Ähnlichkeit über den **Winkel zwischen den Vektoren**

`GET /api/v1/captures/search?q=…` · Provider per Config tauschbar (OpenAI-kompatibel) · ohne Key → sauberes `503`

<!--
[~35 s] Die zweite Hälfte der Idee: Reinwerfen ist nichts wert ohne Wiederfinden.
Die Suche geht über die Bedeutung, nicht das exakte Wort: Anfrage wird in dasselbe
Embedding übersetzt wie die Captures – Mistral, 1024 Dimensionen – und pgvector findet
per Cosine-Distanz die inhaltlich nächsten Treffer in Sub-Millisekunden. Provider per
Config tauschbar; ohne Key liefert die API sauber ein 503 statt zu raten.
-->

---

## Tech-Stack

| Schicht | Technologie |
|---|---|
| Backend | **.NET 10** / C# / ASP.NET Core (LTS) |
| Web-UI | **Blazor SSR** — .NET-native, kein JS-Framework |
| KI-Integration | **Microsoft.Extensions.AI** + Ollama (lokal) / Anthropic · OpenRouter (Fallback) |
| Pipeline | **MassTransit** — In-Memory (dev) / RabbitMQ (prod) |
| Persistenz | **PostgreSQL 17** + **pgvector** · EF Core 10 |
| Deployment | **Docker Compose** — 6 Services, Migrations als Init-Container |

*Inkrementell über 5 Blöcke gebaut: Konzept → UI → Services/KI → Persistenz → Deployment.*

<!--
[~30 s] Stack schnell, nicht vorlesen. Highlights setzen.
"Durchgehend .NET 10, Frontend Blazor – kein separates JS-Framework. KI über
Microsoft.Extensions.AI, Provider per Config umschaltbar. Persistenz Postgres mit
pgvector. Alles in Docker Compose, inkrementell über fünf Blöcke gebaut."
-->

---

<!-- _class: dense -->

## Genutzte **externe Services**

**🔴 Live-Demo:** **`https://demo.flowhub.freaxnx01.ch`**

<div class="cols">
<div style="flex: 3">

| Service | Rolle in FlowHub | Web |
|---|---|---|
| **Telegram** | Eingangskanal (Bot) | `telegram.org` |
| **Vikunja** | To-do / Projekte (Inbox, Zitate …) | `vikunja.io` |
| **Wallabag** | Read-Later für Artikel / URLs | `wallabag.org` |
| **paperless-ngx** | Dokumenten-Management (DMS) | `docs.paperless-ngx.com` |
| **OpenRouter** | LLM-Gateway — Klassifikation (Gemma) | `openrouter.ai` |
| **Mistral** | Embeddings (1024-dim) → Suche | `mistral.ai` |

</div>
<div style="flex: 1; text-align: center;">

![w:150](assets/demo-qr.png)

</div>
</div>

<span class="small">Cloud-LLM-Adapter: **Anthropic** (Claude) · Hosting-Policy ADR 0007 (Default lokal **Ollama** geplant) · Persistenz **PostgreSQL + pgvector**.</span>

<!--
[~35 s] Was FlowHub draussen anbindet — nicht alle vorlesen.
"FlowHub ist ein Hub: rein kommt's über Telegram, raus geht's je nach Skill an Vikunja,
Wallabag oder paperless-ngx — alles self-hosted im Homelab. Die KI läuft in der Demo über
OpenRouter und Mistral für die Embeddings. Anthropic ist als Alternativ-Adapter da; lokales
Ollama ist die geplante Default-Hosting-Variante."
-->

---

## Betrieb: Monitoring & selbst heilen

| Was | Wie |
|---|---|
| Metriken | OpenTelemetry → **Prometheus** (`/metrics`) → **Grafana** |
| Health | `/health/live` · In-App-Integration-Health (Vikunja/Wallabag/Paperless) |
| Demo-Status | öffentliche **Uptime-Kuma**-Statusseite (`status.demo.flowhub…`) — prüft auch LLM-Erreichbarkeit |
| Self-Healing | Container-`restart`-Policies + Healthchecks · KeywordClassifier-Fallback bei LLM-Ausfall |

<!--
[~30 s] Monitoring ist nicht nachträglich angeklebt: Metriken via OpenTelemetry,
Prometheus + Grafana im Stack. Die öffentliche Demo hat eine Uptime-Kuma-Statusseite,
die auch die LLM-Erreichbarkeit prüft. Fällt das LLM aus, greift der KeywordClassifier —
die App bleibt funktionsfähig. Container heilen sich per restart-Policy selbst.
-->

---

<!-- _class: divider -->
<!-- _paginate: true -->

# Teil 2: Bauen mit KI

## Erfahrung · Harness · Learnings

<!--
[~10 s] Übergang. Tempo wechseln.
"Soviel zum Produkt. Jetzt der Teil, um den es im CAS eigentlich geht: Wie war es, das
mit KI zu bauen? Werkzeuge, Disziplin und die Learnings."
-->

---

<!-- _class: dense -->

## Der Harness — Überblick

Die KI wurde nicht ad-hoc geprompted, sondern über einen **Tool-Workflow** gesteuert:

| Ebene | Werkzeug |
|---|---|
| **Agent** | Claude Code (interaktiv) · Codex / Copilot ergänzend |
| **Konventionen** | **`ai-instructions`** (base + `dotnet-blazor`) → `CLAUDE.md` |
| **Workflows** | **eigene Skills**: `/ui-*`, `/flowhub-*`, `/commit`, `/push` |
| **Methode** | **Brainstorm → Spec → Plan → Subagent → Review** |
| **Automatisierung** | **`agent-pipeline`** (Issue→PR) · **`examiner-sim`** (Grading) |
| **Disziplin** | **Context-Hygiene**: Logs-via-File · `/clear`-Schnitte |

<!--
[~30 s] Landkarte für Teil 2, nicht vorlesen.
"Der ganze Harness auf einen Blick – von oben: der Agent, die Konventionen, eigene
Workflows als Skills, die Arbeitsmethode, zwei Automatisierungen, und unten die Disziplin,
die alles zusammenhält. Die nächsten Folien gehen die wichtigsten durch."
-->

---

## Nicht „Prompt rein, Code raus" — eine **Pipeline**

Jeder grössere Baustein lief durch denselben strukturierten Ablauf:

**Brainstorm → Spec → Plan → Subagent-Implementierung → 2-stufiges Review**

1. **Brainstorming** — Design als **A/B/C-Entscheidungen** (z. B. 13 Entscheide für die Async-Pipeline), jede mit Begründung
2. **Spec + Plan** — schriftliches Design, dann **TDD-geordneter** Aufgabenplan
3. **Subagenten** — pro Task ein **frischer Kontext** (Test-First)
4. **Review ×2** — Spec-Konformität, dann Code-Qualität — *bevor* etwas in **`main`** geht

<!--
[~45 s] Der wichtigste konzeptionelle Punkt.
"Der grösste Lerneffekt: Gute KI-Entwicklung ist NICHT 'Prompt rein, Code raus' – es ist
eine Pipeline. Erst Design als A/B/C-Entscheidungen, dann Spec, dann ein Plan in
test-first-Reihenfolge. Erst dann implementiert ein Subagent mit frischem Kontext. Und
nichts geht in main ohne zwei Reviews. Diese Struktur hält die KI auf Kurs."
-->

---

## Werkzeuge: **`ai-instructions`** + eigene **Skills**

**`ai-instructions`** (eigenes Repo) — Konventionen als **feste Regeln**, nicht als Prompt:

- `base` (stack-agnostisch) **+ Stack-Overlay `dotnet-blazor`** → daraus leitet sich `CLAUDE.md` ab
- z. B. **Coding Guidelines** (Clean Code) · **SemVer** · **Conventional Commits** · **12-Factor** (Prinzipien für Cloud-native Apps) · **TDD** — *Tests werden nie nachträglich angepasst, nur damit Code grün wird*

**Eigene Skills** (Claude-Code-Slash-Commands):

- `/ui-brainstorm · /ui-flow · /ui-build · /ui-review` — der **4-Phasen-UI-Workflow**
- `/flowhub-capture · -triage · -issue` — das Produkt selbst bedienen
- `examiner-sim` · `cas-aise-grade-self-check` — **Selbstbewertung** gegen die Moodle-Bewertungskriterien

<!--
[~40 s] Steuerung statt Zuruf.
"Damit die KI nicht bei null anfängt: ein eigenes ai-instructions-Repo – stack-agnostischer
Kern plus .NET-Blazor-Overlay mit Coding Guidelines, SemVer, Conventional Commits, TDD als
festen Regeln, aus denen sich das CLAUDE.md ableitet. Plus eigene Skills als Slash-Commands:
der UI-Workflow, Commands fürs Produkt, und ein Skill zur Selbstbewertung gegen die Bewertungskriterien."
-->

---

## Obsidian-Vault als **2nd Brain**

`vault/` — reine **Markdown**-Dateien als gemeinsamer Wissensspeicher von Mensch **und** Agent:

- Der Agent **liest** Kontext — CAS-Scope, Block-Inhalte, Entscheidungen
- … und **schreibt** zurück — Nachbereitungen, Knowledge-Notizen, Learnings
- **Markdown**: menschen- *und* LLM-lesbar, git-/diff-fähig — keine Export-/API-Schicht
- Konventionen in `vault/CLAUDE.md` — Tags (`claude-generated`/`-updated`), Auto-Commit

<!--
[~30 s] Das zweite Gehirn.
"Der Obsidian-Vault ist reines Markdown – gleichzeitig für mich und für die KI lesbar.
Der Agent zieht sich daraus den Kontext (CAS-Stoff, Entscheidungen) und schreibt selbst
wieder rein: Block-Nachbereitungen, Notizen, Learnings. Mensch und Agent teilen sich
dieselbe Wissensbasis – kein Export, keine API dazwischen, alles versioniert in git."
-->

---

## Beispiel: der **UI-Workflow**

`/ui-brainstorm` → **ASCII-Wireframe** → `/ui-flow` → **Mermaid-Flow** → `/ui-build` → `/ui-review`
**Gate pro Phase** — nichts wird gebaut, bevor Wireframe **und** Flow freigegeben sind.

**Phase 1 — Wireframe** (`New Capture`):

```
┌─ FlowHub ─────────────────────┐
│  New Capture                  │
│  ┌─ Content * ─────────────┐  │
│  │ URL / Zitat / Text…     │  │
│  └─────────────────────────┘  │
│  Skill: [ — KI entscheidet ▾ ]│
│        [Abbrechen] [Speichern] │
└───────────────────────────────┘
```

**Phase 2 — Mermaid-Flow** → echtes `docs/design/new-capture/flow.md`

![bg right:40% h:300](assets/ui-flow-example.svg)

<!--
[~40 s] Konkret zeigen, nicht abstrakt behaupten.
"Vier Phasen, jede mit einem Gate. Phase 1 zwingt mich, das Layout erst als ASCII-Wireframe
zu klären – links. Phase 2 macht den Zustandsfluss explizit als Mermaid-Diagramm – rechts.
Beides muss freigegeben sein, bevor eine Zeile Blazor entsteht. So baut die KI nicht am
Ziel vorbei, und ich denke das UI durch, bevor Code existiert."
-->

---

## Context-Hygiene

Der Kontext ist das **knappste Gut**. Zwei Disziplinen brachten am meisten:

**1 · Logs via File** — grösster Token-Fresser waren Console-, Test- und Build-Streams.
Statt alles in den Chat: in eine **Datei** schreiben, gezielt mit `Read offset/limit` oder
`grep` holen. → **~5–10× weniger Tokens** pro Debug-Session.

**2 · `/clear`-Schnitte** — Spec → `/clear` → Plan → `/clear` → Implement.
Jede Phase hinterlässt ein **Artefakt auf Disk**; der Dialog-Ballast wird verworfen.

> Was zwischen Phasen weiterleben muss, gehört in eine **Datei** — nicht in den Chat.

<!--
[~45 s] Das praktischste Learning – ruhig betonen.
"Das am meisten unterschätzte Thema: Context-Management. Grösster Token-Fresser war
Log-Output. Lösung: erst in eine Datei, dann gezielt nur relevante Zeilen lesen – fünf-
bis zehnmal weniger Tokens pro Debug-Session. Zweitens: zwischen Spec, Plan und
Implementierung ein hartes /clear. Jede Phase hinterlässt ein Artefakt auf der Disk.
Faustregel: Was weiterleben muss, gehört in eine Datei – nicht in den Chat."
-->

---

## Automatisierung — **KI prüft KI**

**`cas-aise-submission-preflight`** (Multi-Agent-Check) — **Dry-Run der Moodle-Abgabe**:
baut das Bundle, prüft alle Verweise, scannt auf Leaks → **Go/No-Go** vor dem Upload.

**`examiner-sim`** (Multi-Agent-Workflow) — baut die Abgabe-PDFs, **benotet** sie
gegen die **Moodle-Bewertungskriterien** mit einem Agenten-Panel und bedient die Live-Demo.

<!--
[~35 s] Die Meta-Ebene: KI prüft KI.
"Zwei KI-gestützte Prüfungen: ein Preflight-Check, der die Moodle-Abgabe als Dry-Run baut,
alle Verweise prüft und ein Go/No-Go gibt; und ein examiner-sim, der die Abgabe-PDFs baut,
gegen die Moodle-Bewertungskriterien benotet und die Demo bedient. Der Mensch definiert die Leitplanken;
KI-gestützte Prüfungen finden, was die KI selbst übersieht."
-->

---

<!-- _class: dense -->

## `agent-pipeline` — autonome **Issue → PR**

Wiederverwendbarer GitHub-Actions-Workflow (`freaxnx01/agent-pipeline`); der lokale `.github/workflows/claude.yml` reicht die Arbeit nur an die Pipeline weiter.

```text
Issue + Label "ai-implement"
   -> claude.yml  ->  agent-pipeline/claude-implement.yml@main
        -> Branch · commit · Claude implementiert · Draft-PR
            -> Issue-Kommentar: Run-URL · PR-Link · Retry-Hinweise
```

<div class="cols small">
<div>

**Auslösen**
- Issue mit **`ai-implement`** labeln; oder manuell *Actions → Run workflow*.
- `ubuntu-latest`, Timeout 60 min.

**Retry-Policy**
- `attempt`-Zähler, Reruns gedeckelt.
- Rate-Limit erreicht → **automatischer** Retry.
- `max-turns`-Erschöpfung → Kommentar im Issue, **Mensch** entscheidet.

</div>
<div>

**LLM-Run (Claude)**
- Auth via `CLAUDE_CODE_OAUTH_TOKEN`.
- Budget über **`max-turns`**.
- Claude-Code-Action meldet je Lauf **Modell + Token-Verbrauch** (Input/Output).

</div>
</div>

<!--
[~40 s] Die Pipeline einmal ganz.
"Ein gelabeltes Issue startet einen GitHub-Actions-Workflow. Der lokale Stub reicht nur an
die wiederverwendbare Pipeline weiter; die branched, committed, lässt Claude implementieren
und öffnet einen Draft-PR. Alles Wichtige landet als Kommentar am Issue. Retries laufen
automatisch bei Rate-Limits, nur die max-turns-Erschöpfung gibt zurück an den Menschen."
-->

---

## Wo die KI **glänzt** — und wo **nicht**

Über alle Blöcke: **100 % des Codes KI-generiert** — ~5 % brauchten menschliche Nacharbeit.

### Glänzt — **repetitiv & gut spezifiziert**
7 `IEntityTypeConfiguration<T>`, EF-Migrations, CI-YAML;
**16 Integrationstests** gegen echtes PostgreSQL — **alle grün beim ersten Lauf**.

### Scheitert — wo **Domäne & Performance** zählen
- **N+1-Blindheit** — `ListAsync` ohne `.Include`
- **CASCADE überall** — Löschen kaskadiert blind (Eltern weg → alle Kinder weg). Was *erhalten* bleiben muss (z. B. Audit-Trail), ist eine **menschliche Entscheidung**
- **Veraltete Versionen** — Trainingsdaten hinken neuen Releases hinterher
- **Feature-Drift** — Scope-Disziplin muss vom **Menschen** kommen

<!--
[~45 s] Ehrlich und konkret – überzeugt die Dozenten.
"Praktisch der gesamte Code kam von der KI – aber rund 5 Prozent brauchten menschliche
Nacharbeit. Wo sie glänzt: alles Repetitive und gut Spezifizierte – Konfigurationsklassen,
Migrations, 16 Integrationstests gegen echtes Postgres, alle grün beim ersten Lauf. Wo sie
scheitert: N+1-Abfragen, blind gesetzte CASCADE-Löschungen, veraltete Versionen, ständiger
Feature-Drang."
-->

---

## Der **Smoke-Test-Moment**

Die KI schrieb den ganzen Deployment-Stack. Dann lief **ein** Befehl:
`make smoke-prod` — End-to-End-Probe des laufenden Stacks.

**Folgende Bugs wurden gefunden:**

- `.editorconfig` fehlt im Build-Image → Build bricht mit `TreatWarningsAsErrors` ab
- Compose-Env-Casing `${EMBEDDINGS__APIKEY}` ≠ `Embeddings__ApiKey` → Embeddings still no-op
- Leerstring-Modellname → `AssertNotNullOrEmpty`-Crash beim Start
- Mistral lehnt das `dimensions`-Feld ab → 422

**¡AI, caramba!**

> KI schrieb den Code. Eine **KI-gestützte Prüfung** fand, was die KI übersah.
> **Der Mensch bleibt im Loop — als Reviewer.**

<!--
[~45 s] Die beste Geschichte – mit etwas Spannung.
"Mein liebster Moment: Die KI hatte den kompletten Deployment-Stack geschrieben, sah gut
aus. Dann ein – auch KI-geschriebener – Smoke-Test, der den echten Stack hochfährt. Erster
Lauf: mehrere latente Bugs, die die Abgabe blockiert hätten. Lektion: KI schreibt den Code,
aber eine – idealerweise KI-gestützte – Prüfung muss finden, was die KI übersieht."
-->

---

<!-- _class: lead -->

## Fazit

**KI ist ein starker Accelerator für Coding** —
Logik, Boilerplate, EF-Migrations, Tests entstehen in Minuten.

**Sie braucht menschliche Führung bei Architektur & Domäne** —
FK-Strategie, Performance, Scope.

### Der Mensch bleibt **Architekt und Reviewer**.

<span class="small">Code & Doku: github.com/freaxnx01/FlowHub-CAS-AISE · Danke — Fragen?</span>

![w:130](assets/repo-qr.png)

<!--
[~30 s] Klar landen, Q&A öffnen.
"Mein Fazit: KI ist ein starker Beschleuniger für Infrastruktur-Code, braucht aber
menschliche Führung bei allem, was Architektur und Domäne berührt. Die Rolle hat sich
verschoben – vom Tippen zum Entwerfen, Lenken, Reviewen. Der Mensch bleibt Architekt und
Reviewer. Danke – Fragen?"
-->
