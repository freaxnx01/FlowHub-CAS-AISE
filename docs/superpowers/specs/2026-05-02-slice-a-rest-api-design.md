---
title: REST API (Slice A) — Brainstorming Spec
status: Draft (awaiting user review)
date: 2026-05-02
block: 3 — Service · Nachbereitung
related:
  - docs/adr/0001-frontend-render-mode-and-architecture.md
  - docs/adr/0002-service-architecture-and-async-communication.md
  - docs/adr/0003-async-pipeline.md
  - docs/design/api/api-surface.md
  - docs/superpowers/specs/2026-04-30-async-pipeline-design.md
---

# REST API (Slice A) — Brainstorming Spec

## Context

ADR 0001 said the Blazor UI calls domain services in-process; the REST API is for **non-UI consumers** (Telegram bot, automation, future mobile clients). ADR 0002 placed the API in `source/FlowHub.Api/`, co-hosted in the `FlowHub.Web` process. The Block 3 Vorbereitung produced `docs/design/api/api-surface.md` with **6 accepted decisions (D1–D6)** and a 7-endpoint catalogue.

Slice B (async pipeline) shipped on 2026-05-02 (commit `5e342fc` on main). Slice A is the next deliverable in Block 3 Nachbereitung; PVA on 2026-05-23 leaves ~3 weeks for Slices A + C + D.

This spec narrows the api-surface sketch to a **Captures-only minimum viable surface**, fills the gaps the sketch left open (cursor encoding, service-layer query shape, test project structure), and locks the implementation contract before plan-writing.

## Scope

**In:**

- New library `source/FlowHub.Api/` co-hosted in `FlowHub.Web`
- 4 endpoints under `/api/v1/captures`:
  - `POST /api/v1/captures` — submit
  - `GET /api/v1/captures` — list with `stage`, `source`, `limit`, `cursor` filters
  - `GET /api/v1/captures/{id}` — fetch single
  - `POST /api/v1/captures/{id}/retry` — re-publish `CaptureCreated`
- FluentValidation at the boundary
- ProblemDetails (RFC 9457) for all 4xx/5xx, with type URIs pointing at `docs/problems/<slug>.md`
- 3 initial problem docs: `validation`, `capture-not-found`, `capture-not-retryable`
- OpenAPI document at `/openapi/v1.json`, Scalar UI at `/scalar`
- New service-layer query: `ICaptureService.ListAsync(CaptureFilter, ct)` returning `CapturePage`
- New types in `FlowHub.Core/Captures/`: `CaptureFilter`, `CapturePage`, `CaptureCursor`
- `ChannelKind.Api` enum value (D1)
- `CaptureServiceStub.ListAsync` implemented over the in-memory store
- New test project `tests/FlowHub.Api.IntegrationTests/` using `WebApplicationFactory<FlowHub.Web.Program>`
- 14 integration tests covering the four endpoints + ProblemDetails wire format
- DevAuthHandler bypass remains the auth path in Development

**Out (deferred):**

- OIDC against Authentik — Block 5
- `GET /skills`, `GET /skills/{name}`, `GET /integrations`, `GET /integrations/{name}` — postponed to a v2 surface; the Blazor Dashboard already exposes this data
- Stretch filters: `createdAfter`, `createdBefore`, `q` (substring match)
- Real persistence — Block 4 (`ListAsync` runs over the in-memory `CaptureServiceStub`)
- Bruno collections under `bruno/api/` — YAGNI; integration tests cover the wire format more rigorously
- Rate limiting, ETag / `If-None-Match`
- Roles / authorisation policies (single-operator)
- `DELETE /api/v1/captures/{id}` (rejected per D6; captures are append-only history)

## Decisions (locked in brainstorming)

Pre-existing decisions inherited verbatim from `docs/design/api/api-surface.md`:

| # | Decision | Source |
|---|---|---|
| **D1** | `ChannelKind.Api` enum value added; clients submit captures with `source: "Api"` | api-surface |
| **D2** | API lives in `source/FlowHub.Api/` library, co-hosted in `FlowHub.Web` via `AddFlowHubApi` + `MapFlowHubApi` extensions | api-surface |
| **D3** | `POST /api/v1/captures/{id}/retry` returns **202 Accepted** (async republish) | api-surface |
| **D4** | Day-one filter set on `GET /api/v1/captures`: `stage`, `source`, `limit`, `cursor` only | api-surface |
| **D5** | ProblemDetails `type` URIs resolve to `https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/<slug>.md` | api-surface |
| **D6** | No `DELETE /api/v1/captures/{id}` in v1 (append-only history) | api-surface |

New decisions locked in this brainstorm:

| # | Decision | Rationale |
|---|---|---|
| **D7** | Slice A scope = **Captures-only**, 4 endpoints. `GET /skills`, `GET /skills/{name}`, `GET /integrations`, `GET /integrations/{name}` are dropped from this slice. | The Captures resource exercises the full rubric story (REST, ProblemDetails, OpenAPI, FluentValidation) end-to-end. The read-only Skills/Integrations endpoints duplicate the Blazor Dashboard cards and add nothing the rubric will reward. ~1.5 days reclaimed for Slices C and D. |
| **D8** | Test project = **new `tests/FlowHub.Api.IntegrationTests/`** using `WebApplicationFactory<FlowHub.Web.Program>`. | The Block 3 vault checklist explicitly names "Component-Tests für API-Endpoints (`Microsoft.AspNetCore.Mvc.Testing`)". A new project keeps API integration tests separate from Blazor bUnit tests; reused in Block 4 for EF Core integration tests. |
| **D9** | Cursor format = **base64-URL-encoded JSON** of `(CreatedAt, Id)`. | Operators can decode in their head with `base64 -d \| jq`; ~80 bytes per cursor; idiomatic .NET (`JsonSerializer` + `Base64UrlEncoder`). Compact binary and JWT alternatives are over-engineered for a single-tenant single-operator API. |
| **D10** | Service interface extension = **single `Task<CapturePage> ListAsync(CaptureFilter, ct)`** in `FlowHub.Core`. Filter and page records co-located. | Grows cleanly to the deferred filters (`CreatedAfter/Before/Q`). Maps directly onto an `IQueryable<Capture>` chain in Block 4 EF Core. The cursor record lives in `FlowHub.Core` (not `FlowHub.Api`) because the Core service interface owns query semantics. |
| **D11** | Bruno collections = **deferred** (not in Slice A). | Integration tests cover the wire format programmatically. No active Bruno usage in the repo today. Add when a real consumer needs interactive exploration. |

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│  FlowHub.Web (single process, Modular Monolith)         │
│                                                         │
│  Program.cs                                             │
│    builder.Services.AddFlowHubApi();                    │
│    app.MapFlowHubApi();    // /api/v1/captures group   │
│                                                         │
│  ┌────────────────────────────┐                         │
│  │ FlowHub.Api (library, D2)  │                         │
│  │                            │                         │
│  │   CaptureEndpoints         │                         │
│  │     ├─ POST /captures      │ ── ICaptureService ──┐  │
│  │     ├─ GET  /captures      │   .SubmitAsync       │  │
│  │     ├─ GET  /captures/{id} │   .ListAsync         │  │
│  │     └─ POST /captures/{id} │   .GetByIdAsync      │  │
│  │           /retry           │                      │  │
│  │   Validation / Pagination /│                      │  │
│  │   ProblemDetails           │                      │  │
│  └────────────────────────────┘                      │  │
│                                                       │  │
│   ┌─────────────────────────────────────────────────┐ │  │
│   │ FlowHub.Web.Stubs / FlowHub.Core (Block 2/3 SB) │◄┘  │
│   │                                                 │    │
│   │  CaptureServiceStub : ICaptureService           │    │
│   │  IBus → MassTransit pipeline (Slice B)          │    │
│   └─────────────────────────────────────────────────┘    │
│                                                         │
└─────────────────────────────────────────────────────────┘

   POST /captures ──► validator ──► Submit ──► publish CaptureCreated ──► 201
   GET  /captures ──► CaptureFilter ──► ListAsync ──► 200 {items, nextCursor}
   GET  /captures/{id} ──► GetById ──► 200 / 404
   POST /captures/{id}/retry ──► stage check ──► re-publish + reset ──► 202 / 409 / 404
```

## Components

### New types in `FlowHub.Core/Captures/`

```csharp
// ChannelKind.cs (extended)
public enum ChannelKind { Telegram, Web, Api }

// CaptureFilter.cs (new)
public sealed record CaptureFilter(
    IReadOnlyList<LifecycleStage>? Stages,
    ChannelKind? Source,
    int Limit,
    CaptureCursor? Cursor);

// CapturePage.cs (new)
public sealed record CapturePage(
    IReadOnlyList<Capture> Items,
    CaptureCursor? Next);

// CaptureCursor.cs (new)
public sealed record CaptureCursor(DateTimeOffset CreatedAt, Guid Id)
{
    public string Encode() { /* JSON → Base64Url */ }
    public static CaptureCursor Decode(string token) { /* Base64Url → JSON; throws FormatException on failure */ }
}

// ICaptureService.cs (extended — two new methods)
Task<CapturePage> ListAsync(CaptureFilter filter, CancellationToken cancellationToken);
Task ResetForRetryAsync(Guid id, CancellationToken cancellationToken);
```

`ResetForRetryAsync` transitions a capture to `LifecycleStage.Raw` and clears `FailureReason` (the retry endpoint then re-publishes `CaptureCreated` to re-enter the pipeline). Implementation in `CaptureServiceStub` reuses the existing `ReplaceCapture` helper. Throws `KeyNotFoundException` for unknown ids — same convention as the existing mark methods.

`Encode()` / `Decode()` use `JsonSerializer` with the same options as the API (`JsonStringEnumConverter` registered globally).

### New files in `FlowHub.Api/`

| File | Responsibility |
|---|---|
| `ServiceCollectionExtensions.cs` | `AddFlowHubApi(this IServiceCollection)` — registers validators, ProblemDetails options, OpenAPI document, JsonStringEnumConverter |
| `Endpoints/CaptureEndpoints.cs` | `MapFlowHubApi(this IEndpointRouteBuilder)` — endpoint group at `/api/v1/captures`, `[Authorize]`, `WithTags("Captures")`, `Produces<T>(...)`, `WithName(...)` |
| `Requests/CreateCaptureRequest.cs` | `(string Content, ChannelKind Source)` |
| `Validation/CreateCaptureRequestValidator.cs` | `Content`: required, 1–8192 chars; `Source`: required, must be a known enum |
| `Pagination/CursorBinder.cs` | Custom model binder: parses `?cursor=…` → `CaptureCursor?`; throws `BadHttpRequestException` (mapped to ProblemDetails) on malformed input |
| `ProblemDetails/FlowHubProblemDetailsFactory.cs` | Sets `type` to `https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/<slug>.md`; injects `traceId` from `Activity.Current?.Id ?? Activity.Current?.RootId` |

### Modified

- `source/FlowHub.Web/Stubs/CaptureServiceStub.cs` — implements `ListAsync` (in-memory LINQ + cursor seek). The cursor seek is `OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id)` then `SkipWhile(c => c.CreatedAt > cursor.CreatedAt || (c.CreatedAt == cursor.CreatedAt && c.Id.CompareTo(cursor.Id) >= 0))`.
- `source/FlowHub.Web/Program.cs` — adds `AddFlowHubApi()`, `AddOpenApi()`, `MapFlowHubApi()`, Scalar middleware.
- `FlowHub.slnx` — adds `FlowHub.Api` and `FlowHub.Api.IntegrationTests`.

### New problem docs in `docs/problems/`

- `validation.md` — for FluentValidation failures (400)
- `capture-not-found.md` — for unknown id (404)
- `capture-not-retryable.md` — for retry on terminal/non-retryable stage (409)

## Data flow

### `POST /api/v1/captures`

```
client → bearer token → endpoint group [Authorize]
       → CreateCaptureRequestValidator
       → if invalid: 400 + validation problem
       → ICaptureService.SubmitAsync(content, ChannelKind.Api, ct)
         (this publishes CaptureCreated on the bus per Slice B)
       → return 201 Created
         Location: /api/v1/captures/{id}
         Content-Type: application/json
         body: full Capture (Stage = Raw, MatchedSkill = null)
```

### `GET /api/v1/captures`

```
client → bearer token → endpoint
       → query string parsed into CaptureFilter
         (stage = csv → IReadOnlyList<LifecycleStage>;
          source = single enum;
          limit = int [1, 200], default 50;
          cursor = CursorBinder)
       → if cursor malformed: 400 + validation problem
       → ICaptureService.ListAsync(filter, ct)
       → return 200 OK
         body: { items: Capture[], nextCursor: string | null }
```

`nextCursor` is null when fewer than `Limit` items remain.

### `GET /api/v1/captures/{id}`

```
client → bearer → endpoint
       → ICaptureService.GetByIdAsync(id, ct)
       → null → 404 + capture-not-found problem
       → otherwise → 200 + Capture body
```

### `POST /api/v1/captures/{id}/retry`

```
client → bearer → endpoint
       → ICaptureService.GetByIdAsync(id, ct)
       → null → 404 + capture-not-found
       → if stage ∈ {Raw, Classified, Routed, Completed} → 409 + capture-not-retryable
         (only Orphan / Unhandled are retryable)
       → reset capture: stage = Raw, FailureReason = null, MatchedSkill stays
         (new method on ICaptureService: ResetForRetryAsync(Guid, ct))
       → publish CaptureCreated for the same id (re-enters the pipeline)
       → return 202 Accepted with the updated Capture body
```

A new mark method `ResetForRetryAsync(Guid id, CancellationToken ct)` lands on `ICaptureService` for this — it transitions stage to `Raw` and clears `FailureReason`. Implementation in `CaptureServiceStub` reuses the `ReplaceCapture` helper.

## Error handling

All 4xx/5xx return `application/problem+json` with this shape:

```json
{
  "type": "https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/<slug>.md",
  "title": "<human-readable>",
  "status": <int>,
  "instance": "<request path>",
  "traceId": "00-<trace-id>-<span-id>-01",
  "errors": { "field": ["message", ...] }
}
```

| Status | Slug | Trigger |
|---|---|---|
| 400 | `validation` | FluentValidation failure or malformed cursor |
| 401 | (none — default challenge) | missing / invalid bearer |
| 404 | `capture-not-found` | unknown capture id |
| 409 | `capture-not-retryable` | retry against stage ∈ {Raw, Classified, Routed, Completed} |

`traceId` always populated from `Activity.Current?.Id`. `errors` only present on 400 (FluentValidation flatten).

## Testing

`tests/FlowHub.Api.IntegrationTests/FlowHub.Api.IntegrationTests.csproj` references `Microsoft.AspNetCore.Mvc.Testing` (new central package version) plus the existing `xunit` + `FluentAssertions` stack. Tests use `WebApplicationFactory<FlowHub.Web.Program>` so the API is exercised via real HTTP roundtrips against the in-process kestrel pipeline.

DevAuthHandler picks up automatically (the factory inherits `ASPNETCORE_ENVIRONMENT=Development`); tests don't need to forge bearer tokens.

### 14 integration test cases

Organised in 4 fact classes mirroring the endpoint group:

**`SubmitCaptureTests`**
1. `Post_ValidContent_Returns201WithLocationAndCapture`
2. `Post_EmptyContent_Returns400WithValidationProblem`
3. `Post_ContentOver8192_Returns400`
4. `Post_UnknownSource_Returns400`

**`ListCapturesTests`**
5. `Get_NoFilter_ReturnsFirstPageWithNextCursor`
6. `Get_WithCursor_ReturnsNextPageNoOverlap`
7. `Get_StageOrphanOrUnhandled_FiltersCorrectly`
8. `Get_SourceApi_FiltersCorrectly`
9. `Get_MalformedCursor_Returns400`

**`GetCaptureByIdTests`**
10. `Get_KnownId_Returns200WithCapture`
11. `Get_UnknownId_Returns404WithCaptureNotFoundProblem`

**`RetryCaptureTests`**
12. `Retry_OrphanCapture_Returns202AndResetsStageToRaw`
13. `Retry_CompletedCapture_Returns409WithCaptureNotRetryableProblem`
14. `Retry_UnknownId_Returns404`

Plus a single `ProblemDetailsFormatTests` test asserting the wire format keys (`type`, `title`, `status`, `instance`, `traceId`) on a 400 response — covers ProblemDetailsFactory wiring.

Test naming follows `Method_State_Behaviour` per CLAUDE.md (CA1707 already suppressed in the test csproj template).

### Test data seeding

`CaptureServiceStub` already seeds 12 Bogus-generated captures with diverse stages and sources. Tests submit additional captures via the API itself (`POST /captures`) to exercise the full happy path; the seeded captures cover stage/source filter cases without requiring extra fixture setup.

## Open questions / deferred work

- **`ChannelKind.Api` migration risk:** when `FlowHub.Persistence` lands in Block 4, the enum value needs an EF Core migration. Documented here so it's not forgotten; one-line `[Migration]` change.
- **Cursor stability under inserts:** new captures submitted between page 1 and page 2 appear at top on page 1 but don't shift cursor semantics — pages remain stable for the slice the cursor describes. Acceptable for Block 3 single-operator usage.
- **Throughput limits on `ListAsync`:** in-memory store is bounded to ~12 captures (Bogus seed) plus whatever the integration tests submit. EF Core in Block 4 brings real volumes; revisit limit cap and add `EXPLAIN ANALYZE` review.
- **Skills / Integrations endpoints:** deliberately deferred per D7. Will land in Slice A v2 (post-Block-4 likely) once a real consumer needs them programmatically.
- **OpenAPI versioning:** `Microsoft.AspNetCore.OpenApi` ships per-document support; we register exactly one document `v1` for `/api/v1/`. When `/api/v2/` lands (breaking changes), add a second document.
- **`docs/problems/` is grown on demand.** Ship 3 today; new types added when their endpoints land.

## References

- ADR 0001: `docs/adr/0001-frontend-render-mode-and-architecture.md`
- ADR 0002: `docs/adr/0002-service-architecture-and-async-communication.md`
- ADR 0003: `docs/adr/0003-async-pipeline.md`
- API surface sketch: `docs/design/api/api-surface.md`
- Slice B brainstorming spec: `docs/superpowers/specs/2026-04-30-async-pipeline-design.md`
- Block 3 Nachbereitung: `vault/Blöcke/03 Service/03 Service - c) Nachbereitung.md`
- Bewertungskriterien: `vault/Organisation/Bewertungskriterien.md`
- ASP.NET Core Minimal API: https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis
- Scalar API documentation UI: https://github.com/scalar/scalar
- RFC 9457 (ProblemDetails): https://www.rfc-editor.org/rfc/rfc9457
