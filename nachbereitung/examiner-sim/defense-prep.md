# FlowHub — PVA Defense Prep & Gap-Fill Plan

Prep aid for the CAS-AISE oral defense (PVA), derived from the `examiner-sim`
runs. **Not part of the submission bundle** — internal study sheet.

- **Max achievable:** 90 conservative floor; **toward /100** with the Quarkus/Jakarta-EE
  item **claimed via .NET equivalents** (free stack choice confirmed in the PVA — see §C).
- **Measured grade:** **86/90** on the architecture-skeptic lens (latest run), ~88 after the
  verified-test-run pass; **balanced ~90**. Quarkus credit (~7) is additive toward /100.
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

**Q: This is the "Quarkus / Jakarta EE" criterion — you used .NET, so isn't it N/A / 0?**
A: No — **free stack choice was explicitly confirmed in the PVA**, and the Moodle
Auftrag names Quarkus/Jakarta EE only as the *reference* stack; the learning
objectives are stack-neutral. The criterion measures **modern application
concepts**, and *"here are the modern-app concepts realised in .NET"* — each proven
in code (full table + evidence in `docs/spec/modern-app-concepts.md`):

| Jakarta EE / Quarkus concept | FlowHub (.NET) — where to point |
|---|---|
| CDI dependency injection | built-in DI, per-module `*ServiceCollectionExtensions` |
| JAX-RS / RESTEasy | Minimal API + RFC 9457 ProblemDetails (`FlowHub.Api/Endpoints`) |
| Bean Validation | FluentValidation at the boundary |
| JPA / Hibernate | EF Core + 6 repositories + `IEntityTypeConfiguration` (ADR 0005) |
| MicroProfile Config | `IConfiguration` + Options + env vars (12-factor) |
| MicroProfile Health / Metrics | Health Checks (`/health/live`) + OpenTelemetry/Prometheus (`/metrics`, live) |
| Reactive (Mutiny) | `async`/`await` + `CancellationToken` throughout |
| Reactive Messaging / Kafka | MassTransit pipeline, 5 consumers (ADR 0003) |
| MicroProfile Fault Tolerance | per-consumer retry + deterministic classifier fallback (EventId 3010) |
| Testcontainers | Testcontainers .NET vs real PostgreSQL (35 persistence tests) |

Honest boundary: I do **not** claim the JVM-runtime specifics (CDI annotations,
Quarkus build-time DI, real GraalVM native image — a lean Alpine container isn't
AOT). Target level: **überwiegend–vollständig (7–10)**; /90 is the floor only if a
grader rejects the (confirmed) stack freedom.

---

## D. Validierung

**Q: How many tests, and what does CI actually prove?**
A: **171** offline tests green (default suite: `Category!=AI&!=BetaSmoke&!=E2E`),
**223** green including AI + live-service integration (E2E excluded). The different
numbers across docs are per-block / per-filter snapshots — see the reconciliation
table in `docs/spec/testing-strategy.md`. CI proves the offline suite; AI/E2E are
trait-gated and run on demand.

**Q: Why Testcontainers over EF InMemory for persistence tests?**
A: Real PostgreSQL catches what InMemory can't: pgvector queries, FK cascade
behaviour, and provider-specific LINQ translation. A query can pass InMemory and
fail real PG — so persistence is tested against the real engine.

---

## E. KI, Sub-Systeme & Reflexion

**Q: Defend the single-process Modular Monolith against "Sub-Systeme unabhängig als Container."**
A: ADR 0002 — a conscious decision for a single-operator tool: logical service
boundaries (ports/adapters, no cross-module refs, per-module DI) without the
operational cost of distribution. The runtime topology is `flowhub.web` +
`flowhub.migrations` (init-job) + backing services. Honest: only one first-party
**app** container today. It's **reversible** — the MassTransit transport already
swaps in-memory↔RabbitMQ, so splitting a consumer into its own process is a
configuration + host change, not a rewrite. (See Gap-Fill §F.5.)

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

Ordered by leverage. Items 1–4 are still **submission-legal** (raise the rubric
score; doc/quality work) — items 5–8 are real features that the `SCOPE-FREEZE`
defers to **after** the Moodle upload (2026-07-04).

| # | Gap | Bucket → effect | How to fill | Effort | Allowed now? |
|---|---|---|---|---|---|
| 1 | The `NF-01..13` table is titled "(SMART)" but isn't | Spez NfA → +2 | Relabel it "Quality attributes (Block 2)" or SMART-decompose it | ~15 min | ✅ doc |
| 2 | §6.1 overview SVG is concept-not-as-built | Entwurf → up to +3 | Redraw §6.1 as the real 6-project monolith + MassTransit pipeline (or promote the `perspectives.md` C4 to the headline slot) | ~half day | ✅ doc/diagram |
| 3 | Inline XML-doc ~27% | Prog → toward 7/7 | Add `///` to public ports/services across `source/`; raise to ~60%+ | ~half day | ✅ doc |
| 4 | Empty Integrations/Telegram still in the tree | Prog credibility | Either delete the two `.gitkeep` folders, or implement the Telegram channel | 5 min (delete) | ✅ doc |
| 5 | No independent first-party container | KI Sub-Systeme 3→5 (+2) | Ship `FlowHub.Api` (or `FlowHub.Telegram`) as its own image + compose service over the shared bus; push it on release | ~1–2 days | ⛔ post-submission |
| 6 | NfA-P2 (AI-Act badge) not implemented | KI credibility | Add `ClassificationSource/ClassifiedAt/ConfidenceScore` columns + migration, set them in `AiClassifier`, render the badge in `LifecycleBadge`, add the bUnit test | ~1 day | ⛔ post-submission |
| 7 | NfA-P1 (local residency) not implemented | Privacy credibility | Build a `Local`/Ollama provider adapter in `FlowHub.AI`, make it the default, add `OutboundCallAuditTests` | ~1–2 days | ⛔ post-submission |
| 8 | EF outbox + OTLP tracing dormant | Correctness/observability | Wire `MassTransit.EntityFrameworkOutbox`; enable the OTLP trace exporter + MassTransit instrumentation | ~1 day | ⛔ post-submission |

**Bottom line for the PVA:** items 1–4 are the only ones worth doing *before* the
deadline (cheap, rubric-positive, freeze-legal). Items 5–8 are product work — in the
defense, present them as a **deliberate, documented roadmap** with the reasons above,
not as omissions. That framing turns the gaps into evidence of architectural judgment.
