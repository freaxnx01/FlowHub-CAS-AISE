Build the approved UI component step by step (Phase 3 of 4). The wireframe and flow diagrams must be approved before starting.

Context: $ARGUMENTS

## Steps

1. **Shell only** — Create `.razor` and `.razor.cs` code-behind with:
   - MudBlazor layout structure matching the approved wireframe
   - Placeholder `@* TODO *@` comments where dynamic content will go
   - `@inject` declarations for required services (no implementation yet)
   - No business logic, no API calls, no real data
   - Present the shell. Wait for confirmation before Step 2.
2. **Wire up data & logic** —
   - Implement service calls in the code-behind
   - Bind data to MudBlazor components (`@bind-Value`, `Items`, etc.)
   - Handle loading states with `MudSkeleton` or `MudProgressLinear`
   - Handle empty states with a clear `MudText` or `MudAlert`
   - Handle API errors with `MudSnackbar`
   - Present the result. Wait for confirmation before Step 3.
3. **Interactions & events** —
   - Implement button handlers and `EventCallback` wiring
   - Add `MudDialog` for confirmations on destructive actions
   - Add form validation with `MudForm` + `DataAnnotations`
   - Present the result. Wait for confirmation before Step 4.
4. **Polish** —
   - Apply consistent spacing (`ma-*`, `pa-*`, `MudStack`, `MudGrid`)
   - Verify responsive behaviour (`xs`, `sm`, `md` breakpoints where needed)
   - Add `TooltipText` on icon buttons
   - Verify `Icons.Material.Filled.*` usage

## Rules
- One step at a time — never skip ahead
- Code-behind (`.razor.cs`) for all logic — no C# blocks in `.razor`
- Reuse from `/src/Shared/` — check before creating anything new
- No raw HTML where a MudBlazor component exists
- Remind the user to run bUnit tests after Step 3
