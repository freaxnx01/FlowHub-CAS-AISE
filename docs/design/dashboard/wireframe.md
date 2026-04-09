# Dashboard — Wireframe (Phase 1)

- **Page route:** `/`
- **Render mode:** Interactive Server (per ADR 0001)
- **Status:** Approved 2026-04-09
- **Phase:** 1 of 4 (`/ui-brainstorm`)
- **Next phase:** `/ui-flow` — Mermaid page-flow diagrams

## Settled inputs

| Decision | Choice |
|---|---|
| Side rail | Mini drawer, expands on **click** (not hover) |
| Quick-capture entry | Compact field in `MudAppBar` (visible on every Page) **plus** dedicated `/captures/new` Page for rich input |
| Density | Medium — Needs Attention + Recent Captures + Skill Health + Integration Health (no charts in Block 2) |
| Row click on a Capture | Navigate to `/captures/{id}` |

---

## Default state — data present, mini drawer expanded

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│ ☰  FlowHub   [ + Quick capture: paste URL or type… 🖇 attach            ⏎ ]  👤 │
├──────────────┬──────────────────────────────────────────────────────────────────┤
│              │                                                                  │
│  ⌂  Dashboard│  ┌─ Needs attention ────────────────────────────────────────┐    │
│              │  │  ⚠   3  orphan captures        →  /captures?lc=orphan    │    │
│  ⊞  Captures │  │  ❓   1  unhandled capture      →  /captures?lc=unhand    │    │
│              │  └──────────────────────────────────────────────────────────┘    │
│  +  New      │                                                                  │
│              │  ┌─ Recent captures (last 10) ───────────────────────[ View all →]┐
│  ⚙  Skills   │  │ ⏱     CH  STATE      CONTENT                       SKILL     │ │
│              │  │ ────  ──  ─────────  ─────────────────────────────  ──────── │ │
│  ⇄  Integr.  │  │ 2 m   📱  ✓ routed   Inception (2010) — rewatch    Movies    │ │
│              │  │ 5 m   🖥  ✓ routed   https://heise.de/select/c…    Articles  │ │
│              │  │ 8 m   📱  ⚠ orphan   https://galaxus.ch/s18/…      Books ⚠   │ │
│              │  │ 12 m  📱  ✓ routed   Schmidts Katze — Bezirks…     Books     │ │
│              │  │ 17 m  🖥  ❓ unhand   https://example.com/weird     —         │ │
│              │  │ 23 m  📱  ✓ routed   "Information is the resol…"   Quotes    │ │
│              │  │ 28 m  📱  ✓ routed   AdGuard Home self-host        Homelab   │ │
│              │  │ 34 m  🖥  ✓ routed   ct 2026/05 article snip…      Knowledge │ │
│              │  │ 41 m  📱  ⚠ orphan   The Imitation Game            Movies ⚠  │ │
│              │  │ 47 m  📱  ✓ routed   Galaxus Quittung 2026-04-09   Belege    │ │
│              │  └──────────────────────────────────────────────────────────────┘ │
│              │                                                                  │
│              │  ┌─ Skill health ──────────────┐  ┌─ Integration health ───────┐ │
│              │  │ Books        ✓ healthy   42 │  │ Wallabag    ✓ up   1m ago  │ │
│              │  │ Movies       ✓ healthy    8 │  │ Wekan       ✓ up   8m ago  │ │
│              │  │ Articles     ✓ healthy   15 │  │ Vikunja     ✓ up   4m ago  │ │
│              │  │ Quotes       ⚠ degraded   2 │  │ Paperless   ✓ up   2h ago  │ │
│              │  │ Knowledge    ✓ healthy    3 │  │ Obsidian    ⚠ slow 2m 1.2s │ │
│              │  │ Belege       ✓ healthy    7 │  │ Authentik   ✓ up   —       │ │
│              │  │            [ Manage skills →]│  │       [ Manage integr. →]  │ │
│              │  └─────────────────────────────┘  └────────────────────────────┘ │
│              │                                                                  │
└──────────────┴──────────────────────────────────────────────────────────────────┘
```

## Region map

| # | Region | MudBlazor mapping | Notes |
|---|---|---|---|
| 1 | Top bar | `MudAppBar` | Hamburger toggles drawer · brand left · quick-capture center · user menu right |
| 2 | Quick-capture field | `MudTextField` (`Adornment.End` = paste/attach + Enter icon) | Lives in the AppBar — visible on **every** Page, not just Dashboard. Submission target: `CaptureService.Submit(new RawCapture(source: WebChannel, …))` |
| 3 | Side rail | `MudDrawer` `Variant=Mini` `OpenMiniOnHover=false` | Click hamburger to expand. Collapsed = icons only |
| 4 | Needs attention | `MudCard` (full width) | Calm-by-default, bold accent only when counts > 0. Each row click-throughs to the Captures list with the matching `lc=` filter |
| 5 | Recent captures | `MudDataGrid` | 10 rows, compact density, row click → `/captures/{id}`. Lifecycle state in its own column so failures are findable in-line |
| 6 | Skill health | `MudCard` (left, ½ width) | Tiny status icon + counter per Skill. Footer link → `/skills` |
| 7 | Integration health | `MudCard` (right, ½ width) | Same shape as Skill health for visual symmetry. Footer link → `/integrations` |
| 8 | User menu | `MudMenu` in AppBar | Profile · theme toggle · logout |

## Mini drawer — collapsed (default appearance)

```
┌────┬───────────────────────────────────────…
│ ☰  │  FlowHub   [ + Quick capture …    ⏎ ] │
├────┼───────────────────────────────────────…
│ ⌂  │  ┌─ Needs attention ────────────────  │
│ ⊞  │  │  ⚠ 3 orphan      ❓ 1 unhandled    │
│ +  │  └──────────────────────────────────  │
│ ⚙  │  …
│ ⇄  │
│    │
└────┴───
```

Drawer expands on hamburger **click**, not hover (hover-expand is twitchy and the operator's mouse passes over the rail constantly).

## "All clear" state — Needs attention with zero failures

```
┌─ Needs attention ────────────────────────────────────────┐
│  ✓  All captures routed successfully — nothing to review │
└──────────────────────────────────────────────────────────┘
```

Card stays present (consistent layout), changes content + tone. No bold accents, no click-through.

## Empty state — first run, no captures yet

```
┌─ Recent captures ────────────────────────────────────────┐
│                                                          │
│                          📭                              │
│                                                          │
│           No captures yet. Send something via            │
│        Telegram, or use the quick-capture field          │
│                  in the bar above ↑                      │
│                                                          │
│                  [  + Capture something  ]               │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

Skill health / Integration health in first-run: each card lists configured Skills/Integrations with `— no activity yet` instead of counters.

## Loading state — initial render before stub data resolves

```
┌─ Needs attention ────────────────────────────────────────┐
│  ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒    ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒                   │
└──────────────────────────────────────────────────────────┘

┌─ Recent captures ────────────────────────────────────────┐
│  ▒▒▒▒  ▒▒  ▒▒▒▒▒▒    ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒    ▒▒▒▒▒▒     │
│  ▒▒▒▒  ▒▒  ▒▒▒▒▒▒    ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒    ▒▒▒▒▒▒     │
│  ▒▒▒▒  ▒▒  ▒▒▒▒▒▒    ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒    ▒▒▒▒▒▒     │
│  …                                                       │
└──────────────────────────────────────────────────────────┘

┌─ Skill health ──────────────┐  ┌─ Integration health ───┐
│  ▒▒▒▒▒▒▒  ▒▒▒▒▒▒▒  ▒▒       │  │  ▒▒▒▒▒▒▒  ▒▒▒▒▒▒▒  ▒▒  │
│  ▒▒▒▒▒▒▒  ▒▒▒▒▒▒▒  ▒▒       │  │  ▒▒▒▒▒▒▒  ▒▒▒▒▒▒▒  ▒▒  │
│  ▒▒▒▒▒▒▒  ▒▒▒▒▒▒▒  ▒▒       │  │  ▒▒▒▒▒▒▒  ▒▒▒▒▒▒▒  ▒▒  │
└─────────────────────────────┘  └────────────────────────┘
```

`MudSkeleton` per region. AppBar and drawer render immediately (no loading state — they don't depend on stub data).

## What this wireframe deliberately decides

- **Quick-capture lives in the AppBar**, so the operator can capture from any Page without navigating home — the Dashboard does not repeat it as a hero.
- **Needs Attention is first** in the main content, above Recent Captures. Failures get top billing.
- **Recent Captures shows lifecycle state** in its own column — orphan/unhandled rows are visually distinguishable in-line, so failures are findable even outside the Needs Attention widget.
- **Skill and Integration health are sibling cards**, side-by-side on desktop. They reuse the same shape so the operator's eye learns one pattern.
- **No charts in Block 2** — Faker data would make them theatre. Charts can land in a later block when real metrics exist.
- **Drawer expands on click**, not hover.
- **All four cards have a footer link** to their canonical Page so the Dashboard is a jumping-off point, not a dead end.

## Deliberately deferred

- **Page-flow** (`/ui-flow`, Phase 2): row click navigation, AppBar quick-capture submission flow, error toasts via `MudSnackbar`, dialog vs. page transitions.
- **Real C# / Razor** (`/ui-build`, Phase 3).
- **Tests** (`/ui-review`, Phase 4).
