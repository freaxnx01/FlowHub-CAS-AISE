# UI Brainstorm — Phase 1 of 4

You are helping design a new UI screen or component for a Blazor WebAssembly application using MudBlazor.

**Target:** $ARGUMENTS

---

## Your job in this phase

### Step 1 — Ask clarifying questions (do this first)
Ask only what you need to understand scope. Cover:
- What is the primary user goal on this screen?
- Which user roles interact with it?
- What data is displayed or captured?
- Are there any existing components in `/src/Shared/` that could be reused?
- Any known constraints (auth, offline, performance)?

Wait for answers before continuing.

### Step 2 — Propose ASCII wireframe
After the user answers, draw a clear ASCII wireframe showing:
- Overall layout (AppBar, Drawer, main content area)
- Key MudBlazor regions (DataGrid, Form, Dialog, etc.)
- Primary actions (buttons, FABs)
- Empty state and loading state placeholders

Use box-drawing characters for clarity:
```
┌─────────────────────────────────────┐
│ AppBar                              │
├──────────┬──────────────────────────┤
│ Drawer   │ Main Content             │
│          │                          │
└──────────┴──────────────────────────┘
```

### Step 3 — Wait for approval
Do NOT proceed to Mermaid diagrams or code.
End with: "Does this wireframe match your intent? Approve to continue to Phase 2 (/ui-flow)."

---

## Rules
- No Mermaid diagrams in this phase
- No code in this phase
- One wireframe iteration at a time
- If the user asks for code, remind them we are still in Phase 1
- On approval, save the wireframe to `docs/design/<feature-name>/wireframe.md`
