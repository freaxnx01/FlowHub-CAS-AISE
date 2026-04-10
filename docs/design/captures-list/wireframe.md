# Captures List — Wireframe (Phase 1)

- **Page route:** `/captures`
- **Render mode:** Interactive Server (per ADR 0001)
- **Status:** Approved 2026-04-10
- **Phase:** 1 of 4 (`/ui-brainstorm`)
- **Next phase:** `/ui-flow` — Mermaid page-flow diagrams

## Settled inputs

| Decision | Choice |
|---|---|
| Scope | **Medium (option B)** — lifecycle chips + channel chips + text search (client-side) |
| Query param | `?lc=orphan` / `?lc=unhandled` pre-selects lifecycle chip on entry |
| New service method | `GetAllAsync()` added to `ICaptureService` (option A) |
| Row click | Navigate to `/captures/{id}` |
| Pagination | `MudDataGrid` built-in, default 10 rows, options [10, 25, 50] |

---

## Default state — all Captures, no filter

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│ ☰  FlowHub   [ + Quick capture: paste URL or type… 🖇 attach            ⏎ ]  👤 │
├────┬────────────────────────────────────────────────────────────────────────────┤
│ ⌂  │                                                                            │
│ ⊞← │  Captures                                                                  │
│ +  │  ─────────────────────────────────────────────────────────────────          │
│ ⚙  │                                                                            │
│ ⇄  │  ┌─ Filter bar ──────────────────────────────────────────────────────┐     │
│    │  │                                                                    │     │
│    │  │  Lifecycle:                                                        │     │
│    │  │  [ All ] [Raw] [Classified] [Routed] [•Orphan] [Unhandled]        │     │
│    │  │                                                                    │     │
│    │  │  Channel:                 Search:                                  │     │
│    │  │  [ All ] [📱 Telegram] [🖥 Web]    [ 🔍 Search content…         ] │     │
│    │  │                                                                    │     │
│    │  └────────────────────────────────────────────────────────────────────┘     │
│    │                                                                            │
│    │  ┌─ MudDataGrid ─────────────────────────────────────────────────────┐     │
│    │  │ ⏱     CH  STATE       CONTENT                        SKILL       │     │
│    │  │ ────  ──  ──────────  ─────────────────────────────  ──────────  │     │
│    │  │ 2 m   📱  ✓ routed    Inception (2010) — rewatch     Movies      │     │
│    │  │ 5 m   🖥  ✓ routed    https://heise.de/select/c…     Articles    │     │
│    │  │ 8 m   📱  ⚠ orphan    https://galaxus.ch/s18/…       Books ⚠     │     │
│    │  │ 12 m  📱  ✓ routed    Schmidts Katze — Bezirks…      Books       │     │
│    │  │ 17 m  🖥  ❓ unhand    https://example.com/weird      —           │     │
│    │  │ 23 m  📱  ✓ routed    "Information is the resol…"    Quotes      │     │
│    │  │ 28 m  📱  ✓ routed    AdGuard Home self-host         Homelab     │     │
│    │  │ 34 m  🖥  ✓ routed    ct 2026/05 article snip…       Knowledge   │     │
│    │  │ 41 m  📱  ⚠ orphan    The Imitation Game             Movies ⚠    │     │
│    │  │ 47 m  📱  ✓ routed    Galaxus Quittung 2026-04-09    Belege      │     │
│    │  │ 1 h   📱  ✓ routed    Star Trek SNW S03              Movies      │     │
│    │  │ 1 h   🖥  ✓ routed    https://jellyfin.org/          Homelab     │     │
│    │  │                                                                    │     │
│    │  │ ──── Results: 12   ──────────── Page 1 of 1 ─── [10▾] [◀] [▶] ── │     │
│    │  └────────────────────────────────────────────────────────────────────┘     │
│    │                                                                            │
└────┴────────────────────────────────────────────────────────────────────────────┘
```

## Region map

| # | Region | MudBlazor mapping | Notes |
|---|---|---|---|
| 1 | Page title | `MudText` Typo.h5 | "Captures" |
| 2 | Lifecycle filter chips | `MudChipSet` `T="LifecycleStage?"` `SelectionMode.SingleSelection` | `null` = All. Pre-selects from `?lc=` query param. Active chip filled, rest outlined. |
| 3 | Channel filter chips | `MudChipSet` `T="ChannelKind?"` `SelectionMode.SingleSelection` | `null` = All. Same styling as lifecycle chips. |
| 4 | Search field | `MudTextField` with `Adornment.Start` search icon + `Adornment.End` clear button | Client-side `StringComparison.OrdinalIgnoreCase` Contains on `Capture.Content` |
| 5 | Data grid | `MudDataGrid<Capture>` | Full page width, same columns as Dashboard's `RecentCapturesCard`. Row click → `/captures/{id}`. Built-in pagination + sorting. |
| 6 | Pagination | `MudDataGrid` built-in `PagerContent` | Default page size 10, options [10, 25, 50]. Results counter. |
| 7 | Lifecycle badge | Shared `LifecycleBadge` component | Reused in State column cell template |
| 8 | Channel icon | Inline `MudIcon` | Telegram = `Icons.Material.Filled.Send`, Web = `Icons.Material.Filled.Computer` |

## Filter bar — with `?lc=orphan` pre-selected

```
  Lifecycle:
  [ All ] [Raw] [Classified] [Routed] [■ Orphan ■] [Unhandled]
                                         ↑ filled, rest outlined

  Channel:                 Search:
  [■ All ■] [📱 Telegram] [🖥 Web]    [ 🔍 Search content…         ]

  Grid shows only orphan Captures.
```

## Filter bar — with search active

```
  Lifecycle:
  [■ All ■] [Raw] [Classified] [Routed] [Orphan] [Unhandled]

  Channel:                 Search:
  [■ All ■] [📱 Telegram] [🖥 Web]    [ 🔍 inception              ✕ ]
                                                                   ↑ clear

  Grid shows only rows where Content contains "inception" (case-insensitive).
  Results counter: "Results: 1"
```

## Empty state — no Captures at all

```
┌─ MudDataGrid ────────────────────────────────────────────┐
│                                                          │
│                          📭                              │
│                                                          │
│        No captures yet. Send something via               │
│     Telegram, or use the quick-capture field above.      │
│                                                          │
│              [  + New Capture  ]                         │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

## Empty state — filters active but no match

```
┌─ MudDataGrid ────────────────────────────────────────────┐
│                                                          │
│                          🔍                              │
│                                                          │
│        No captures match the current filters.            │
│           Try adjusting your search or filters.          │
│                                                          │
│              [  Clear filters  ]                         │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

## Loading state

```
┌─ Filter bar ────────────────────────────────────────┐
│  ▒▒▒▒  ▒▒▒  ▒▒▒▒▒▒  ▒▒▒▒  ▒▒▒▒▒  ▒▒▒▒▒▒           │
└─────────────────────────────────────────────────────┘

┌─ MudDataGrid ───────────────────────────────────────┐
│  ▒▒▒▒  ▒▒  ▒▒▒▒▒▒  ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒  ▒▒▒▒▒▒      │
│  ▒▒▒▒  ▒▒  ▒▒▒▒▒▒  ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒  ▒▒▒▒▒▒      │
│  ▒▒▒▒  ▒▒  ▒▒▒▒▒▒  ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒  ▒▒▒▒▒▒      │
│  ▒▒▒▒  ▒▒  ▒▒▒▒▒▒  ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒  ▒▒▒▒▒▒      │
│  ▒▒▒▒  ▒▒  ▒▒▒▒▒▒  ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒  ▒▒▒▒▒▒      │
└─────────────────────────────────────────────────────┘
```

## Design decisions

- **Full-page `MudDataGrid`**, not embedded in a card — canonical data view, gets full width + pagination.
- **Client-side filtering** — all Captures live in Bogus stub (12 items). No server-side paging until Block 4.
- **`?lc=` query param** read via `[SupplyParameterFromQuery]`, pre-selects lifecycle chip. Completes Dashboard → list click-through.
- **Same column layout as Dashboard** — operator's eye already knows the pattern. Addition: pagination + full count.
- **`MudDataGrid` built-in sorting** on When column (descending default).
- **Search is client-side `Contains` (OrdinalIgnoreCase)** — no debounce needed for 12 items.
- **"Clear filters" button** in the no-match empty state resets all filters + clears search.
- **`GetAllAsync()`** added to `ICaptureService` — clean intent, one line in interface + stub.

## Deliberately deferred

- **Page-flow** (`/ui-flow`, Phase 2): entry from Dashboard with query param, filter interactions, row click, empty states.
- **Code** (`/ui-build`, Phase 3).
- **Tests** (`/ui-review`, Phase 4).
