# UI Build — Phase 3 of 4

The wireframe and flow diagrams are approved. Now build the component. This skill is stack-neutral — follow the active stack overlay (`.ai/stacks/<stack>.md`) for component-library specifics (which layout/form/dialog/loading components to prefer, file naming, code-behind conventions).

**Target:** $ARGUMENTS

---

## Build order — follow this exactly, one step at a time

### Step 1 — Shell only
Create the component file(s) with:
- Layout structure matching the approved wireframe, using the stack's preferred layout/container components
- Placeholder comments where dynamic content will go
- Service/dependency declarations (no implementation yet)
- No business logic, no API calls, no real data

Present the shell. Wait for confirmation before Step 2.

### Step 2 — Wire up data & logic
- Implement service calls
- Bind data to the UI (using the stack's native binding mechanism)
- Handle **loading** states (skeleton/spinner/progress indicator)
- Handle **empty** states (clear message or empty-state widget)
- Handle **API error** states (toast/snackbar/banner)

Present the result. Wait for confirmation before Step 3.

### Step 3 — Interactions & events
- Implement action handlers and parent/child event wiring
- Add a confirmation dialog for destructive actions
- Add form validation using the stack's validation mechanism

Present the result. Wait for confirmation before Step 4.

### Step 4 — Polish
- Apply consistent spacing using the stack's spacing utilities
- Verify responsive behaviour at the expected breakpoints
- Add tooltips on icon-only buttons
- Verify icon set consistency (one family, one weight)

---

## Hard rules
- One step at a time — never skip ahead
- Keep view/markup files free of business logic — only binding and UI events
- Reuse from the project's shared/common folder — check before creating anything new
- No raw HTML / primitive widgets where a first-party component library component exists (per the stack overlay)
- Remind the user to run the component-level tests after Step 3 (bUnit / widget test / RTL — per stack)
