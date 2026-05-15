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

- `MudDataGrid` pagination requires explicit `T` type parameter on `MudDataGrid<Capture>` — implicit inference fails at runtime. The error message points elsewhere, costing ~30 min the first time.
- `@inject` services must be registered before `WebApplicationFactory<Program>` boots — missing DI registration surfaces as a confusing 500, not a startup exception. Always wire the test host's `Services.AddXxx` mirror.
- Stub services should implement real interfaces (not separate stubs-only interfaces) so swapping to real services in Block 4 is zero-effort. This decision compounded over Blocks 3 and 4 — every later swap was a single DI line change.
- The mandatory 4-phase UI workflow (`/ui-brainstorm → /ui-flow → /ui-build → /ui-review`) is the single biggest reason Block 2 stayed on time. Without the wireframe gate, agents jump straight to component code; with it, structural decisions are forced into ASCII and Mermaid first, where they are cheap to change.
- `MudThemeProvider` global state interacts poorly with `RenderFragment` callbacks under Interactive Server — push theme reads to component parameters instead of resolving them inside the fragment.
- bUnit tests run in-process and share state across cases by default — every test must construct its own `TestContext`; sharing through fields silently leaks DI registrations.

## AI Tooling

Used Claude Code UI-brainstorm and UI-flow skills to generate wireframes and Mermaid diagrams before any code. Claude's wireframe suggestions were adopted with minor layout adjustments. Generated bUnit test skeletons; all assertions were written by hand to ensure meaningful coverage rather than tautological tests. The biggest tooling lesson of Block 2: **gate AI's code output behind a human-approved design artefact** — see the 4-phase workflow in `CLAUDE.md` § UI Development Workflow.
