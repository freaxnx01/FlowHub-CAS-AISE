# FlowHub.Api

**Layer:** Inbound HTTP adapter (driving side). Minimal-API endpoint definitions
for non-UI consumers, registered into the `FlowHub.Web` host.

> Packaging note: this is a **class library** of endpoint/registration code that
> `FlowHub.Web` composes into its pipeline — it is not a separately deployed
> container. The split keeps the REST surface isolated and independently testable.

## Contents

- `Endpoints/` — `CaptureEndpoints` (`/api/v1/captures` submit/get/list/retry),
  `SearchEndpoints` (semantic search; returns RFC 9457 `503` when embeddings are
  disabled), `AdminEndpoints` (e.g. rebuild embeddings).
- `Requests/` — request DTOs such as `CreateCaptureRequest`.
- `Validation/` — FluentValidation validators (`CreateCaptureRequestValidator`) so
  the domain stays free of boundary validation.
- `ServiceCollectionExtensions.cs` — DI registration for the API surface.

## Conventions

- Minimal API endpoints, FluentValidation at the boundary, ProblemDetails
  (RFC 9457) for all error responses, OpenAPI + Scalar for documentation.

## Tests

`tests/FlowHub.Api.IntegrationTests`.
