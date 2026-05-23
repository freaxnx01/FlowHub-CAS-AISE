# Block 2 — Insights

**Block:** 2 — UI & Prototyp
**Date range:** 2026-03-09 – 2026-04-11

## What We Built

Blazor UI prototype with MudBlazor. Six routable pages designed and implemented following the mandatory four-phase workflow (brainstorm → flow → build → review): Dashboard, New Capture, Captures List, Capture Detail, Skills, Integrations. Each page reached production quality against Bogus stub services.

`source/FlowHub.Web/Stubs/` folder: Bogus-backed `StubCaptureService`, `SkillRegistryStub`, `IntegrationHealthServiceStub` for realistic fake data without a real database. All UI components unit-tested with bUnit; a full smoke-test walkthrough lands in `tests/FlowHub.Web.SmokeTests/` and replaces the manual click-through that the Nachbereitung TODO originally called for.

`justfile` added with `run` / `watch` / `build` / `test` / `format` targets — the entry point used by every subsequent block. CHANGELOG bootstrapped. Five FlowHub Claude Code skills authored (`flowhub-capture`, `flowhub-triage`, `flowhub-issue`, `flowhub`, `flowhub-dispatcher`) to support the workflow that drives the captures themselves.

## Key Decisions

- **Render mode:** Interactive Server (ADR 0001) — simplest path to real-time updates; no WASM bundle overhead.
- **UI workflow:** mandatory 4-phase gate (ASCII wireframe → Mermaid flow → build → review) prevents premature coding.
- **Stubs implement real interfaces:** swapping to real services in Block 4 reduces to a DI registration change.
- **Test tooling:** bUnit + NSubstitute covers component rendering and event callbacks without spinning up a full app; smoke tests cover the cross-page walkthrough.

## Process

The 4-phase UI workflow ran on every page:

1. **Wireframe (Phase 1)** — ASCII wireframe committed to `docs/design/`. AI-assisted brainstorming via the `ui-brainstorm` skill.
2. **Flow (Phase 2)** — Mermaid flow + sequence diagrams (`docs/design/sequences/`, `docs/design/journeys.md`). Rendered to PNG via the same toolchain that produces the submission PDF.
3. **Build (Phase 3)** — Page + components + DI wiring + bUnit tests in the same commit family.
4. **Review (Phase 4)** — Walkthrough check, then `ui-review` skill for cross-cutting checks.

This was enforced by skill design — the AI itself refuses to skip phases.

## Lessons Learned

- `MudDataGrid<T>` pagination requires the explicit type parameter (`MudDataGrid<Capture>`) — implicit inference fails at runtime, not at compile time.
- `@inject` services must be registered before `WebApplicationFactory<Program>` boots — missing DI registration surfaces as a confusing 500, not a startup exception. Fixed by moving the stub registrations to a `Program` extension method used by both `Web` and `WebApplicationFactory`.
- Stubs implementing the real driving-port interfaces meant the Block 4 transition was nearly mechanical — a 2-line DI change, not a rewrite.
- Mermaid → PNG rendering at design time (rather than relying on runtime browser support) keeps the submission PDF self-contained and ensures the diagrams in the PDF match the diagrams the bUnit tests reference.

## AI Usage by Page

### Dashboard

Dashboard layout and the "Needs Attention" widget were AI-generated from the wireframe. The first version mixed concerns (data fetch + render inline); human refactor extracted `IDashboardData` and a parallel-load pattern (4 parallel `Task<T>` for the 4 cards).

### New Capture

Form layout and validation wiring fully AI-generated. AI's initial form used inline `Action<MouseEventArgs>` handlers; human refactor to `EventCallback<…>` for proper Blazor lifecycle and bUnit testability. Skill dropdown disable-on-error logic was a human-added robustness improvement.

### Captures List

`MudDataGrid<Capture>` with filter chips, search field, and pagination was AI-scaffolded. Filter composition (lifecycle AND channel AND search) was a human refactor — AI's first version treated the filters as OR. Deep-link query-param parsing (`?lc=Orphan`) added in a second pass.

### Capture Detail

Detail page + stub action buttons fully AI-generated. The "Coming in Block 3" snackbar pattern was a human design decision to make Block-2 stub state explicit rather than silently no-op.

### Skills + Integrations

Mostly mechanical given the patterns established by the earlier pages — AI generated both in one pass, human reviewed for consistency.

### Smoke tests

`tests/FlowHub.Web.SmokeTests` replaced the manual walkthrough TODO. AI-generated using the patterns already established by the bUnit suite; human-added the cross-page assertions that the original TODO described.

## AI Usage Metrics (Block 2, estimate)

| Artifact | Approx. lines | AI-generated | Human lines | AI % (est.) |
|---|--:|--:|--:|--:|
| Razor pages (6) | ~900 | ~720 | ~180 | 80% |
| Stub services (3) + Bogus fakes | ~250 | ~225 | ~25 | 90% |
| bUnit tests (~30 tests across pages) | ~700 | ~595 | ~105 | 85% |
| Smoke tests | ~150 | ~120 | ~30 | 80% |
| Wireframes + Mermaid flows | ~400 | ~280 | ~120 | 70% |
| justfile + Make targets doc | ~80 | ~60 | ~20 | 75% |
| FlowHub CC-skills (5 skills) | ~500 | ~350 | ~150 | 70% |

Human contributions concentrated in: filter-composition logic (AND vs OR), parallel data-fetch refactor, deep-link param parsing, Block-3-stub UX policy, and skill design (the FlowHub triage logic is human-authored after AI failed to converge on a workable classification policy).

## KI-Reflexion / Fazit

### Stärken

- **Razor / MudBlazor.** Komponentengeneration aus Wireframes ist die mit Abstand effizienteste Form der KI-Unterstützung in diesem Stack — Wireframe rein, Razor + bUnit-Skelett raus, in einem Schritt.
- **Test-Skelette.** bUnit-Test-Klassen folgen einem stark wiederholten Muster (`RenderComponent<T>`, `Find(…)`, `Markup.Should().Contain(…)`); KI generiert sie zuverlässig.
- **Mermaid-Diagramme.** Flow- und Sequence-Diagramme sind ein Format, in dem die KI besonders stark ist; das Phase-2-Artefakt war faktisch immer ein One-Shot.

### Grenzen

- **Filter-Komposition.** Die "AND vs OR"-Frage in der Captures-List ist eine Spec-Lese-Frage — die KI hat den Wireframe-Hinweis (mehrere Chips = AND) übersehen und OR generiert. Ohne Acceptance-Kriterium kein verlässliches Verhalten.
- **Stub-Disziplin.** KI schlug mehrfach vor, in Stubs `throw new NotImplementedException()` für noch nicht implementierte Operationen zu setzen; Policy-Entscheidung war stattdessen "leere, aber valide Antwort + Snackbar-Hinweis", damit die UI im Demo-Modus durchläuft.
- **Component-Lifecycle.** Erstvorschläge nutzten `Action<…>` statt `EventCallback<…>` — funktional fast identisch, aber bUnit-Tests scheitern an fehlender Re-Render-Signal.

### Fazit

Block 2 hat den UI-Workflow etabliert, der den Rest des Projekts trägt: Wireframe → Flow → Build → Review, mit der KI als Beschleuniger in Phase 1 und 3. Der menschliche Beitrag konzentrierte sich auf Policy-Entscheidungen (Stub-UX, Filter-Logik, Phase-Disziplin) — der reine Tipparbeitsanteil ist deutlich gesunken, der Anteil an Entscheidungs- und Review-Arbeit unverändert hoch.
