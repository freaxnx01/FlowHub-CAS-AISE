# Capture Detail — Flow Diagrams (Phase 2)

- **Page route:** `/captures/{id:guid}`
- **Render mode:** Interactive Server (per ADR 0001)
- **Status:** Approved 2026-04-10
- **Phase:** 2 of 4 (`/ui-flow`)
- **Predecessor:** [`wireframe.md`](./wireframe.md)
- **Next phase:** `/ui-build` — Razor component implementation

## Diagram 1 — Entry & loading

```mermaid
flowchart TD
    Entry{How did the user arrive?}
    DashRow[Dashboard: click capture row]
    ListRow[Captures list: click row]
    Snackbar[Snackbar 'Open' link after quick-capture]
    DirectURL["Direct URL /captures/{id}"]
    Render[CaptureDetail.razor renders]
    Load[OnInitializedAsync:<br/>ICaptureService.GetByIdAsync]
    Result{Load OK?}
    ErrorState[MudAlert: could not load<br/>Retry button]
    Found{Capture found?}
    NotFound[Not-found state:<br/>'Capture not found' + Back link]
    StageCheck{Lifecycle stage?}
    ViewOnly[View-only: Completed / Routed / Raw / Classified<br/>No actions section]
    ActionView[Action view: Orphan or Unhandled<br/>Failure alert + action buttons]

    Entry --> DashRow & ListRow & Snackbar & DirectURL --> Render
    Render --> Load --> Result
    Result -- exception --> ErrorState
    Result -- ok --> Found
    Found -- null --> NotFound
    Found -- exists --> StageCheck
    StageCheck -- Orphan / Unhandled --> ActionView
    StageCheck -- other --> ViewOnly
    ErrorState -- Retry --> Load
```

## Diagram 2 — Actions (all stubbed in Block 2)

```mermaid
flowchart TD
    ActionView[Action view rendered]
    UserAction{User clicks}
    Retry[Click 'Retry routing']
    Reassign[Click 'Reassign/Assign skill']
    SkillMenu[MudMenu opens: skill list from ISkillRegistry]
    PickSkill[User picks a skill]
    Ignore[Click 'Ignore']
    Stub[Snackbar: 'This action will work<br/>once backend Skills are wired in Block 3']
    Back[Click '← Back to Captures']
    NavList[NavigateTo /captures]

    ActionView --> UserAction
    UserAction --> Retry & Reassign & Ignore & Back
    Retry --> Stub --> ActionView
    Reassign --> SkillMenu --> PickSkill --> Stub
    Ignore --> Stub
    Back --> NavList
```

## Diagram 3 — Component hierarchy

```mermaid
graph TD
    Main["MainLayout (inherited)"]
    CD["Pages/CaptureDetail.razor<br/>OWNS: capture?, skills[], isLoading, loadError<br/>INJECTS: ICaptureService, ISkillRegistry, ISnackbar, NavigationManager"]
    Badge["Shared/LifecycleBadge (reused)"]
    Alert["MudAlert (conditional: Orphan/Unhandled)"]
    Actions["MudStack Row: action MudButtons<br/>(conditional: Orphan/Unhandled)"]
    Menu["MudMenu: skill list<br/>(for Reassign/Assign)"]

    Main --> CD
    CD --> Badge
    CD --> Alert
    CD --> Actions
    Actions --> Menu
```

### State & data flow

| Component | Owns | Receives | Calls |
|---|---|---|---|
| `CaptureDetail` | `capture?`, `skills[]`, `isLoading`, `loadError` | `{Id}` route param | `ICaptureService.GetByIdAsync`, `ISkillRegistry.GetHealthAsync`, `ISnackbar.Add`, `NavigationManager.NavigateTo` |

Flat page, no child components, no EventCallbacks.

### Implied surfaces

| # | Surface | Already exists? | Action needed |
|---|---|---|---|
| 1 | `GetByIdAsync(Guid)` on `ICaptureService` | No | Add to interface + stub |
| 2 | `FailureReason` on `Capture` record | No | Add nullable string field |
| 3 | Skill list for MudMenu | Yes (`ISkillRegistry.GetHealthAsync`) | None |

No new pages, no new dialogs, no new shared components.
