# Block 2 — Insights

**Block:** 2 — UI & Prototyp  
**Date range:** 2026-03-09 – 2026-04-11

## What We Built

Blazor UI prototype with MudBlazor. Four UI screens designed and implemented following the mandatory phase order (brainstorm → flow → build → review): Dashboard, Captures List, New Capture, Capture Detail.

`Stubs/` folder: Bogus-backed `StubCaptureService` for realistic fake data without a real database. All UI components unit-tested with bUnit.

## Key Decisions

- **Render mode:** Interactive Server (ADR 0001) — simplest path to real-time updates; no WASM bundle overhead
- **UI workflow:** mandatory 4-phase gate (ASCII wireframe → Mermaid flow → build → review) prevents premature coding
- **Test tooling:** bUnit + NSubstitute covers component rendering and event callbacks without spinning up a full app

## Lessons Learned

- `MudDataGrid` pagination requires explicit `T` type parameter on `MudDataGrid<Capture>` — implicit inference fails at runtime
- `@inject` services must be registered before `WebApplicationFactory<Program>` boots — missing DI registration surfaces as a confusing 500, not a startup exception
- Stub services should implement real interfaces (not separate stubs-only interfaces) so swapping to real services in Block 4 is zero-effort

## AI Tooling

Used Claude Code UI-brainstorm and UI-flow skills to generate wireframes and Mermaid diagrams before any code. Claude's wireframe suggestions were adopted with minor layout adjustments. Generated bUnit test skeletons; all assertions were written by hand to ensure meaningful coverage rather than tautological tests.
