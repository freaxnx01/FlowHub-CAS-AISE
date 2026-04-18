# FlowHub REST API — Surface Sketch

- **Status:** Accepted (sketch + 6 decisions)
- **Date:** 2026-04-18
- **Block:** Block 3 (Services) — Vorbereitung
- **Scope:** Endpoint catalogue, resource shapes, auth, error model for `source/FlowHub.Api/`.
- **Related:** ADR 0001 (UI does not consume this API), ADR 0002 (async pipeline + REST surface decisions).

This is the Block 3 API-surface sketch. Six decisions (D1–D6) are recorded at the bottom; the endpoint catalogue above already reflects them. Scaffolding happens in Block 3 Nachbereitung (see *Implementation checklist*).

---

## Context

Per ADR 0001 Decision 2, the Blazor UI reaches domain services in-process; this REST API is for **non-UI consumers**:

- `FlowHub.Telegram` — bot submits captures, reads back status
- External automation / CLI — retries, dashboards
- Webhook receivers from upstream services (future)
- Future mobile / third-party clients

Per ADR 0002 Decision 4, the API lives in `source/FlowHub.Api/`, uses Minimal API, FluentValidation, ProblemDetails (RFC 9457), Scalar at `/scalar`, and is URL-versioned from day one (`/api/v1/...`).

## Non-goals

- No endpoints serving the Blazor UI's own data needs.
- No custom auth scheme — OIDC cookie or bearer only (see "Authentication").
- No GraphQL / OData / gRPC — REST only in this surface.
- No public-facing / unauthenticated endpoints (FlowHub is single-operator behind Authentik).

## Data model (reminder, from `FlowHub.Core`)

| Type | Fields |
|---|---|
| `Capture` | `Id: Guid`, `Source: ChannelKind`, `Content: string`, `CreatedAt: DateTimeOffset`, `Stage: LifecycleStage`, `MatchedSkill: string?`, `FailureReason: string?` |
| `ChannelKind` | `Telegram`, `Web`, `Api` (see D1) |
| `LifecycleStage` | `Raw`, `Classified`, `Routed`, `Completed`, `Orphan`, `Unhandled` |
| `SkillHealth` | `Name: string`, `Status: HealthStatus`, `RoutedToday: int` |
| `IntegrationHealth` | `Name: string`, `Status: HealthStatus`, `LastWriteAt: DateTimeOffset?`, `LastWriteDuration: TimeSpan?` |
| `HealthStatus` | `Healthy`, `Degraded`, `Unhealthy` |
| `FailureCounts` | (aggregate over `Orphan` + `Unhandled` — exact shape TBD) |

Wire format: JSON, `application/json`. `DateTimeOffset` serialises as ISO-8601 with offset. `TimeSpan` as ISO-8601 duration string (`PT1.234S`). Enums serialised as the string name (`"Classified"`, not `1`) — controlled via `JsonStringEnumConverter`.

---

## Base

| Aspect | Value |
|---|---|
| Base URL | `/api/v1/` (prefix applied via endpoint-group convention) |
| Media type | `application/json; charset=utf-8` |
| Error type | `application/problem+json` per RFC 9457 |
| Auth | `Authorization: Bearer <token>` (OIDC JWT from Authentik) |
| Versioning | URL-prefix (`/api/v1/`, later `/api/v2/` for breaking changes) |
| Docs | Scalar UI at `/scalar`; OpenAPI JSON at `/openapi/v1.json` |

---

## Captures resource

Primary resource. Covers the Telegram bot path, the automation retry path, and future mobile clients.

### `POST /api/v1/captures`

Submit a new Capture. Equivalent to `ICaptureService.SubmitAsync(...)` called by the API channel.

**Request:**
```json
{ "content": "https://example.com/article", "source": "Api" }
```

| Field | Rules |
|---|---|
| `content` | required, `MinLength=1`, `MaxLength=8192` |
| `source` | required, must be a known `ChannelKind`; API clients typically send `"Api"`, `"Telegram"` is reserved for the bot's own channel |

FluentValidation rules live in `FlowHub.Api/Validation/CreateCaptureValidator.cs`.

**Responses:**
- `201 Created` with `Location: /api/v1/captures/{id}` and the full `Capture` in the body (initial `Stage` = `Raw`). The enrichment pipeline kicks off asynchronously (ADR 0002); clients poll `GET /api/v1/captures/{id}` for progress.
- `400 Bad Request` — ProblemDetails with field-level validation errors.
- `401 Unauthorized` — missing / invalid token.

### `GET /api/v1/captures`

List Captures with filters. Replaces the need for ad-hoc database queries from automation scripts.

**Query parameters:**

| Param | Type | Default | Notes |
|---|---|---|---|
| `stage` | `LifecycleStage` or comma-list | all | e.g. `?stage=Orphan,Unhandled` |
| `source` | `ChannelKind` | all | |
| `createdAfter` | ISO-8601 datetime | — | lower bound, inclusive |
| `createdBefore` | ISO-8601 datetime | — | upper bound, exclusive |
| `q` | string | — | case-insensitive substring match on `Content` |
| `limit` | int (1–200) | 50 | page size |
| `cursor` | opaque string | — | pagination token from previous response |

**Response shape:**
```json
{
  "items": [ /* array of Capture */ ],
  "nextCursor": "eyJjcmVhdGVkQXQiOiIyMDI2LTA0LTEy..."
}
```

`nextCursor` is null when no more pages. Cursor-based to keep pages stable under inserts.

Status codes: `200`, `400` (invalid filter combination), `401`.

### `GET /api/v1/captures/{id}`

Fetch a single Capture by id.

Status codes: `200`, `404` (ProblemDetails with `title=Capture not found`), `401`.

### `POST /api/v1/captures/{id}/retry`

Re-publish a `CaptureCreated` event for a Capture that landed in `Orphan` or `Unhandled`. Idempotency is via the id — internal bus dedupes consecutive events.

**Request body:** empty (or `{}`).

**Responses:**
- `202 Accepted` with the updated Capture (`Stage` typically transitions back to `Raw`, `FailureReason` cleared).
- `404 Not Found` if the id does not exist.
- `409 Conflict` if the current `Stage` is not retryable (e.g. `Completed`, `Classified`, `Routed`) — ProblemDetails explains which stages are valid for retry.
- `401 Unauthorized`.

---

## Skills resource

Read-only surface over `ISkillRegistry`. Used by external monitoring / status dashboards.

### `GET /api/v1/skills`

List all registered Skills with current health snapshot.

**Response:**
```json
{
  "items": [
    { "name": "Wallabag", "status": "Healthy", "routedToday": 12 },
    { "name": "Wekan",    "status": "Degraded", "routedToday": 3  }
  ]
}
```

No pagination — Skill count is low (≤20 realistically).

### `GET /api/v1/skills/{name}`

Detail for a single Skill. Returns the same shape plus a bounded list of recent Captures handled by this Skill (latest 20 by default).

Status codes: `200`, `404`, `401`.

---

## Integrations resource

Read-only surface over `IIntegrationHealthService`. Mirrors Skills.

### `GET /api/v1/integrations`

List all Integrations with status, last successful write, and last write duration.

**Response:**
```json
{
  "items": [
    {
      "name": "Wallabag",
      "status": "Healthy",
      "lastWriteAt": "2026-04-18T09:12:44+02:00",
      "lastWriteDuration": "PT0.482S"
    }
  ]
}
```

### `GET /api/v1/integrations/{name}`

Detail for a single Integration. Same fields; future extension for per-integration config surfacing is deferred.

Status codes: `200`, `404`, `401`.

---

## Health endpoints (standard, not under `/api/v1/`)

Per `CLAUDE.md` and 12-factor:

- `GET /health/live` — liveness; `200 OK` if the process is up.
- `GET /health/ready` — readiness; `200 OK` when DB + bus + Authentik reachable; `503 Service Unavailable` otherwise.

Unauthenticated (platform-level probes).

---

## Error model

All failures return `application/problem+json` per RFC 9457.

```json
{
  "type": "https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/validation.md",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "instance": "/api/v1/captures",
  "errors": {
    "content": [ "Content must not be empty." ]
  },
  "traceId": "00-abc123-def456-01"
}
```

- `traceId` always included for correlation with server logs (OpenTelemetry).
- FluentValidation failures flatten into the `errors` object keyed by field name.
- Domain-specific problem types live under `https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/<slug>.md` — real, resolvable URLs that point at human-readable problem docs in this repo (see D5). The `docs/problems/` catalogue is grown on demand as new problem types surface.

---

## Authentication

OIDC JWTs issued by Authentik. Two expected client flows:

1. **Telegram bot** — owns a confidential OIDC client in Authentik; calls `POST /api/v1/captures` with its own bearer token.
2. **CLI / automation** — device code flow or a long-lived service-account token.

All endpoints require `[Authorize]` at the endpoint-group level. The dev-mode bypass (`DevAuthHandler` from ADR 0001) continues to apply when `ASPNETCORE_ENVIRONMENT=Development`.

Authorization granularity is single-operator — no roles beyond "authenticated user" in scope for Block 3.

---

## Pagination, filtering, caching

- **Pagination:** cursor-based on the list endpoint (captures only); Skills and Integrations fit on one page.
- **Filtering:** query parameters per endpoint as documented above. All filters are additive (AND).
- **Caching:** no ETag / `If-None-Match` in scope for Block 3. The list endpoint is expected to be called infrequently by external automation.
- **Rate limiting:** deferred; single-operator trust model makes this low priority. Platform-level abuse protection (Cloudflare tunnel) is assumed.

---

## OpenAPI + Scalar

- `Microsoft.AspNetCore.OpenApi` generates the OpenAPI document at build time (enabled via `builder.Services.AddOpenApi()`).
- Scalar UI at `/scalar` exposes it interactively; OpenAPI JSON at `/openapi/v1.json`.
- Every endpoint uses `WithTags("Captures" | "Skills" | "Integrations")` and `WithName(...)` so Scalar renders a clean sidebar.
- Every response is declared with `Produces<T>(statusCode)` so the OpenAPI schema is accurate.

Bruno collections under `bruno/api/captures/`, `bruno/api/skills/`, `bruno/api/integrations/` mirror the endpoint structure, one `.bru` per action.

---

## Decisions

- **D1 — `ChannelKind.Api` added.** New enum value for Captures submitted through the REST API; keeps provenance audit-visible and lets clients filter `GET /api/v1/captures?source=Api`. Cost: one enum value plus a one-line persistence migration when `FlowHub.Persistence` lands. `Telegram` stays reserved for the bot's channel; `Web` stays for the Blazor Web Channel's quick-add field.

- **D2 — Separate library project `source/FlowHub.Api/`.** The API is co-hosted with the Blazor Web UI in the same process (per ADR 0002 Decision 4), but the code lives in its own library. `FlowHub.Web/Program.cs` calls `AddFlowHubApi()` on `IServiceCollection` and `MapFlowHubApi()` on the endpoint route builder. Two reasons: (1) `FlowHub.Api.IntegrationTests` can boot a `WebApplicationFactory<T>` against just the API surface; (2) if a capability ever lifts into its own process, the move is a `Program.cs` change, not a code relocation.

- **D3 — Retry returns `202 Accepted`.** `POST /api/v1/captures/{id}/retry` publishes `CaptureCreated` on the bus and returns. Consumers run asynchronously — `202` is the HTTP semantic for "request accepted, processing deferred". Body still contains the Capture so the client sees `Stage=Raw` and cleared `FailureReason` immediately. `200 OK` would misrepresent async completion as synchronous.

- **D4 — Day-one list filters: `stage`, `source`, `limit`, `cursor`.** Enough to satisfy the Telegram bot (its own captures), operator dashboards (failed captures via `stage=Orphan,Unhandled`), and paginated crawls. Stretch filters deferred to when a concrete caller needs them: `createdAfter`, `createdBefore`, `q` (substring match). Each deferred filter adds a validator + at least one index concern in Block 4 Persistence.

- **D5 — Error `type` URIs point at repo-hosted problem docs.** Base URL: `https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/`. Per-problem files are created on demand (`docs/problems/validation.md`, `docs/problems/capture-not-retryable.md`, …). Real, resolvable URLs beat a fictional `flowhub.local` namespace; git history implicitly versions the semantics.

- **D6 — No `DELETE /api/v1/captures/{id}` in v1.** Captures are append-only history per FlowHub's concept. The Blazor UI has no delete action. A redact-style `POST /api/v1/captures/{id}/redact` is a future, narrowly scoped option if sensitive content ever lands in a Capture body; not in Block 3 scope. Retention lives in a scheduled background job (Block 5 Deployment at earliest), not an operator-triggered endpoint.

---

## Implementation checklist (Block 3 Nachbereitung)

1. Scaffold `source/FlowHub.Api/` as a library project with an `AddFlowHubApi()` extension on `IServiceCollection` and `MapFlowHubApi()` on `IEndpointRouteBuilder`.
2. Wire in `FlowHub.Web/Program.cs`.
3. Add FluentValidation validators under `FlowHub.Api/Validation/`.
4. Add ProblemDetails configuration (type URIs, traceId inclusion).
5. Implement the 7 endpoints above.
6. Add xUnit integration tests under `tests/FlowHub.Api.IntegrationTests/` using `WebApplicationFactory`.
7. Add Bruno collections under `bruno/api/`.
8. Confirm OpenAPI renders in Scalar and every endpoint has a `Produces<T>` declaration.
9. Create `docs/problems/` and seed with the first problem docs as endpoints need them (D5).
