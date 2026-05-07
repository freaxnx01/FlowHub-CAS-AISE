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

- `global.json` SDK pin prevents silent .NET version drift across machines
- `Directory.Packages.props` pays off immediately: one version bump in one file
- MudBlazor's `IsDarkMode` must be wired via `MudThemeProvider.MudThemeChanged` event, not static config
- Starting with the domain model (`Capture` as record, `LifecycleStage` enum) before any UI kept the architecture clean

## AI Tooling

Used Claude Code for scaffold generation (solution structure, directory layout, initial domain types). Verified every generated file for correctness — AI suggestion for `CancellationToken` propagation was sound; suggestion to use `IAsyncEnumerable` in `ICaptureRepository` was rejected (overkill for current scale).
