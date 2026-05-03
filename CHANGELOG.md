# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **Project scaffolding**: `global.json` (.NET 10), `Directory.Build.props`, `Directory.Packages.props` (central package management), `FlowHub.slnx` root solution, `Makefile` with dev task targets
- **FlowHub.Core**: domain types (`Capture`, `ChannelKind`, `LifecycleStage` with `Completed` terminal state, `FailureCounts`, `SkillHealth`, `IntegrationHealth`, `HealthStatus`) and driving-port interfaces (`ICaptureService`, `ISkillRegistry`, `IIntegrationHealthService`)
- **FlowHub.Web**: Blazor Web App with Interactive Server rendering (per ADR 0001)
  - MudBlazor wiring + `MudLayout` shell (AppBar with QuickCaptureField, mini MudDrawer, MudMainContent)
  - `DevAuthHandler` — dev-only auth bypass returning fixed "Dev Operator" principal
  - Bogus-backed stub services (`CaptureServiceStub`, `SkillRegistryStub`, `IntegrationHealthServiceStub`) with seeded test data including orphan/unhandled/routed/completed lifecycle states
  - **Dashboard** (`/`): NeedsAttentionCard, RecentCapturesCard (MudDataGrid, 10 rows), SkillHealthCard, IntegrationHealthCard — with loading, empty, all-clear, and error states
  - **New Capture** (`/captures/new`): multi-line content + optional Skill override dropdown, rapid multi-entry (clear form + stay on page after submit)
  - **Captures list** (`/captures`): full-page MudDataGrid with lifecycle chip filter, channel chip filter, text search, pagination; `?lc=` query param pre-selection from Dashboard click-through
  - **Capture detail** (`/captures/{id}`): single-card view with full content, metadata, conditional failure alert (Orphan/Unhandled), and stubbed action buttons (Retry/Reassign/Assign/Ignore → "Coming in Block 3" snackbar)
  - **Skills** (`/skills`): MudDataGrid with name, HealthDot status, routed-today count
  - **Integrations** (`/integrations`): MudDataGrid with name, HealthDot status, last-write timing
  - Shared components: `LifecycleBadge`, `HealthDot`
  - `<NotFound>` template in Routes.razor for unmatched URLs
- **FlowHub.Web.ComponentTests**: bUnit + xunit + FluentAssertions + NSubstitute — 17 tests covering Dashboard cards, stub service behavior, New Capture form, and Captures list rendering
- **ADR 0001**: Frontend render mode and architecture (Blazor Interactive Server, OIDC/Authentik, Web as Channel, Bogus stubs)
- **FlowHub Glossary** in the CAS Obsidian vault: Capture (with 6-stage lifecycle), Channel, Skill, Integration, Enrichment, Page/Component/Layout/Card/Widget, Render Mode
- **Design docs**: wireframes + Mermaid flow diagrams for Dashboard, New Capture, Captures list, and Capture detail in `docs/design/`
- **Block 3 Slice C — AI integration**: `FlowHub.AI` becomes an active project with `AiClassifier` (`IClassifier` adapter using `Microsoft.Extensions.AI`)
  - Two interchangeable provider adapters behind one `IChatClient`: Anthropic native (default `claude-haiku-4-5-20251001`) and OpenRouter (default `meta-llama/llama-3.1-70b-instruct`)
  - One round-trip for classification + AI-generated `Title` (extends `ClassificationResult` with optional `Title?`)
  - Graceful fallback to `KeywordClassifier` on any AI failure (network, timeout, JSON parse, schema-violation, generic exception) — capture is always classified
  - `AddFlowHubAi(IConfiguration)` extension with D8 behaviour matrix: silent fallback on missing provider/key, throws on invalid `Ai__Provider`
  - `AiBootLogger` `IHostedService` writes startup log `3020 AiProviderRegistered` / `3021 AiProviderNotConfigured`
  - 18 mocked unit tests (10 for `AiClassifier`, 8 for `AddFlowHubAi`) + 4 trait-gated live integration tests
  - `Makefile`: `make test` filters `Category!=AI`; new `make test-ai` runs the live tests
- **ADR 0004**: AI integration in services (provider, abstraction, prompt + cost strategy)
- **EventId range 3000–3999** reserved for AI (extends ADR 0003 namespacing)
