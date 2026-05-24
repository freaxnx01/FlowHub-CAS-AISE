# ADR 0009 — Telemetry-PII-Policy: Span-Tag-Allow-List für OpenTelemetry

- **Status:** Accepted
- **Date:** 2026-05-24
- **Block:** Block 5 (Deployment) — Nachbereitung
- **Decider:** freax
- **Affects:** `source/FlowHub.Web/Program.cs` (OTel-Pipeline), zukünftige `ActivitySource`-Nutzung in `source/FlowHub.*/`, `tests/FlowHub.Web.IntegrationTests/TracingPiiAuditTests.cs` (Block 5)

---

## Context

Block 5 Tracing-Stand (Stand 2026-05-24): `Program.cs` registriert
`AddOpenTelemetry().WithMetrics(...)` mit AspNetCore-/Runtime-/Prometheus-
Instrumentation. Explizite `ActivitySource`/`Activity.SetTag`-Aufrufe gibt es im
Anwendungs-Code noch nicht — der einzige aktive Trace-Pfad läuft über die MEAI-
`UseOpenTelemetry()`-Decorator-Chain im `AiClassifier` (ADR 0004 §OTel), die
`gen_ai.*`-Attribute mit Provider/Model/Token-Counts emittiert.

Sobald die Block-5-Grafana-Dashboard-Stufe oder OTLP-Export aktiviert wird, werden
Span-Tags zu einem zweiten PII-Exposure-Pfad — strukturell ähnlich zu Logs (siehe
ADR 0008), aber mit zwei Eigenschaften, die separate Regeln verlangen:

1. **Kardinalität ist hier ein eigenes Problem.** Logs vertragen jede Cardinality,
   weil sie pro Event gespeichert werden. Span-Tags landen in Trace-Backends, die
   Tag-Werte indizieren — Hochkardinalität (z.B. `confidence_score=0.6437`) bläht
   den Index auf und ist betriebliche Schuld, nicht nur Datenschutz-Schuld.
2. **MEAI-`gen_ai.*` und MassTransit-Auto-Instrumentation emittieren bereits Tags**,
   die wir nicht selber schreiben. Eine Policy nur für Eigen-Tags wäre lückenhaft —
   die Policy muss auch sagen, *welche fremden Tags wir akzeptieren* und welche
   ggf. mit einem Processor strippen.

Risiko-Tabelle in `vault/Knowledge/Datenschutz-und-AI-Act.md` Abschnitt 3.3 nennt
"OpenTelemetry-Span-Tags" als eigene Kategorie mit `TracingPiiAuditTests` als
Nachweis.

---

## Decision

### 1. Allow-List für Eigen-Tags (`Activity.SetTag`)

Eigene Span-Tags in FlowHub-Code DÜRFEN nur aus folgender Liste stammen:

| Tag-Key | Typ | Beispielwert | Quelle |
|---|---|---|---|
| `flowhub.capture_id` | string (Guid) | `"7c8f…"` | Pipeline / API |
| `flowhub.stage` | string (enum) | `"Classified"` | Pipeline |
| `flowhub.classification_source` | string (enum) | `"AI"` | Classifier |
| `flowhub.matched_skill` | string (closed set) | `"Vikunja"` | Classifier / Router |
| `flowhub.skill.name` | string (closed set) | `"Wallabag"` | Skill-Adapter |
| `flowhub.skill.outcome` | string (enum) | `"Routed" \| "Unhandled"` | Router |
| `flowhub.body_length` | int | `342` | Capture-Parser (Bucket-Wert ok) |
| `flowhub.tag_count` | int | `4` | Classifier |
| `flowhub.fallback` | bool | `true` | AiClassifier |
| `flowhub.reason` | string (exception-type) | `"HttpRequestException"` | Fault-Pfade |

`flowhub.*`-Namespace ist reserviert. Punktnotation, snake_case (OTel-Konvention).
Tag-Werte mit *kontinuierlicher* Numerik (z.B. `confidence_score=0.6437`) sind
verboten — solche Werte gehören in Histogramme/Counter (siehe §3), nicht in
Trace-Tags.

### 2. Verbotene Tags (auch aus fremder Instrumentation)

Auch wenn eine Bibliothek sie automatisch setzt, MÜSSEN folgende Tags vor dem
Export gestrippt werden:

- `http.request.body.*`, `http.response.body.*` — Capture-Bodies und API-Antworten
- `db.statement`, `db.query.text` — kann WHERE-Bedingungen mit Capture-IDs +
  Bodies enthalten (EF Core Sensitive-Data-Logging ist in `Directory.Build.props`
  ohnehin off; falls jemand es lokal aktiviert, fängt der Processor es ab)
- `messaging.message.body.size` ist ok; `messaging.message.payload` ist verboten
- `gen_ai.prompt`, `gen_ai.completion` — MEAI emittiert diese standardmässig
  *nicht*, aber die OTel-Sem-Conv-Spec definiert sie; opt-in muss verhindert
  werden
- Alle Tag-Keys, die auf `.email`, `.username`, `.user.id` enden (Heuristik)

### 3. Trennung Trace vs. Metric — hohe Kardinalität → Metric

Werte, die pro Request variieren und keine fixe Werte-Menge haben, gehören in:

- **Histogramme** für numerische Verteilungen (`flowhub.classification.confidence`,
  `flowhub.classification.duration_ms`).
- **Counter** für Event-Häufigkeiten (`flowhub.skill.outcome` als Counter mit dem
  closed-set Label `outcome`).

Trace-Tags bleiben low-cardinality. Verhindert Index-Explosion in Tempo/Jaeger und
hält die Tags-Liste in §1 prüfbar.

### 4. `TagAllowListProcessor` als zweite Verteidigungslinie

`source/FlowHub.Web/Program.cs` fügt der `AddOpenTelemetry().WithTracing(...)`-
Pipeline (sobald Tracing aktiviert wird) einen
`BaseProcessor<Activity> TagAllowListProcessor` hinzu, der:

- alle Tags mit Prefix `flowhub.` gegen die Allow-List aus §1 prüft — unbekannte
  flowhub-Tags werden gestrippt (nicht der Span; nur das Tag) und auf
  `EventId 9011 UnknownFlowhubTagDropped` (Warning, gedrosselt) geloggt.
- Tags mit Keys aus der Block-Liste in §2 entfernt.
- Tag-Werte, die String-Länge >256 überschreiten, durch `"<redacted:length>"`
  ersetzt.

Defense-in-Depth — primäre Verteidigung bleibt §1 + Audit-Test.

### 5. Source-Generated Marker für eigene Tag-Aufrufe

`Activity.SetTag("flowhub.…", value)` darf nur über eine zentrale Helper-Klasse
`FlowHubActivityTags` (in `source/FlowHub.Core/Telemetry/`) gesetzt werden. Die
Klasse hat eine `partial` Methode pro erlaubtem Tag:

```csharp
public static void SetCaptureId(this Activity? activity, Guid captureId);
public static void SetStage(this Activity? activity, CaptureStage stage);
// …
```

Damit ist das Setzen von Tags compile-time auf die Allow-List eingeschränkt. Direkte
`Activity.SetTag(string, object?)`-Aufrufe ausserhalb von `FlowHubActivityTags`
sind verboten und werden vom Audit-Test (siehe §6) erkannt.

### 6. Audit-Test `TracingPiiAuditTests`

`tests/FlowHub.Web.IntegrationTests/TracingPiiAuditTests.cs` (Block 5):

- **`NoDirectActivitySetTagCallsOutsideHelper`** — Roslyn-Symbol-Scan: alle
  `Activity.SetTag`-Aufrufe ausser in `FlowHubActivityTags` failen.
- **`AllFlowhubTagKeysAreOnAllowList`** — extrahiert die String-Literale aus
  `FlowHubActivityTags`-Aufrufen und prüft sie gegen die Liste in §1.
- **`InMemoryExporterShowsNoForbiddenTags`** — Integration-Test, der die App mit
  einem `InMemoryExporter` instanziiert, einen vollen Capture-Zyklus durchspielt
  und die exportierten Spans gegen §2 prüft (kein `http.*.body`, kein
  `gen_ai.prompt`, kein `db.statement`).

### 7. EventId-Reuse `9xxx` aus ADR 0008

Telemetry-Policy nutzt dieselbe Namespace-Sektion wie Logging-Policy (`9xxx`):

- `9011 UnknownFlowhubTagDropped` (Warning, gedrosselt) — Processor hat einen
  flowhub-Tag gefiltert.
- `9012 ForbiddenTagStripped` (Warning, gedrosselt) — Processor hat einen Tag
  aus §2 entfernt.

---

## Consequences

### Rubric coverage

- **Programmierung: Code lesbar, strukturiert** (max 7) — `FlowHubActivityTags` ist
  die zentrale Stelle, an der Tracing-Vokabular definiert wird.
- **Validierung: Unit-Tests programmiert** (max 3) — drei Audit-Tests in
  `TracingPiiAuditTests`.
- **Sub-Systeme als Container** (max 5) — der OTel-Collector (falls eingeführt)
  ist ein eigener Compose-Service mit klarer Tag-Boundary.

### Operational impact

- Trace-basiertes Debugging eines Captures geht nur über `flowhub.capture_id`
  als Filter — kein Body, keine Tags, kein Inhalt im Trace. Workflow ist wie bei
  Logs (ADR 0008): `capture_id` aus dem Trace → lokale DB-Abfrage.
- MEAI-`gen_ai.*`-Tags bleiben erlaubt (Provider, Model, Token-Counts) — sie
  sind low-cardinality und enthalten kein PII. Konkretes Allow-Set wird zur
  ersten echten Tracing-Aktivierung in die Liste in §1 nachgezogen.
- Wenn jemand `WithTracing(...)` aktiviert, ohne den `TagAllowListProcessor` zu
  registrieren, schlägt ein zusätzlicher Boot-Check (`AddFlowHubTelemetryAudit`-
  Helper) mit `InvalidOperationException` fehl. Fail-fast statt stiller
  Datenleck.

### Was diese ADR NICHT regelt

- **Metric-Labels** — Counter/Histogramm-Labels haben dieselbe Kardinalitäts-
  Disziplin (closed sets, keine free-form-Strings), folgen aber dem OTel-
  Metric-Vokabular und liegen ausserhalb dieser ADR. Falls ein Audit-Test
  dafür gewünscht ist: eigene Folge-ADR.
- **OTLP-Exporter-Zielsystem** — diese ADR betrifft *was* exportiert wird, nicht
  *wohin*. Cloud-OTLP-Backends (Honeycomb, Datadog, etc.) sind eine eigene
  Hosting-Entscheidung analog zu ADR 0007.
- **Logs als Tracing-Backplane (Logs-as-Spans)** — falls in einem späteren Block
  ein OpenTelemetry-Logs-Pipeline aktiviert wird, gelten ADR 0008 (Logging) +
  diese ADR gemeinsam; die Überschneidung wird dann konkret aufgelöst.

---

## References

- ADR 0003 — Async-Pipeline (EventId-Namespace)
- ADR 0004 — AI Integration (MEAI `UseOpenTelemetry`, `gen_ai.*`-Tags)
- ADR 0007 — LLM-Hosting (Outbound-Boundary, lokale vs. Cloud-Telemetry)
- ADR 0008 — Logging-Policy (gleiches Schema für Logs; EventId 9xxx-Namespace)
- NfA-P1: `docs/spec/nfa.md`
- Risiko-Tabelle: `vault/Knowledge/Datenschutz-und-AI-Act.md` Abschnitt 3.3
- Audit-Test: `tests/FlowHub.Web.IntegrationTests/TracingPiiAuditTests.cs` (Block 5)
- OpenTelemetry Semantic Conventions: https://opentelemetry.io/docs/specs/semconv/
- Microsoft.Extensions.AI OTel decorator: https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai
