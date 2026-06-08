# FlowHub.Core

**Layer:** Domain + ports (the hexagon's inside). No infrastructure dependencies — no EF Core, Npgsql, MassTransit, or ASP.NET references.

The center of the modular monolith: domain types and the driving/driven port
interfaces every other project implements or consumes.

## Contents

- `Captures/` — the `Capture` aggregate and its lifecycle: `LifecycleStage`,
  `CaptureFilter`/`CapturePage`/`CaptureCursor` (pagination), `SkillRun`,
  `Attachment`, and the ports `ICaptureService`, `ICaptureRepository`,
  `ISkillRunRepository`, `ITagRepository`, `IEmbeddingService`, `IAttachmentStorage`.
- `Classification/` — `IClassifier` (the AI/keyword classification port),
  `ClassificationResult`, `EnrichmentResult`.
- `Channels/` — `Channel` and `IChannelRepository` (capture sources).
- `Events/` — the async pipeline contracts (`CaptureCreated`, `CaptureClassified`; see ADR 0003).
- `Skills/` — `ISkillIntegration` (the outbound skill-write port).
- `Health/` — skill/integration health abstractions surfaced on the dashboard.

## Dependency rule

`FlowHub.Core` depends on **nothing** in the solution. All other source projects
depend inward on it; adapters (Persistence, AI, Skills) implement its ports.
This keeps the domain testable in isolation and is the structural backbone of the
hexagonal design (ADR 0001, ADR 0005).

## Tests

`tests/FlowHub.Core.Tests`.
