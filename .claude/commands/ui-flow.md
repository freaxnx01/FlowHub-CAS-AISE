Map UI logic with Mermaid flow diagrams (Phase 2 of 4). The ASCII wireframe from Phase 1 must be approved before starting.

Context: $ARGUMENTS

## Steps

1. Generate a Mermaid `flowchart TD` for the user journey covering:
   - All entry points to this screen
   - User decisions and branching paths
   - Error states (validation errors, API failures, 403/404)
   - Empty states (no data yet, first-run)
   - Success states and exit points
   - Confirmation dialogs for destructive actions
2. Generate a Mermaid component & state map showing:
   - Component hierarchy (parent → children)
   - Which component owns which state
   - Data flow direction (props down, EventCallback up)
   - Which services are injected and where
   - API calls: which component triggers them
3. List any additional screens or dialogs implied by this flow that were not in the wireframe — flag them explicitly
4. Wait for approval — do NOT write any component code
5. End with: "Do these diagrams capture the intended logic? Approve to continue to Phase 3 (/ui-build)."
6. On approval, save the diagrams to `docs/design/<feature-name>/flow.md`

## Rules
- No component code in this phase
- If the flow reveals a missing screen, surface it — do not silently skip it
- Keep Mermaid diagrams readable: max ~15 nodes per diagram, split if needed
- Use MudBlazor component names in the component map (e.g. MudDataGrid, MudDialog)
