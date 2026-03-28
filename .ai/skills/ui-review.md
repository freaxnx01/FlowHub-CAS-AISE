# UI Review — Phase 4 of 4

Review the implemented component against the approved wireframe, flow diagrams, and project conventions.

**Target:** $ARGUMENTS

---

## Review checklist — work through every item

### Layout & Wireframe fidelity
- [ ] Does the layout match the approved ASCII wireframe?
- [ ] Are all sections present (AppBar, Drawer, main content, actions)?
- [ ] Are empty states implemented and visible when there is no data?
- [ ] Are loading states implemented (MudSkeleton or progress indicator)?
- [ ] Are error states handled visibly (MudSnackbar or MudAlert)?

### Flow fidelity
- [ ] Does every user action from the Mermaid flow have an implementation?
- [ ] Are all error branches handled (API failure, 403, 404, validation)?
- [ ] Are destructive actions gated by a MudDialog confirmation?
- [ ] Are all exit points (navigation, success redirect) wired correctly?

### MudBlazor conventions
- [ ] No raw HTML where a MudBlazor component could be used
- [ ] MudDataGrid used for tables (not MudTable unless legacy)
- [ ] MudSnackbar used for feedback (not custom toast)
- [ ] Icons use `Icons.Material.Filled.*`
- [ ] Spacing uses MudBlazor utilities (`ma-*`, `pa-*`, `MudStack`, `MudGrid`)

### Code conventions
- [ ] No C# logic in the `.razor` file (only binding and UI events)
- [ ] Code-behind `.razor.cs` is a `partial class`
- [ ] No direct API calls from the component (always via a service)
- [ ] No duplicate components — anything reusable moved to `/src/Shared/`

### Testing
- [ ] bUnit test file exists for this component
- [ ] Happy path test passes
- [ ] Empty state test passes
- [ ] Error state test passes
- [ ] Playwright E2E test added or updated

---

## Output format
Report findings in three groups:
1. **Must fix** — blocks PR merge
2. **Should fix** — not blocking but flagged for follow-up
3. **Looks good** — confirmed compliant items

End with an overall verdict: ✅ Ready for PR / ⚠️ Needs fixes before PR.
