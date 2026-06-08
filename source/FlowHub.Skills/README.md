# FlowHub.Skills

**Layer:** Outbound adapters (driven side) implementing `FlowHub.Core`'s
`ISkillIntegration` port — the concrete "write to a downstream homelab service"
targets a classified capture can be routed to.

## Contents

- `Wallabag/` — `WallabagSkillIntegration` (+ `WallabagOptions`): saves
  read-later links to a Wallabag instance.
- `Vikunja/` — `VikunjaSkillIntegration` (+ `VikunjaOptions`,
  `VikunjaProjectCatalog`): creates tasks in a Vikunja project (e.g. Inbox).
- `SkillsServiceCollectionExtensions.cs` — DI registration; each integration is
  registered as an `ISkillIntegration` and selected by name by the routing
  consumer in `FlowHub.Web/Pipeline/`.
- `SkillsBootLogger.cs`, `SkillsRegistrationOutcome.cs` — startup diagnostics
  reporting which skills are configured/healthy.

## Notes

- Each integration uses `IHttpClientFactory`-provided clients (no direct
  `HttpClient` instantiation).
- In the public demo profile, skill writes are disabled — captures stop at
  `Classified`/`Unhandled` and no external write happens.

## Tests

`tests/FlowHub.Skills.Tests`, `tests/FlowHub.Skills.ContractTests`,
`tests/FlowHub.Skills.IntegrationTests`.
