# AI Tool Usage — FlowHub

> Living document for the rubric criterion "Wurden KI-unterstützende Werkzeuge verwendet und deren Nutzung beschrieben (12)" (highest-weighted single item, see `vault/Organisation/Bewertungskriterien.md`). Updated as work happens — not as a one-shot at submission.

## Tools in use

| Tool | Purpose | Where it shows up |
|---|---|---|
| Claude Code (Opus 4.7, 1M context) | Brainstorming, plan writing, ADR drafting, controller for subagent dispatches | `docs/superpowers/specs/`, `docs/superpowers/plans/`, `docs/adr/`, this file |
| Claude Sonnet 4.6 (subagents) | Implementer + spec reviewer + code-quality reviewer subagents under the superpowers SDD workflow | All `source/` and `tests/` changes from Block 3 Slice B |
| GitHub Copilot | Inline suggestions during editing | Sparingly — Claude Code drives sessions end-to-end |
| ChatGPT | Ad-hoc concept clarification, side checks | Only when Claude is mid-task on something else |

## Workflow used in Block 3

The Block 3 Slice-B work (async pipeline) ran end-to-end through the **superpowers** plugin's structured workflow:

1. `superpowers:brainstorming` — locked 13 design decisions via A/B/C trade-off questions; output is `docs/superpowers/specs/2026-04-30-async-pipeline-design.md`.
2. `superpowers:writing-plans` — produced the 16-task TDD-driven implementation plan at `docs/superpowers/plans/2026-04-30-async-pipeline.md`.
3. `superpowers:subagent-driven-development` — for each task: dispatch a fresh implementer subagent → spec-compliance review subagent → code-quality review subagent → mark complete in the controller's TaskList.
4. `superpowers:using-git-worktrees` — isolated all implementation work in `.worktrees/block3-async-pipeline/` on branch `feat/block3-async-pipeline`, leaving `main` untouched until the branch is reviewed.

## Block 3 Slice B — async pipeline

### Brainstorming + spec writing

Conversational design via Claude Code's brainstorming skill. ~13 decisions surfaced as A/B/C questions; reviewer (the human) corrected scope choice from "Slice A first" (REST API) to "Slice B (ADR 0003 first — pin scope before coding)" after a technical discussion of MassTransit's tradeoffs and the genuine value vs. overhead of a queue in a single-user system.

The user explicitly delegated the remaining design decisions during a 1.5-hour break ("pick recommendations and give a short summary on return"); Claude completed the spec, committed it locally, and presented a decision summary for review.

Final spec self-reviewed by Claude before commit (placeholder scan, internal consistency, ambiguity check, scope check).

### Implementation plan

Authored by Claude Code via the `writing-plans` skill from the approved spec. 16 tasks total, bite-sized TDD ordering: packages → events → ports → adapters → service mark-methods → consumers (enrichment, routing, fault) → Program.cs wiring → ADR 0003 → ai-usage.md → docker-compose sketch → vault checklist.

### Implementation execution

Subagent-driven. Pattern per task:

- Dispatch implementer subagent (general-purpose, Sonnet 4.6 for TDD/judgment, Haiku for mechanical) with the full task text + context — never make the subagent read the plan file.
- Implementer reports DONE / DONE_WITH_CONCERNS / BLOCKED / NEEDS_CONTEXT.
- Dispatch spec-compliance reviewer subagent — reads actual code, compares to plan, returns ✅ / ❌ with file:line citations.
- If ✅ spec, dispatch `superpowers:code-reviewer` agent — returns Strengths / Issues (Critical / Important / Minor) / Assessment.
- Apply minor fix-ups inline (controller-side) for trivial issues; surface critical/important ones back to the implementer.

### Notable adaptations the implementers caught (real value, not hallucinations)

- **`MassTransit.Testing` is not a separate NuGet package** in MassTransit 8.5.9 — `AddMassTransitTestHarness()` lives in the main `MassTransit` package. The plan was wrong; the implementer corrected the package list and the test-project reference.
- **Captive-dependency trap on `IPublishEndpoint`** — MassTransit registers `IPublishEndpoint` as Scoped; `CaptureServiceStub` is Singleton. Plain `AddSingleton<ICaptureService, CaptureServiceStub>()` would fail at startup. Implementer surfaced the issue, applied the canonical fix (factory using `IBus`), and propagated the same pattern to `Program.cs`.
- **`harness.Consumed.Any<T>(predicate, TimeSpan)` overload** doesn't exist in MassTransit 8.5.9 — implementer used `using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5))` and passed `cts.Token` instead.
- **`appsettings.Development.json` is in `.gitignore`** (12-factor compliance per CLAUDE.md). The first T11 commit force-added it; the implementer flagged the issue in their report, and a follow-up commit removed it. The `Program.cs` `else` branch already defaults to `UsingInMemory`, making the file redundant.
- **Pre-existing `CaptureServiceStubTests.cs`** had 4 Block-2 regression tests not mentioned in the plan. Implementer merged the 6 new tests with the existing 4 (rather than overwriting) and updated `CapturesTests.cs` + `SmokeTests.cs` to pass an `IPublishEndpoint` substitute to the new constructor.

### Generated vs. handwritten share (estimate, Slice B only)

| Artifact | AI-drafted | Human-edited |
|---|---|---|
| Brainstorming spec | ~95% | ~5% (decisions, scope, factual corrections) |
| Implementation plan | ~95% | ~5% (verifying packages, paths) |
| ADR 0003 | ~95% | TBC after review |
| Production code (consumers, classifier, registration) | ~95% | ~5% (constructor scope fixes during review) |
| Tests | ~95% | 0% (TDD-first; tests were author-once) |

The 5% human input is high-value: scope correction, package-name correction, factory-pattern surfacing, 12-factor compliance enforcement.

### Reflexion — what worked, what didn't

✅ **What worked**

- The brainstorming-skill A/B/C question format forced explicit decision-making instead of hand-waving. 13 decisions written down, each with rationale.
- Subagent-driven development per task gave clean isolation: each implementer started with a fresh context, the controller curated exactly what they needed, and the two-stage review (spec then quality) caught real issues twice.
- LoggerMessage source-gen caught CA1848/CA1873 analyzer rules early; the team established an EventId namespacing convention (Pipeline 1000–1999, Skills 2000–2999) before the third consumer landed.
- The smoke-test gate (`make run` + `curl /` returning 200) revealed the captive-dependency issue immediately when it would have been silent in a unit-test-only run.

⚠ **What needed correction**

- The plan was authored from incomplete repository knowledge (missed reading `CaptureServiceStubTests.cs`, the existing `ChannelKind` values, the `MassTransit.Testing` package layout). Implementers had to adapt mid-task. Lesson: brainstorming + plan authoring should explicitly grep/read every file the plan references.
- The first T11 commit force-added a gitignored file. The plan didn't catch this; the implementer's self-review did. Lesson: any time the agent runs `git add -f`, that's a red flag worth surfacing in the dispatch prompt.
- The first T8 implementer used a verbose 50-line `NoopPublishEndpoint` hand-rolled mock when `Substitute.For<IPublishEndpoint>()` was already available. The plan's fallback note was missed. Lesson: spell out the simpler alternative inline, not as a footnote.

❌ **What didn't work / where humans had to intervene**

- The .NET SDK pin in `global.json` (10.0.201) didn't match the installed SDK (10.0.104). The agent installed 10.0.201 to `~/.dotnet` and symlinked it into `~/.local/bin/dotnet`. Worked, but added a non-trivial environment change the user has to know about; documented in `~/.bashrc` with a removal note.
- A mid-stream `git history rewrite on main` (PII scrub) invalidated all in-flight commit SHAs. The user had to coordinate the stop/restart manually. Workflow worked: stop after current todo, document HEAD + remaining tasks, resume on the rewritten branch using commit messages as identity.

## Block 3 Slice A — REST API

### Brainstorming + spec writing

Conversational design via Claude Code's brainstorming skill. The session built on the `docs/design/api/api-surface.md` sketch produced during Block 3 Vorbereitung (D1–D6 already locked). Slice A added 5 new decisions (D7–D11): scope sizing (captures-only vs. full surface), versioning strategy (`/api/v1/` prefix), cursor pagination format, ProblemDetails conformance level, and test project topology.

The user picked option β (Captures-only scope) over the full 7-endpoint surface (α) and the no-skills-endpoint variant (γ), explicitly to free implementation time for Slices C and D. Skills and integrations endpoints deferred to Slice D.

Final spec self-reviewed by Claude before commit: placeholder scan, internal consistency check, ambiguity check, scope check.

### Implementation plan

Authored by Claude Code via the `writing-plans` skill from the approved spec. 15 tasks total, TDD-ordered: new project scaffold → endpoint stubs → validators → cursor pagination → list async → GET by id → retry handler → JSON enum config → test project → integration tests per endpoint → OpenAPI/Scalar wiring → problem details → MassTransit IBus injection → vault/ai-usage docs → final test pass.

### Implementation execution

Subagent-driven. 15 tasks dispatched sequentially; Haiku for mechanical tasks (T1 scaffolding, T4 project registration, T13 vault checklist, T14 problem docs); Sonnet 4.6 for all TDD, judgment-heavy, and integration tasks.

New project `source/FlowHub.Api/` co-hosted via project reference in `FlowHub.Web`; new test project `tests/FlowHub.Api.IntegrationTests/` bootstrapped via `WebApplicationFactory<Program>`. Cursor format: hand-rolled URL-safe base64 of JSON `(CreatedAt, Id)` — no ASP.NET dependency introduced into `FlowHub.Core`.

### Notable adaptations the implementers caught

- **`JsonStringEnumConverter` configuration:** Minimal API uses integer enum serialization by default; the T8 implementer added `ConfigureHttpJsonOptions` with `JsonStringEnumConverter` server-side, plus matching `JsonSerializerOptions` in test deserialization. This wasn't pre-specified in the plan.
- **`Captures` namespace shadow:** creating `tests/FlowHub.Web.ComponentTests/Captures/` for `CaptureCursorTests` shadowed the `FlowHub.Web.Components.Pages.Captures` Blazor component referenced in `CapturesTests.cs`. T5 implementer fixed via a `using CapturesPage =` alias at the top of the test file.
- **Consumer race in retry handler:** synthesizing the response capture from the post-reset state (rather than re-querying after `ResetForRetryAsync`) avoids a flaky test where the Slice-B in-memory consumer reclassifies the capture before the response is built (T11).
- **MassTransit package reference:** T11 implementer added `<PackageReference Include="MassTransit" />` to `FlowHub.Api.csproj` for `IBus` injection — the package version was already centrally pinned in `Directory.Packages.props` from Slice B, so no version conflict arose.
- **Missing `using Microsoft.AspNetCore.Http;`:** the T2 endpoint snippet in the plan omitted this import; the T2 implementer caught and added it before the build could fail.

### Generated vs. handwritten share (estimate, Slice A only)

| Artifact | AI-drafted | Human-edited |
|---|---|---|
| Spec | ~95% | ~5% (scope choice α/β/γ + cursor format choice) |
| Plan | ~95% | ~5% (verifying paths) |
| Production code (endpoints, validators, cursor, ListAsync) | ~95% | ~5% (constructor / namespace fixes during review) |
| Tests (integration via WebApplicationFactory) | ~95% | 0% |

### Reflexion — what worked, what didn't

**What worked**

- The api-surface sketch from Block 3 Vorbereitung removed ~80% of the design work for Slice A — the brainstorming session reduced to scope sizing and gap-filling 5 new decisions rather than designing from scratch.
- Implementer subagents caught real .NET defaults issues (`JsonStringEnumConverter`, namespace shadow) that the plan didn't anticipate — the two-stage review (spec-compliance then code-quality) continued to justify its overhead.
- The `WebApplicationFactory`-based test project gave end-to-end confidence that the Minimal API routes, validators, and JSON serialization all compose correctly in the real DI container — no mocking of infrastructure.

**What needed correction**

- Plan defects: the T2 endpoint snippet was missing `using Microsoft.AspNetCore.Http;`, and the JSON enum string-converter plumbing wasn't pre-specified. Both were caught by implementers mid-task. Lesson: plan authoring should compile-check inline snippets mentally against the existing `_Imports` / `using` surface.
- Cursor format was underspecified in the initial plan draft (no mention of URL-safe base64 vs. plain base64). The T5 implementer picked URL-safe; the plan was updated retroactively.

**What didn't work / blockers**

N/A — no blockers; subagent dispatches went straight through without escalation.

## Block 3 Slice C — AI integration (production-runtime)

Slice C is the first time AI moves from *development tool* to *production-runtime
component*. The classifier port `IClassifier` (introduced in Slice B as a hexagonal
seam with a deterministic `KeywordClassifier` adapter) now has a second adapter,
`AiClassifier`, that calls a real LLM in the request path of the enrichment consumer.

### Brainstorming + spec writing

Conversational design via Claude Code's brainstorming skill. 10 decisions surfaced
as A/B/C questions covering: scope (classifier-only vs. classifier+title vs. multi-
feature enrichment), provider mix (which two adapters), abstraction layer
(`Microsoft.Extensions.AI` vs. Semantic Kernel), port shape (extend
`ClassificationResult` vs. sibling `IEnricher`), failure semantics (graceful fallback
vs. hard fail), default models, structured-output strategy, no-key startup behaviour,
test layering (mocked unit + trait-gated live), and provider selection.

User picked the .NET-native MEAI abstraction over Semantic Kernel (Q3) and the
graceful fallback over hard-fail (Q5) — both decisions explicitly motivated by the
rubric phrasing *"intelligente und flexible Services"* (graceful = flexible).

Final spec self-reviewed by Claude before commit (placeholder scan, internal
consistency, ambiguity check, scope check).

### Implementation plan

Authored by Claude Code via the `writing-plans` skill from the approved spec. 15
tasks total, TDD-ordered: packages → ClassificationResult extension → FlowHub.AI
csproj → AiPrompts → AiClassificationResponse DTO → AiClassifier (10 unit tests) →
AddFlowHubAi (8 unit tests covering D8 matrix) → Program.cs wiring → integration
test project → 4 trait-gated live tests → Makefile filter → ai-usage.md → ADR 0004
→ CLAUDE.md placeholder note → vault checklist + CHANGELOG + final pass.

### Implementation execution

Subagent-driven. Sonnet 4.6 for TDD/judgment-heavy tasks (T2, T4–T10); Haiku for
mechanical tasks (T1 packages, T3 csproj scaffolding, T9 integration test scaffold,
T11 Makefile, T14 CLAUDE.md tweak, T15 vault checklist).

### Production-runtime AI use

This slice is what the rubric *"KI: Wurden KI-Werkzeuge verwendet"* (max 12 pts)
actually asks for: AI inside the running application, not just AI as a coding
assistant. The `AiClassifier` calls a real LLM (Anthropic Haiku 4.5 by default,
swappable to OpenRouter Llama 3.1 70B Instruct via one env var) on every capture
that flows through the enrichment pipeline.

Cost guards: `MaxOutputTokens=300`, `Temperature=0.2`, 10s HTTP timeout, Anthropic
prompt-cache marker on the system prompt segment.

Failure handling: any provider exception or schema violation logs `EventId 3010
AiClassifierFellBackToKeyword` at Warning and routes to the deterministic
`KeywordClassifier` floor — capture is always classified.

### Notable adaptations the implementers caught (real value, not hallucinations)

(To be filled in by the implementer after Task 15 final pass — same shape as the
Slice B / Slice A entries.)

### Generated vs. handwritten share (estimate, Slice C only)

(To be filled in.)

### Reflexion — what worked, what didn't

(To be filled in.)

## Prompts of note

(Captured here when surprising or high-leverage. Empty for now — most prompts followed standard skill conventions.)

## References

- ADR 0003: `docs/adr/0003-async-pipeline.md`
- Spec: `docs/superpowers/specs/2026-04-30-async-pipeline-design.md`
- Plan: `docs/superpowers/plans/2026-04-30-async-pipeline.md`
- Bewertungskriterien: `vault/Organisation/Bewertungskriterien.md`
