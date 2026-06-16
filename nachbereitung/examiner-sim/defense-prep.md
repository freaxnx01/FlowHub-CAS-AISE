# FlowHub — PVA Defense Prep & Gap-Fill Plan

Prep aid for the CAS-AISE oral defense (PVA), derived from the `examiner-sim`
runs. **Not part of the submission bundle** — internal study sheet.

- **Max achievable: 100** — the **rubric was updated (June 2026)**: the programming
  "framework concepts" item is now **framework-neutral** (no longer Quarkus/Java), so
  .NET earns it directly, and the Sub-System item now **explicitly accepts a modular
  monolith run as a container**. No item is excluded — the old "/90" framing is gone.
- **Measured grade:** **86/90** on the old architecture-skeptic lens (~88 after the
  verified-test-run pass; balanced ~90). On the new /100 rubric, add the framework item
  (~10) and the now-winnable Sub-System point → low-to-mid **90s/100**.
- **How to use:** for each likely question, a crisp answer + *where to point* in the
  repo/bundle. Own the known gaps honestly — examiners reward "I know exactly what's
  not done and why" over a defended overclaim.

---

## A. Spezifikation

**Q: Which NfAs are genuinely SMART, and how do you measure NfA-P1/P2?**
A: `NfA-01..05`, `NfA-D1..D3`, `NfA-O1` are decomposed along all five SMART
dimensions — point to `docs/spec/nfa.md` (each has Specific/Measurable/Achievable/
Relevant/Time-bound). `NfA-P1` (residency) and `NfA-P2` (AI-Act transparency) are
explicitly labelled **ZIEL/target, not yet implemented** — the measurement is
defined as an acceptance criterion but honestly marked open. The older `NF-01..NF-13`
table in `use-cases.md` is the Block-2 catalogue, superseded by `nfa.md` (mapping
note at the top of that file).

**Q: UC-09 — walk the async classify-and-route failure path.**
A: `CaptureEnrichmentConsumer` calls `IClassifier`; on model failure it falls back
to `KeywordClassifier` (logged, `EventId 3010`). Empty classification → `Orphan`.
`SkillRoutingConsumer` resolves `ISkillIntegration` by name; none registered →
`Unhandled`. Past retry budget, `Fault<T>` → `LifecycleFaultObserver` sets the
terminal stage. Tests in `tests/FlowHub.Web.ComponentTests/Pipeline/`.

---

## B. Entwurf

**Q: Show me the capture state machine and the submit→classify→route sequence.**
A: `docs/design/perspectives.md` — a `stateDiagram-v2`
(Raw→Classified→Routed/Unhandled/Orphan) and a sequence diagram; deeper sequences
in `docs/design/sequences/` (intake, enrichment, skill-routing). **Now rendered as
SVG in the bundle PDF** (Design-Perspektiven section).

**Q: Is the §6.1 overview diagram the system you built?**
A: No — §6.1 is the **concept/target** architecture (captioned as such); it shows
not-yet-built parts (Ollama, Telegram, Redis). The **as-built** view is the
C4/hexagonal diagrams in `perspectives.md`, the ER model in `db/er.md`, and the ADRs
(0002/0003/0005 each carry an "As built" note). Built system = Modular Monolith,
six projects, MassTransit pipeline.

**Q: Justify the foreign-key / delete strategy.**
A: `docs/design/db/er.md` — soft vs hard FK, CASCADE vs RESTRICT per relationship,
index list, pgvector/HNSW for search (ADR 0006).

---

## C. Programmierung

**Q: Show a driving port and its adapter — how does Core stay infra-free?**
A: `ICaptureRepository` (Core) → `EfCaptureRepository` (Persistence/Repositories/);
`IClassifier` → `AiClassifier` (AI); `ISkillIntegration` → Wallabag/Vikunja (Skills).
`FlowHub.Core` has **zero** EF/Npgsql/MassTransit references (verifiable in the
`.csproj`). `EfCaptureService` composes `ICaptureRepository`, not `DbContext`.

**Q: Your inline XML-doc coverage is ~27% — comment on that.**
A: Acknowledge openly. Public ports and key types are documented; every project now
has a `README.md` describing its role; the gap is private/implementation members.
It's on the post-submission list (see Gap-Fill §F.3), not hidden.

**Q: Why are FlowHub.Telegram / FlowHub.Integrations shown as projects when empty?**
A: They aren't — the empty folders were **removed**; the source tree is exactly the
six solution projects. A Telegram channel and a generic integrations layer are
planned (not yet scaffolded). The Wallabag/Vikunja adapters live in `FlowHub.Skills`
(ADR 0002 has an "As built" note correcting the early plan).

**Q: The "Konzepte des gewählten Frameworks" criterion (DI, REST, Konfiguration, Fehlerbehandlung) — how does .NET deliver it?**
A: The criterion is **framework-neutral** (the June-2026 rubric replaced the old
Quarkus/Jakarta-EE wording). The chosen framework is **.NET 10 / ASP.NET Core**, and
the four named concepts are all in code — *"here's each one, where to point"* (full
evidence: `docs/spec/modern-app-concepts.md`):

| Named concept | FlowHub (.NET) — where to point |
|---|---|
| **Dependency Injection** | built-in DI, per-module `*ServiceCollectionExtensions`; Core stays infra-free |
| **REST-Schnittstellen** | Minimal API + RFC 9457 ProblemDetails (`FlowHub.Api/Endpoints`) |
| **Konfiguration** | `IConfiguration` + Options + env vars (12-factor, no secrets in code) |
| **Fehlerbehandlung** | ProblemDetails everywhere + MassTransit retry + deterministic fallback (EventId 3010) |

…plus the broader modern-app concepts (ORM via EF Core + 6 repositories; async
throughout; MassTransit messaging, 5 consumers; Testcontainers vs real PostgreSQL;
OpenTelemetry/Prometheus). **Target: vollständig/korrekt (10)** — no stack caveat
needed; the criterion no longer names Java/Quarkus. (Only honest non-claim: no real
GraalVM native image — a lean Alpine container isn't AOT.)

---

## D. Validierung

**Q: How many tests, and what does CI actually prove?**
A: **294** offline tests green (default CI suite: `Category!=AI&!=BetaSmoke&!=E2E`),
0 failed, 0 skipped; the suite additionally passes the trait-gated AI + live-service
integration tests when enabled (E2E excluded). The lower numbers in the
Nachbereitungen (31, 99, 171, 223) are earlier per-block / per-filter snapshots — see
the reconciliation table in `docs/spec/testing-strategy.md`. CI proves the offline
suite; AI/E2E are trait-gated and run on demand.

**Q: Why Testcontainers over EF InMemory for persistence tests?**
A: Real PostgreSQL catches what InMemory can't: pgvector queries, FK cascade
behaviour, and provider-specific LINQ translation. A query can pass InMemory and
fail real PG — so persistence is tested against the real engine.

---

## E. KI, Sub-Systeme & Reflexion

**Q: Sub-Systeme / Container — is a modular monolith enough?**
A: **Yes — the June-2026 rubric explicitly accepts it**: *"klar abgegrenzte Module
bzw. Sub-Systeme (modularer Monolith oder verteilte Services) und als Container
lauffähig betrieben."* FlowHub is exactly that: six clearly-bounded modules
(`Core/Api/AI/Persistence/Skills/Web`) with clean responsibilities, no cross-module
refs, per-module DI (ADR 0002) — **and it runs as a container stack** (Docker
Compose: web + migrations init-job + postgres/rabbitmq/prometheus/grafana, live on
the demo). So this is **vollständig/korrekt (5)**, not a gap. If pushed on
distribution: the MassTransit transport already swaps in-memory↔RabbitMQ, so a
later split into independent processes is a config+host change, not a rewrite —
but the rubric doesn't require it.

**Q: ADR 0003 / p.56 show a `flowhub.api` container — show it in compose.**
A: It wasn't built — `FlowHub.Api` is an **in-process class library** composed into
`flowhub.web`. ADR 0003 carries an "As built" note saying exactly this. The split
was unnecessary for the submission.

**Q: Is the default LLM local Ollama? Where does capture text actually go?**
A: Be straight: **today it's cloud** — OpenRouter (Gemma) for classification,
Mistral for embeddings. Local Ollama is the *target* (ADR 0007 + NfA-P1, both
labelled as not-yet-implemented). So capture content currently leaves the homelab
for inference — a documented, deliberate gap, not a hidden one.

**Q: A bare arXiv id got the correct title — how do you prevent a hallucinated one?**
A: Schema-validated response, allow-list re-validation of `MatchedSkill`,
deterministic fallback to `KeywordClassifier` on any invalid/failed response
(`EventId 3010`), plus temperature/token caps and cost guards at the composition root.

**Q: Show a distributed trace in Grafana.**
A: Honest: **metrics** are live (Prometheus/Grafana, `/metrics`); **distributed
tracing** (OTLP exporter / `WithTracing` / MassTransit source) is wired-but-dormant.
It's a known observability gap (Gap-Fill §F.8), not a claimed feature.

**Q: Your Fazit refutes the "fast typist" hypothesis — what changed concretely?**
A: The bottleneck moved from typing to **spec + review**: more time on ADRs/specs
up front and on reviewing generated code, less on authoring. Per-block insight files
record the shift with concrete incidents (e.g. dual-provider EF trap, N+1 `.Include`).

---

## F. Remaining gaps & how to fill them

**Done — earlier doc passes (shipped):** NF-table relabel, as-built §6.1 + rendered
diagrams, XML-doc on the Core ports, removed the empty Integrations/Telegram folders,
verified-test-run embedded (294/0/0). These already landed.

**Resolved by the June-2026 rubric update — no longer a gap:** the "independent
first-party container" requirement. The rubric now accepts a **modular monolith run
as a container**, which FlowHub is → Sub-System item is **5/5**, not a deduction.

**Remaining (post-submission product work; in the defense, present as a documented
roadmap, not omissions — they're honestly labelled "geplant" in the submission):**

| Item | Why it's not done | Honest status in submission |
|---|---|---|
| NfA-P2 (AI-Act classification badge + provenance columns) | post-submission feature | labelled ZIEL/planned in `nfa.md` |
| NfA-P1 (local Ollama residency + `OutboundCallAuditTests`) | post-submission feature | labelled ZIEL/planned; cloud-today stated openly |
| EF outbox + OTLP tracing | post-submission hardening | ADR 0003/0004 "As built" notes mark them open |

**Bottom line for the PVA:** with the rubric update, both former weak spots
(Java-stack mismatch, independent-container requirement) are gone. What remains is a
short, honestly-documented roadmap of optional features — evidence of judgment, not
holes.
