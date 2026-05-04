# Beta MVP — End-to-End Vertical Slice (Web → AI → Wallabag/Vikunja)

- **Status:** Accepted (brainstorming complete, ready for implementation plan)
- **Date:** 2026-05-04
- **Scope:** Beta-MVP that validates the FlowHub architecture end-to-end. Replaces in-memory Bogus stubs with SQLite persistence, ships two real `ISkillIntegration` adapters (Wallabag + Vikunja), adds the `Completed` lifecycle terminal state, and activates the Slice-C `AiClassifier` with an Anthropic key.
- **Pivot:** This slice consciously leaves the Block/Phase rhythm (Block 4 Persistence + Block 5 Skills/Deployment work pulled forward). Goal is a working homelab Beta in 3-4 days to validate the Modular-Monolith + async-pipeline + AI-classifier-with-fallback architecture before continuing rubric-aligned work.
- **Related:** ADR 0001 (frontend), ADR 0002 (modular monolith), ADR 0003 (async pipeline), ADR 0004 (AI integration), `docs/design/db/entities.md` (DB sketch), `docs/spec/use-cases.md` (UC-08..UC-11 + NF-09..NF-13).

---

## Context

After Slice C the codebase has every architectural piece **except** persistence and real downstream skills:

- ✅ Blazor UI (Block 2) with Bogus-stub `ICaptureService`
- ✅ REST API (`/api/v1/captures`, Slice A)
- ✅ MassTransit async pipeline with `CaptureEnrichmentConsumer` + `SkillRoutingConsumer` + `LifecycleFaultObserver` (Slice B)
- ✅ `AiClassifier` wrapping `KeywordClassifier` floor with structured output via `Microsoft.Extensions.AI` (Slice C)
- ❌ **Persistence** — everything is in-memory; restart loses state
- ❌ **Real `ISkillIntegration` implementations** — no actual Wallabag/Vikunja calls happen; routing is end-of-line at `Stage=Routed`
- ❌ **`Completed` terminal state** — exists in the enum, no consumer writes it

The Beta closes those three gaps in a vertical slice that touches every part of the architecture in one demo: paste a URL → see it land in Wallabag → see the lifecycle chip transition Raw→Classified→Routed→Completed → restart → state is still there.

If the architecture is working, this slice surfaces it. If something's wrong, the failure surfaces here on a small enough surface to fix.

## Goals

- One Web-UI capture submission flows end-to-end through AI classification and reaches a real downstream (Wallabag for URLs, Vikunja for `todo:` strings).
- Capture state survives a `dotnet stop` / `make run` cycle (SQLite-backed).
- The existing Dashboard cards (RecentCaptures, NeedsAttention) and Captures list/detail pages all display real data — no UI rewrite needed; only the service implementation swaps.
- The classifier's `Title` field (Slice C addition) becomes visible in the UI: as the Recent Captures grid title and as the Vikunja task title.
- Demo runs via `make run` on the dev box. No Docker dependency yet.

## Non-goals

- Telegram channel — deferred (the Beta is **Web-UI-only** input). Telegram is a fast-follow once the architecture proves out.
- Authentication — `DevAuthHandler` (the Block-2 dev bypass returning fixed "Dev Operator") stays. OIDC via Authentik is Block-5 deployment work.
- Production deployment / Docker — `make run` against the dev box is the Beta target. Dockerfile + compose is Block-5 work.
- `SkillHealth` / `IntegrationHealth` real probes — those pages keep their Bogus stub for the Beta. Live health-checks against Wallabag/Vikunja are post-Beta polish.
- PostgreSQL — SQLite for the Beta. Block-4 Persistence work later swaps the EF Core provider; the abstraction is identical, so the swap is a one-line change.
- `Tags` field on persisted Capture — the classifier returns Tags, the UI doesn't display them yet, so the column is deferred. Block 4 makes the proper text[] vs join-table decision.
- Refit / Kiota typed REST client — deferred indefinitely (no live external consumer of the API; Blazor UI uses in-process services per ADR 0001 D2).

---

## Decisions

### D1: Demo flow = Web UI input + two-downstream routing (Wallabag, Vikunja)

One channel (Web UI), two downstreams chosen by the AI classifier. This is the smallest scope that actually validates the classifier's *routing* job — option-A (Wallabag-only) would have meant every capture goes to one place, giving zero signal on whether the AI is doing its job. With two downstreams, the demo has an obvious correctness check: paste a URL → does it go to Wallabag? Type a todo → does it go to Vikunja?

### D2: Persistence = SQLite + EF Core, run via `make run` on dev box

`Microsoft.EntityFrameworkCore.Sqlite` provider. SQLite file `flowhub.db` in the working directory by default; configurable via `ConnectionStrings:Default`. Same EF Core code paths as Postgres — Block 4 Persistence work later swaps the provider via one config change.

Migrations apply automatically at startup (dev convenience; Block 5 will move to a separate init step per the 12-factor rule).

### D3: UI integration = wire `ICaptureService` to EF Core, leave health pages on Bogus stub

`ICaptureService` interface stays in `FlowHub.Core`; new `EfCaptureService` implementation in `FlowHub.Persistence` reads/writes via `FlowHubDbContext`. The existing Dashboard cards (RecentCaptures, NeedsAttention) and Captures list/detail pages already consume `ICaptureService`, so they automatically display real data once the implementation swaps.

`SkillHealth` / `IntegrationHealth` keep their Bogus stub — orthogonal to architecture validation; live probes can come later.

### D4: Lifecycle = add `Completed` terminal state

`SkillRoutingConsumer` flow becomes:

1. Resolve `ISkillIntegration` by `MatchedSkill` (existing).
2. If `MatchedSkill = ""` → `MarkOrphanAsync` (existing, no change).
3. `MarkRoutedAsync(capture.Id)` (existing — "dispatch initiated").
4. **NEW:** `var result = await integration.HandleAsync(capture, ct)`.
5. **NEW:** `await _captureService.MarkCompletedAsync(capture.Id, result.ExternalRef)` on success.
6. On exception → rethrow → MassTransit retry policy → after exhaustion → `Fault<CaptureClassified>` → `LifecycleFaultObserver` → `MarkUnhandled`.

`Routed` becomes a *transient* state (dispatched but unconfirmed). `Completed` means "downstream accepted the write." Visual payoff in the Recent Captures grid: lifecycle chips transition through the full state machine.

### D5: AI provider = Anthropic Haiku 4.5

Default per ADR 0004. Slice C wired both adapters; the Beta uses `claude-haiku-4-5-20251001` via `Ai__Provider=Anthropic` + `Ai__Anthropic__ApiKey`. Sub-cent per classification at this model tier; `MaxOutputTokens=300`, `Temperature=0.2`, 10s HttpClient timeout (all from Slice C).

### D6: Implementation = vertical slice (Wallabag end-to-end first, then Vikunja, then AI)

Phases:

1. **Persistence** — EF Core wiring, `EfCaptureService`, migration, swap registration in `Program.cs`.
2. **Wallabag end-to-end** — `ISkillIntegration` shape, `WallabagSkillIntegration`, `MarkCompletedAsync` + the consumer transition, EventId namespace `4xxx` for skill startup logs. Test: paste URL → full lifecycle through to Wallabag.
3. **Vikunja** — symmetrical to Wallabag with three deltas (project ID required, Title-as-task-title, different endpoint shape).
4. **AI activation** — set Anthropic key in user-secrets, smoke-regression three captures (URL, todo, nonsense) to cover the routing matrix.

The biggest unknowns (EF wiring, full-lifecycle plumbing, real bearer auth, ISkillIntegration contract shape) all surface during phase 2. If something's wrong, it's caught before doubling the surface area in phase 3.

### D7: Skill-integration auth = bearer token directly, no OAuth2 dance

Wallabag's `/developer/new-token` page issues long-lived personal access tokens (similar shape to GitHub PATs). Vikunja's *Settings → API Tokens* page does the same. Beta stores them as bearer tokens in user-secrets and skips the OAuth2 password-grant + refresh flow entirely. Production deployment work (Block 5) can revisit if real OAuth2 becomes worth the cost.

### D8: Routing pattern = single `SkillRoutingConsumer`, no new event/consumer split

The integration HTTP call (~<1s POST) happens synchronously inside `SkillRoutingConsumer` rather than via a separate `SkillExecutionConsumer` triggered by a new `SkillRoutingRequested` event. Simpler; aligns with the current consumer shape; MassTransit's existing retry budget (500/2000/5000ms × 3) covers the call.

If integration calls grow into long-running streaming workloads in a later block, the split can be added then.

---

## Architecture

### File structure (Beta delta)

**Promote to active:**
- `source/FlowHub.Persistence/` — currently `.gitkeep` placeholder
- `source/FlowHub.Skills/` — currently `.gitkeep` placeholder

**Create — production:**
- `source/FlowHub.Persistence/FlowHub.Persistence.csproj`
- `source/FlowHub.Persistence/FlowHubDbContext.cs`
- `source/FlowHub.Persistence/Entities/CaptureEntity.cs`
- `source/FlowHub.Persistence/EfCaptureService.cs` (`ICaptureService` adapter)
- `source/FlowHub.Persistence/PersistenceServiceCollectionExtensions.cs` (`AddFlowHubPersistence`)
- `source/FlowHub.Persistence/Migrations/<timestamp>_Initial.cs` (EF Core generated)
- `source/FlowHub.Skills/FlowHub.Skills.csproj`
- `source/FlowHub.Skills/Wallabag/WallabagSkillIntegration.cs`
- `source/FlowHub.Skills/Vikunja/VikunjaSkillIntegration.cs`
- `source/FlowHub.Skills/SkillsServiceCollectionExtensions.cs` (`AddFlowHubSkills`)
- `source/FlowHub.Skills/SkillsBootLogger.cs` (`IHostedService` for the 4020/4021 startup log, mirroring Slice-C's `AiBootLogger`)

**Create — tests:**
- `tests/FlowHub.Persistence.Tests/FlowHub.Persistence.Tests.csproj`
- `tests/FlowHub.Persistence.Tests/EfCaptureServiceTests.cs` (in-memory EF Core provider)
- `tests/FlowHub.Skills.Tests/FlowHub.Skills.Tests.csproj`
- `tests/FlowHub.Skills.Tests/WallabagSkillIntegrationTests.cs` (mocked HttpClient)
- `tests/FlowHub.Skills.Tests/VikunjaSkillIntegrationTests.cs` (mocked HttpClient)
- `tests/FlowHub.Skills.IntegrationTests/FlowHub.Skills.IntegrationTests.csproj` — trait-gated `[Trait("Category","BetaSmoke")]`
- `tests/FlowHub.Skills.IntegrationTests/WallabagLiveTests.cs`
- `tests/FlowHub.Skills.IntegrationTests/VikunjaLiveTests.cs`

**Modify:**
- `Directory.Packages.props` — add `Persistence` ItemGroup (EF Core + Sqlite + EF Core InMemory for tests) and `Skills` ItemGroup (`Microsoft.Extensions.Http` if not already pulled in)
- `FlowHub.slnx` — register the two new production projects + three new test projects
- `source/FlowHub.Core/Captures/ICaptureService.cs` — add `MarkCompletedAsync(Guid id, string? externalRef, CancellationToken ct)`
- `source/FlowHub.Core/Skills/ISkillIntegration.cs` — confirm or add the `Task<SkillResult> HandleAsync(Capture capture, CancellationToken ct)` shape; add new `SkillResult` record
- `source/FlowHub.Web/Captures/SkillRoutingConsumer.cs` — wire steps 4-5 above (call integration, mark completed)
- `source/FlowHub.Web/Captures/CaptureServiceStub.cs` — **keep**, but only as a bUnit-test fixture. bUnit component tests render UI in isolation and don't need real persistence; injecting a controlled in-memory `ICaptureService` is the cheapest path. Production DI registration switches to `EfCaptureService`; the stub stays out of `Program.cs`.
- `source/FlowHub.Web/Program.cs` — remove `AddSingleton<ICaptureService, CaptureServiceStub>()`; add `AddFlowHubPersistence(builder.Configuration)` + `AddFlowHubSkills(builder.Configuration)` calls
- `Makefile` — add `make test-beta` target running `[Trait("Category","BetaSmoke")]`
- `docs/ai-usage.md` — append "Beta MVP" section with retrospective on the vertical slice
- `CHANGELOG.md` — Beta MVP entry under `[Unreleased]`

### Configuration surface

```jsonc
// All values live in dotnet user-secrets (UserSecretsId already set in Slice C)
{
  "ConnectionStrings": {
    "Default": "Data Source=flowhub.db"   // optional; this is the default when unset
  },
  "Ai": {                                  // unchanged from Slice C
    "Provider": "Anthropic",
    "Anthropic": {
      "ApiKey": "<sk-ant-…>"
    }
  },
  "Skills": {
    "Wallabag": {
      "BaseUrl": "https://wallabag.home.freaxnx01.ch",
      "ApiToken": "<bearer>"
    },
    "Vikunja": {
      "BaseUrl": "https://vikunja.home.freaxnx01.ch",
      "ApiToken": "<bearer>",
      "DefaultProjectId": 42
    }
  }
}
```

`AddFlowHubSkills` mirrors Slice-C's `AddFlowHubAi` shape: silent fallback if `Skills:<X>:BaseUrl` or `:ApiToken` is missing — the skill registers as a no-op stub and `SkillsBootLogger` writes `EventId 4021 SkillNotConfigured`. Operator running with no Wallabag config sees URL captures land in `Orphan` because the Wallabag integration short-circuits.

### Data model (Beta-scoped)

```csharp
public sealed class CaptureEntity
{
    public Guid Id { get; set; }
    public string Content { get; set; } = "";
    public string Source { get; set; } = "";        // ChannelKind as string
    public string Stage { get; set; } = "";         // LifecycleStage as string
    public DateTimeOffset CreatedAt { get; set; }
    public string? MatchedSkill { get; set; }
    public string? Title { get; set; }              // populated by AiClassifier
    public string? FailureReason { get; set; }
    public string? ExternalRef { get; set; }        // Wallabag entry ID / Vikunja task ID
}
```

Indexes: `Stage` (Dashboard "needs attention"), `CreatedAt DESC` (Recent Captures cursor). Migration ships these.

### Lifecycle state machine (after Beta)

```
Raw → Classified → Routed → Completed   (happy path)
                         ↘
                          Unhandled       (integration failure after retries)
        ↘
         Orphan                            (no skill match)
```

### EventId namespace

Extends ADR 0003 / ADR 0004 conventions:

- `1xxx` Pipeline (Slice B)
- `2xxx` Skills runtime: `2010` SkillIntegrationCalled (Debug), `2011` SkillIntegrationSucceeded (Information), `2012` SkillIntegrationFailed (Warning, before retry)
- `3xxx` AI (Slice C)
- `4xxx` Skills startup: `4020` SkillRegistered, `4021` SkillNotConfigured

### `ISkillIntegration` contract

```csharp
public interface ISkillIntegration
{
    string Name { get; }
    Task<SkillResult> HandleAsync(Capture capture, CancellationToken ct);
}

public sealed record SkillResult(
    bool Success,
    string? ExternalRef = null,
    string? FailureReason = null);
```

If the existing `ISkillIntegration` returns something different, the implementation plan adapts to keep the routing-consumer change minimal. Goal is one method per skill.

---

## Error handling

| Failure mode | Handler | Terminal state |
|---|---|---|
| AI provider unreachable / throws | `AiClassifier` catch-all → `KeywordClassifier` floor | Capture still classified (`Title=null`), `EventId 3010` |
| `MatchedSkill = ""` (no skill match) | `SkillRoutingConsumer` short-circuit | `Orphan` |
| `ISkillIntegration.HandleAsync` throws | MassTransit retry (500/2000/5000ms × 3 per ADR 0003) → `Fault<CaptureClassified>` → `LifecycleFaultObserver` | `Unhandled` (with `FailureReason`) |
| EF Core write failure | propagates up; consumer retry takes effect; eventual `Unhandled` if persistent | `Unhandled` |
| Migration failure at startup | app fails to boot; visible in stdout | (no capture state — operator sees boot crash) |
| Skill not configured (`BaseUrl` or `ApiToken` missing) | no-op stub registered; capture short-circuits; `EventId 4021` at boot | `Orphan` |

The Dashboard's existing **Needs Attention** card surfaces `Orphan` + `Unhandled` counts — automatic visual surface for failures. Operator drills into Capture detail to see `FailureReason`. Stdout logs catch the rest.

---

## Testing strategy

| Layer | What's tested | Framework |
|---|---|---|
| **Unit (Persistence)** | `EfCaptureService` against `Microsoft.EntityFrameworkCore.InMemory` provider — submit, mark-classified, mark-routed, mark-completed, mark-orphan, mark-unhandled, GetRecent ordering, GetById not-found | xunit + FluentAssertions |
| **Unit (Skills)** | `WallabagSkillIntegration` + `VikunjaSkillIntegration` against mocked `HttpMessageHandler` (e.g. `RichardSzalay.MockHttp`) — assert request URL, headers (`Authorization: Bearer …`), body shape; assert response handling for 200/201, 401, 5xx, timeout | xunit + FluentAssertions + RichardSzalay.MockHttp |
| **Pipeline (existing — extended)** | `SkillRoutingConsumer` already harness-tested. **Extend:** verify `MarkCompletedAsync` is called on integration success and the integration receives the right Capture | MassTransit.Testing |
| **API integration (existing)** | 17 tests in `FlowHub.Api.IntegrationTests` — should stay green. `WebApplicationFactory<Program>` may need an in-memory EF Core override per test (or per-test SQLite file); decide during impl | `Microsoft.AspNetCore.Mvc.Testing` |
| **Beta live (new, opt-in)** | One trait-gated test per skill that runs the full vertical: submit a Capture → wait for `Completed` → assert Wallabag entry / Vikunja task exists. Operator-only, gated by `Skills__*__ApiToken` env vars same as `Ai__*__ApiKey`-gated Slice-C live tests | `[SkippableFact]` + `[Trait("Category","BetaSmoke")]` |

### `make` targets after Beta

```make
test       — Category!=AI && Category!=BetaSmoke   # default suite, ~120+ tests
test-ai    — Category=AI                            # Slice-C live tests (4)
test-beta  — Category=BetaSmoke                     # Beta live verticals (2)
```

CI runs only `make test`. Operator runs `test-ai` and `test-beta` on demand when keys are loaded.

---

## Demo path (acceptance)

End-of-Phase-4 manual smoke test:

1. `dotnet user-secrets` set: `Ai:Provider=Anthropic`, `Ai:Anthropic:ApiKey`, `Skills:Wallabag:BaseUrl`, `Skills:Wallabag:ApiToken`, `Skills:Vikunja:BaseUrl`, `Skills:Vikunja:ApiToken`, `Skills:Vikunja:DefaultProjectId`.
2. `make run`. Boot log shows `EventId 3020 AiProviderRegistered (provider=Anthropic, model=claude-haiku-4-5-…)` and `EventId 4020 SkillRegistered` for both Wallabag and Vikunja.
3. Browser to `http://localhost:5070/`. Recent Captures grid is empty.
4. AppBar quick-capture: paste `https://en.wikipedia.org/wiki/Hexagonal_architecture` → submit. Within ~3s the grid shows: `Stage=Completed, MatchedSkill=Wallabag, Title="Hexagonal architecture"` (or close). Wallabag's homelab UI shows the new entry.
5. AppBar: type `todo: review Block 4 prep tomorrow` → submit. Within ~3s: `Stage=Completed, MatchedSkill=Vikunja, Title="Review Block 4 prep tomorrow"`. Vikunja's project-42 page shows the new task.
6. AppBar: type `asdfqwerty` → submit. Within ~3s: `Stage=Orphan, MatchedSkill=""`. Recent Captures shows it; Needs Attention card increments orphan count.
7. `pkill -f FlowHub.Web`. `make run`. All three captures still visible in Recent Captures (SQLite persistence works).

If any step fails, the Beta hasn't validated the architecture and the failure surfaces a real bug in one of: persistence, skill integration auth, classifier wiring, or lifecycle plumbing.

---

## Out of scope (explicitly)

- Telegram channel — fast-follow; not on Beta path
- Authentik / OIDC — Block 5
- Dockerfile / docker-compose with FlowHub services running — Block 5
- PostgreSQL — Block 4 swaps the EF Core provider; Beta is SQLite
- `Tags` field on `CaptureEntity` — Block 4 decision (text[] vs join table)
- Real `SkillHealth` / `IntegrationHealth` probes — post-Beta polish
- Refit / Kiota typed REST client — deferred indefinitely (no live external API consumer)
- Per-skill modules (`source/FlowHub.Wallabag/`, `source/FlowHub.Vikunja/`) — Beta keeps both in `FlowHub.Skills/`; module split is a Block 5+ concern
- "Click-through to external item" link in UI (open Wallabag entry / Vikunja task from Capture detail) — `ExternalRef` is persisted but the UI link is a Beta-polish if time permits
- Auto-retry on Orphan from the Dashboard — already works via UC-11 (`POST /api/v1/captures/{id}/retry`); Beta inherits this

---

## References

- Brainstorming dialogue: this session (Q1–Q5 + design sections 1–4)
- ADR 0001 — Frontend Render Mode: `docs/adr/0001-frontend-render-mode-and-architecture.md`
- ADR 0002 — Modular Monolith + Async Comm: `docs/adr/0002-service-architecture-and-async-communication.md`
- ADR 0003 — Async Pipeline (retry budgets, EventId namespacing): `docs/adr/0003-async-pipeline.md`
- ADR 0004 — AI Integration: `docs/adr/0004-ai-integration-in-services.md`
- DB sketch: `docs/design/db/entities.md`
- Use cases (UC-08..UC-11): `docs/spec/use-cases.md`
- NfAs (NF-09..NF-13): `docs/spec/use-cases.md`
- Sequence diagrams: `docs/design/sequences/capture-enrichment.md`, `docs/design/sequences/skill-routing.md`
- Slice C AI integration: `docs/superpowers/specs/2026-05-03-slice-c-ai-integration-design.md`, `docs/superpowers/plans/2026-05-03-slice-c-ai-integration.md`
- Wallabag API: https://doc.wallabag.org/developer/api/
- Vikunja API: https://vikunja.io/docs/api/
- `Microsoft.EntityFrameworkCore.Sqlite`: https://learn.microsoft.com/en-us/ef/core/providers/sqlite/
