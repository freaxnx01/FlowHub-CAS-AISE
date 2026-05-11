# UI Brainstorm — Phase 1 of 4

You are helping design a new UI screen or component. Use the active stack overlay (`.ai/stacks/<stack>.md`) for component-library-specific vocabulary and conventions — this skill itself is stack-neutral.

**Target:** $ARGUMENTS

---

## Your job in this phase

### Step 1 — Ask clarifying questions (do this first)
Ask only what you need to understand scope. Cover:
- What is the primary user goal on this screen?
- Which user roles interact with it?
- What data is displayed or captured?
- Are there any existing components that could be reused (check the project's shared/common folder)?
- Any known constraints (auth, offline, performance, a11y)?

Wait for answers before continuing.

### Step 2 — Propose ASCII wireframe
After the user answers, draw a clear ASCII wireframe showing:
- Overall layout (top bar / side nav / main content area, or the stack's equivalent)
- Key regions (tables, forms, dialogs, etc.)
- Primary actions (buttons, FABs, links)
- Empty state and loading state placeholders
- Error state placement

Use box-drawing characters for clarity:
```
┌─────────────────────────────────────┐
│ Top Bar                             │
├──────────┬──────────────────────────┤
│ Nav      │ Main Content             │
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
