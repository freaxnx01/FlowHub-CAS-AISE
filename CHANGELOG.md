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
