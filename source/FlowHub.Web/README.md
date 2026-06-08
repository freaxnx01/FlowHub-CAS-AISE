# FlowHub.Web

**Layer:** Composition root + inbound adapters (driving side). The single
deployable host: the Blazor Web App (Interactive Server), the REST API surface
(`FlowHub.Api`), and the async pipeline consumers. `Program.cs` wires every
module's DI together.

## Contents

- `Program.cs` — composition root: registers Core ports + their adapters
  (Persistence, AI, Skills), the API, MassTransit (in-memory in dev/test,
  RabbitMQ in prod), health checks, OpenTelemetry, ProblemDetails, auth.
- `Components/` — MudBlazor UI: `Pages/` (`Dashboard`, `Captures`,
  `CaptureDetail`, `NewCapture`, `Skills`, `Integrations`), `DashboardCards/`,
  `Layout/` (incl. `QuickCaptureField`), and shared components.
- `Pipeline/` — the async consumers: `CaptureEnrichmentConsumer`,
  `SkillRoutingConsumer`, `CaptureEmbeddingConsumer`,
  `CaptureNotificationConsumer`, and `LifecycleFaultObserver` (maps
  `Fault<T>` to lifecycle state; see ADR 0003).
- `Auth/DemoAuthHandler.cs` — dev/demo auth bypass (real OIDC is future work).
- `Notifications/` — optional ntfy.sh capture notifications (dormant until configured).

## Render mode

Interactive Server per ADR 0001. Components hold no business logic — they bind
to injected services only.

## Tests

`tests/FlowHub.Web.ComponentTests` (bUnit), `tests/FlowHub.Web.E2ETests` (Playwright).
