# Captures List — Flow Diagrams (Phase 2)

- **Page route:** `/captures`
- **Render mode:** Interactive Server (per ADR 0001)
- **Status:** Approved 2026-04-10
- **Phase:** 2 of 4 (`/ui-flow`)
- **Predecessor:** [`wireframe.md`](./wireframe.md)
- **Next phase:** `/ui-build` — Razor component implementation

## Diagram 1 — Entry & data loading

```mermaid
flowchart TD
    Entry{How did the user arrive?}
    Drawer[Drawer: click 'Captures']
    DashOrphan[Dashboard: click orphan count]
    DashUnhand[Dashboard: click unhandled count]
    DashViewAll[Dashboard: click 'View all →']
    DirectURL[Direct URL /captures or /captures?lc=orphan]
    Render[Captures.razor renders]
    ReadQP[Read ?lc= query param<br/>SupplyParameterFromQuery]
    PreSelect{Has ?lc= param?}
    SetFilter[Pre-select lifecycle chip]
    NoFilter[All chips = default]
    LoadAll[OnInitializedAsync:<br/>ICaptureService.GetAllAsync]
    LoadResult{Load OK?}
    ErrorState[MudAlert: could not load<br/>Retry button]
    DataDecision{Any captures?}
    EmptyState[Empty state: 'No captures yet'<br/>'+ New Capture' button]
    Ready[Grid renders with filters applied]

    Entry --> Drawer & DashOrphan & DashUnhand & DashViewAll & DirectURL
    Drawer & DashViewAll & DirectURL --> Render
    DashOrphan --> Render
    DashUnhand --> Render
    Render --> ReadQP --> PreSelect
    PreSelect -- yes --> SetFilter --> LoadAll
    PreSelect -- no --> NoFilter --> LoadAll
    LoadAll --> LoadResult
    LoadResult -- exception --> ErrorState
    LoadResult -- ok --> DataDecision
    DataDecision -- 0 --> EmptyState
    DataDecision -- ≥1 --> Ready
    ErrorState -- click Retry --> LoadAll
```

## Diagram 2 — Filter & interaction loop

```mermaid
flowchart TD
    Ready[Grid rendered with data]
    Action{User action}
    ClickLC[Click lifecycle chip]
    ClickCH[Click channel chip]
    TypeSearch[Type in search field]
    ClearSearch[Click ✕ in search field]
    ClearAll[Click 'Clear filters'<br/>from no-match empty state]
    ApplyFilter[Recompute filtered list:<br/>lifecycle ∩ channel ∩ search]
    FilterResult{Any matches?}
    ShowFiltered[Grid shows filtered rows<br/>Results counter updates]
    NoMatch[No-match empty state:<br/>'No captures match' + Clear filters]
    RowClick[Click a row]
    NavDetail[NavigateTo /captures/{id}]

    Ready --> Action
    Action --> ClickLC & ClickCH & TypeSearch & ClearSearch & ClearAll & RowClick
    ClickLC & ClickCH & TypeSearch & ClearSearch & ClearAll --> ApplyFilter
    ApplyFilter --> FilterResult
    FilterResult -- ≥1 --> ShowFiltered --> Action
    FilterResult -- 0 --> NoMatch --> Action
    RowClick --> NavDetail
```

## Diagram 3 — Component hierarchy & state ownership

```mermaid
graph TD
    Main["MainLayout<br/>(inherited)"]
    Cap["Pages/Captures.razor<br/>OWNS: allCaptures[], filtered[], selectedLC?, selectedCH?, searchText, isLoading, loadError<br/>INJECTS: ICaptureService, NavigationManager"]
    LCChips["MudChipSet&lt;LifecycleStage?&gt;<br/>@bind-SelectedValue=selectedLC"]
    CHChips["MudChipSet&lt;ChannelKind?&gt;<br/>@bind-SelectedValue=selectedCH"]
    Search["MudTextField<br/>@bind-Value=searchText"]
    Grid["MudDataGrid&lt;Capture&gt;<br/>Items=filtered, RowClick → NavigateTo"]
    Badge["Shared/LifecycleBadge<br/>(reused)"]

    Main --> Cap
    Cap --> LCChips
    Cap --> CHChips
    Cap --> Search
    Cap --> Grid
    Grid --> Badge
```

### State & data flow

| Component | Owns | Receives | Calls |
|---|---|---|---|
| `Captures` | `allCaptures[]`, `filtered[]`, `selectedLC?`, `selectedCH?`, `searchText`, `isLoading`, `loadError` | `?lc=` via `[SupplyParameterFromQuery]` | `ICaptureService.GetAllAsync`, `NavigationManager.NavigateTo` |
| `MudChipSet<LifecycleStage?>` | — | `@bind-SelectedValue` | — |
| `MudChipSet<ChannelKind?>` | — | `@bind-SelectedValue` | — |
| `MudTextField` | — | `@bind-Value` | — |
| `MudDataGrid<Capture>` | pagination state (internal) | `Items=filtered` | — |

### Filtering logic (`ApplyFilters()`)

1. Start with `allCaptures`
2. If `selectedLC` is not null → `.Where(c => c.Stage == selectedLC)`
3. If `selectedCH` is not null → `.Where(c => c.Source == selectedCH)`
4. If `searchText` is not empty → `.Where(c => c.Content.Contains(searchText, OrdinalIgnoreCase))`
5. Assign to `filtered`

### Implied surfaces

| # | Surface | Already exists? | Action needed |
|---|---|---|---|
| 1 | `GetAllAsync()` on `ICaptureService` | No | Add to interface + stub |
| 2 | Row click → `/captures/{id}` | Yes — stub page | None |
| 3 | "New Capture" button in empty state → `/captures/new` | Yes — page exists | None |

### Deliberately not in scope

- Server-side filtering/pagination — Block 4
- Debounced search — overkill for 12 in-memory items
- Multi-select lifecycle filter — single-select per chip set is enough
