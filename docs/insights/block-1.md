# Block 1 — Insights

**Block:** 1 — Vorbereitung & Konzept  
**Date range:** 2026-02-01 – 2026-03-08

## What We Built

Project concept and initial architecture. Chose .NET 10 / Blazor Web App over Quarkus/Jakarta EE (justified in submission PDF — stack-neutral rubric). Defined FlowHub as a modular-monolith PKM automation hub. Created `FlowHub.Core` domain model: `Capture`, `LifecycleStage`, `ChannelKind`, `ICaptureService`, `ICaptureRepository`.

Set up solution structure: `FlowHub.slnx`, `Directory.Build.props` (warnings-as-errors, embedded PDB), `Directory.Packages.props` (central package management).

## Key Decisions

- **Stack:** .NET 10 Blazor Server-interactive + MudBlazor (ADR 0001)
- **Persistence placeholder:** `InMemoryCaptureRepository` (stub) — EF Core PostgreSQL wires in Block 4
- **Test framework:** xunit + FluentAssertions + NSubstitute + bUnit

## Lessons Learned

- `global.json` SDK pin prevents silent .NET version drift across machines — without it, two developers can produce subtly different artefacts from the same source.
- `Directory.Packages.props` pays off immediately: one version bump in one file. Migrating mid-project would cost a day; doing it on day 1 cost ten minutes.
- MudBlazor's `IsDarkMode` must be wired via `MudThemeProvider.MudThemeChanged` event, not static config — the static-config trap is well-documented but easy to fall into when copying examples.
- Starting with the domain model (`Capture` as record, `LifecycleStage` enum) before any UI kept the architecture clean — the temptation to "just throw up a page first" would have leaked UI concepts into the core.
- Pinning **agent conventions** (`CLAUDE.md`, `.ai/base-instructions.md`) at the same time as the SDK and packages turned out to be the most leveraged decision of the block: every later agent session inherits the convention without re-prompting.
- The decision to skip Quarkus/Jakarta EE despite the rubric's wording (10 pts) was made explicit early via ADR-style reasoning in `docs/ai-usage.md` — committing to a stack mismatch is cheap when documented, expensive when discovered at submission.

## AI Tooling

Used Claude Code for scaffold generation (solution structure, directory layout, initial domain types). Verified every generated file for correctness — AI suggestion for `CancellationToken` propagation was sound; suggestion to use `IAsyncEnumerable` in `ICaptureRepository` was rejected (overkill for current scale). The biggest tooling lesson of Block 1: **agent instructions are a deliverable, not metadata** — see `vault/Projektarbeit/Learnings.md` §1.
