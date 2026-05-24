# ADR 0008 — Logging-Policy: kein PII / Capture-Body in Serilog-Output

- **Status:** Accepted
- **Date:** 2026-05-24
- **Block:** Block 5 (Deployment) — Nachbereitung
- **Decider:** freax
- **Affects:** alle `LoggerMessage`-Aufrufe in `source/FlowHub.*/`, `source/FlowHub.Web/Program.cs` (Serilog-Konfiguration), `tests/FlowHub.Web.IntegrationTests/SerilogPiiAuditTests.cs` (Block 5)

---

## Context

NfA-P1 fordert, dass Capture-Inhalte das Homelab nicht verlassen. Logs sind ein
Datenpfad, der diese Boundary umgehen kann — auch bei vollständig lokaler Hostung
landen Serilog-Outputs im Container-Stdout, werden vom Docker-Logging-Driver
abgegriffen und können in eine zentrale Log-Aggregation (Loki, ELK) wandern. Sobald
ein Cloud-Logging-Driver konfiguriert ist (z.B. ein Sidecar zu einem SaaS-Log-
Dienst), verlässt jeder PII-haltige Log-Eintrag die Trust-Boundary unbemerkt.

ADR 0004 hat die `LoggerMessage`-source-gen-Form mit explizit benannten Parametern
eingeführt (`EventId 3010 AiClassifierFellBackToKeyword`). Die Form ist bereits in
`FlowHub.AI`, `FlowHub.Skills`, `FlowHub.Web/Pipeline/` durchgängig im Einsatz —
diese ADR fixiert die *inhaltliche* Regel, welche Parameter überhaupt in eine
Log-Message dürfen.

Risiko-Tabelle in `vault/Knowledge/Datenschutz-und-AI-Act.md` Abschnitt 3.3 nennt
"Log-Inhalte (Serilog)" als eigene Kategorie mit `ADR-0008 Logging-Policy` als
verlangtem Nachweis.

---

## Decision

### 1. Verbotene Felder in Log-Messages

Folgende Capture-Felder DÜRFEN NICHT als `LoggerMessage`-Parameter erscheinen, weder
im strukturierten Property-Bag noch im Message-Template:

- `Capture.Body` (der eigentliche Capture-Inhalt)
- `Capture.Title` (KI-generiert oder manuell — kann PII enthalten)
- `Capture.SourceMetadata.SenderHandle` (Telegram), `SenderEmail` (Mail)
- Embedding-Vektoren (numerisch, aber re-identifizierbar)
- LLM-Prompts und LLM-Responses im Volltext
- API-Keys, Bearer-Tokens, `Embeddings__ApiKey`, `Vikunja__Token`

### 2. Erlaubte Felder

Logs DÜRFEN folgende Capture-bezogenen Werte enthalten:

- `CaptureId` (Guid — nicht-sprechend)
- `Stage` (enum: `Raw | Classified | Routed | Unhandled`)
- `ClassificationSource` (enum: `None | Heuristic | AI | Manual`)
- `MatchedSkill` (Wert aus dem geschlossenen Set `{"Wallabag", "Vikunja", ""}`)
- `ConfidenceScore` (numerisch)
- Aggregierte Zähler (`tag_count`, `body_length`)
- Zeit-/Dauer-Werte (`elapsed_ms`, `classified_at`)
- Provider-/Modell-Identifiers (`provider`, `model`)
- Exception-Type-Namen (`reason="HttpRequestException"`) — aber NICHT `Exception.Message`,
  weil deren Inhalt im Fall von Skill-/LLM-Exceptions Bodies oder Tokens echoen kann

### 3. `Exception.Message` wird strukturell behandelt

`LoggerMessage` mit `Exception ex`-Parameter ist erlaubt (Serilog rendert den Stack
strukturell). `ex.Message` als String-Parameter in eigene Felder einzubauen ist NICHT
erlaubt, weil:

- Skill-Adapter-Exceptions enthalten oft den HTTP-Response-Body im `Message` (z.B.
  Vikunja-API-Fehler-Payloads mit Task-Titles).
- LLM-Adapter-Exceptions enthalten oft den Prompt oder die Schema-Violation mit
  Original-Antwort.

Stattdessen: `reason: ex.GetType().Name` als String-Parameter, plus `ex` als
Exception-Parameter — Serilog logt den Stack, die `Message`-Property wird zwar im
strukturierten Ausgang sichtbar, aber sie wird nicht in eigene `properties` mit
sprechenden Keys eingewoben. Audit-Test (siehe §6) prüft auf
`reason in {"<known exception types>"}` als positive Liste, nicht auf
`Message`-Inhalte.

### 4. Serilog-Enricher als zweite Verteidigungslinie

`source/FlowHub.Web/Program.cs` registriert einen `PiiScrubbingEnricher`, der bei
jedem Log-Event die `LogEvent.Properties` durchgeht und Werte, deren Key in der
Block-Liste aus §1 steht oder deren String-Länge eine Schwelle (Default 512 Zeichen)
überschreitet, durch `"<redacted:length>"` ersetzt. Reine Defense-in-Depth — die
primäre Verteidigung bleibt §1 + Audit-Test.

Konfigurierbar via `Logging__PiiScrubber__MaxStringLength` (Default 512). Boot-Log
`EventId 9001 PiiScrubberRegistered` (Information).

### 5. Source-Generated `LoggerMessage` ist Pflicht

Direkte `_logger.LogInformation("Capture {Id} body={Body}", id, capture.Body)`-
Aufrufe sind verboten — sie umgehen die statische Analyse. Alle Log-Aufrufe MÜSSEN
durch eine `LoggerMessage`-attributierte `partial` Methode laufen. Bestehende
Konvention seit ADR 0004; CA1848 / CA1873 sind in `Directory.Build.props` aktiv.

`SerilogPiiAuditTests` (siehe §6) prüft im Roslyn-Analyzer-Stil, dass die `LogXxx`-
Erweiterungsmethoden ausserhalb der `[LoggerMessage]`-Member nirgends mehr
verwendet werden.

### 6. Audit-Test `SerilogPiiAuditTests`

`tests/FlowHub.Web.IntegrationTests/SerilogPiiAuditTests.cs` (Block 5) enthält zwei
Roslyn-basierte Source-Tests:

- **`AllLogCallsUseLoggerMessage`** — scannt das Compilation-Symbol-Tree und stellt
  sicher, dass alle `ILogger`-Aufrufe `[LoggerMessage]`-generierte Methoden sind.
- **`LoggerMessageParametersAreOnAllowList`** — extrahiert die `Message`-Template-
  Parameter aus jedem `[LoggerMessage]`-Attribut und prüft sie gegen die Allow-List
  aus §2. Unbekannte Parameter-Namen failen den Test mit einer Hinweisliste.

Beide Tests laufen in `just test` (kein Trait-Gate) und schlagen den Build im CI.

### 7. EventId-Namespace `9xxx` reserviert für Compliance/Policy

Erweitert das EventId-Schema aus ADR 0003 / 0004:

- `1xxx` — Pipeline (ADR 0003)
- `2xxx` — Skills (ADR 0003)
- `3xxx` — AI (ADR 0004)
- `9xxx` — Compliance / Policy (diese ADR)
  - `9001 PiiScrubberRegistered` (Information, beim Boot)
  - `9002 PiiScrubberRedacted` (Debug, pro Redaktion — Counter, kein Inhalt)

---

## Consequences

### Rubric coverage

- **Programmierung: Code lesbar, dokumentiert** (max 7) — die Policy ist die Doku;
  source-gen + Allow-List sind die Lesbarkeit.
- **Programmierung: Erkenntnisse dokumentiert** (max 3) — Policy + Audit-Test sind
  die institutionalisierte Erkenntnis.
- **Validierung: Unit-Tests programmiert** (max 3) — `SerilogPiiAuditTests` ist
  Compile-time-Validierung der Code-Basis.

### Operational impact

- Debugging eines kaputten Captures aus dem Log allein wird unmöglich, sobald
  Body/Title nicht mehr im Output stehen. Workflow ist stattdessen:
  `CaptureId` aus dem Log → API-Call `GET /api/captures/{id}` gegen die lokale
  Instanz → Body sichtbar im UI. Akzeptabel, weil Logs in produktiver Aggregation
  landen, die DB nicht.
- `Exception.Message` im Stacktrace bleibt sichtbar — das ist unvermeidbar und in
  der Regel auf bekannte Exception-Typen mit fixen Messages beschränkt. Der
  PII-Scrubber-Enricher fängt den Edge-Case ab, in dem ein HTTP-Body in
  `ex.Message` durchschlägt.

### Was diese ADR NICHT regelt

- **OpenTelemetry-Span-Tags** — siehe ADR-0009 (Telemetry-PII-Policy). Spans und
  Logs sind separate Pfade; die Allow-Lists überschneiden sich, sind aber nicht
  identisch (z.B. ist `MatchedSkill` als Span-Tag zulässig, weil low-cardinality;
  `ConfidenceScore` als Span-Attribute wäre Hochkardinalität und gehört ins
  Metric-Histogram, nicht in den Trace).
- **Audit-Log für Compliance-Events** — Logs hier sind Diagnostik, kein DSGVO-
  Audit-Log. Falls so etwas in einem späteren Block nötig wird, eigene ADR.
- **PII in Capture-Bodies selbst** — Capture-Parser-Pseudonymisierung (Telegram-
  Handles, Mail-Sender) liegt bei den Parsern, nicht bei der Logging-Policy.

---

## References

- ADR 0003 — Async-Pipeline (EventId-Namespace, `LoggerMessage`-Konvention)
- ADR 0004 — AI Integration (CA1848/CA1873 aktiv; `LoggerMessage`-Beispiel)
- ADR 0007 — LLM-Hosting (Cloud-Outbound-Risiko)
- ADR 0009 — Telemetry-PII-Policy (Span-Tag-Allow-List)
- NfA-P1: `docs/spec/nfa.md`
- Risiko-Tabelle: `vault/Knowledge/Datenschutz-und-AI-Act.md` Abschnitt 3.3
- Audit-Test: `tests/FlowHub.Web.IntegrationTests/SerilogPiiAuditTests.cs` (Block 5)
- Serilog: https://serilog.net/
- CA1848 / CA1873 — High-performance logging via `LoggerMessage` source generator
