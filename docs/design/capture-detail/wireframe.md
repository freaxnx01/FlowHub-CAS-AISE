# Capture Detail — Wireframe (Phase 1)

- **Page route:** `/captures/{id:guid}`
- **Render mode:** Interactive Server (per ADR 0001)
- **Status:** Approved 2026-04-10
- **Phase:** 1 of 4 (`/ui-brainstorm`)
- **Next phase:** `/ui-flow` — Mermaid page-flow diagrams

## Settled inputs

| Decision | Choice |
|---|---|
| Actions scope | **Stubbed (option B)** — buttons exist, show "Coming in Block 3" snackbar on click |
| New service method | `GetByIdAsync(Guid)` added to `ICaptureService` |
| Model change | Add `FailureReason` (nullable string) to `Capture` record |
| Back navigation | "Back to Captures" → `/captures` (not browser-back) |

---

## Default state — Completed capture

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│ ☰  FlowHub   [ + Quick capture: paste URL or type… 🖇 attach            ⏎ ]  👤 │
├────┬────────────────────────────────────────────────────────────────────────────┤
│ ⌂  │                                                                            │
│ ⊞  │  ← Back to Captures                                                       │
│ +  │                                                                            │
│ ⚙  │  ┌─ MudCard ─────────────────────────────────────────────────────────┐     │
│ ⇄  │  │                                                                    │     │
│    │  │  ┌─ Header row ──────────────────────────────────────────────┐    │     │
│    │  │  │  [✓ completed]   📱 Telegram   2 min ago                  │    │     │
│    │  │  │  Skill: Movies                                            │    │     │
│    │  │  └───────────────────────────────────────────────────────────┘    │     │
│    │  │                                                                    │     │
│    │  │  Content                                                          │     │
│    │  │  ─────────────────────────────────────────────────────            │     │
│    │  │  Inception (2010) — rewatch                                       │     │
│    │  │                                                                    │     │
│    │  │  Metadata                                                         │     │
│    │  │  ─────────────────────────────────────────────────────            │     │
│    │  │  ID:       a1b2c3d4-e5f6-...                                      │     │
│    │  │  Created:  2026-04-10 15:42:03 UTC                                │     │
│    │  │  Source:   Telegram                                               │     │
│    │  │  Stage:    Completed                                              │     │
│    │  │  Skill:    Movies                                                 │     │
│    │  │                                                                    │     │
│    │  └────────────────────────────────────────────────────────────────────┘     │
│    │                                                                            │
└────┴────────────────────────────────────────────────────────────────────────────┘
```

## Orphan variant — failure reason + action buttons

```
│  ┌─ Header row ──────────────────────────────────────────────┐    │
│  │  [⚠ orphan]   📱 Telegram   8 min ago                     │    │
│  │  Skill: Books (failed)                                     │    │
│  └───────────────────────────────────────────────────────────┘    │
│                                                                    │
│  ┌─ MudAlert Severity.Warning ──────────────────────────────┐    │
│  │  ⚠ Routing failed: Wallabag API returned 503 Service      │    │
│  │    Unavailable — the Integration was unreachable.         │    │
│  └───────────────────────────────────────────────────────────┘    │
│                                                                    │
│  Content                                                          │
│  ─────────────────────────────────────────────────────            │
│  https://galaxus.ch/de/s18/product/eine-kurze-geschichte-...      │
│                                                                    │
│  Metadata                                                         │
│  ...                                                              │
│                                                                    │
│  Actions                                                          │
│  ─────────────────────────────────────────────────────            │
│  [ 🔄  Retry routing ]   [ ✏️  Reassign skill ▼ ]   [ 🚫  Ignore ]│
│                                                                    │
```

## Unhandled variant — no-skill message + assign action

```
│  ┌─ Header row ──────────────────────────────────────────────┐    │
│  │  [❓ unhandled]   🖥 Web   17 min ago                      │    │
│  │  Skill: —                                                  │    │
│  └───────────────────────────────────────────────────────────┘    │
│                                                                    │
│  ┌─ MudAlert Severity.Info ─────────────────────────────────┐    │
│  │  ❓ No Skill matched this Capture. The AI classifier       │    │
│  │    could not determine a category.                         │    │
│  └───────────────────────────────────────────────────────────┘    │
│                                                                    │
│  Content                                                          │
│  ...                                                              │
│                                                                    │
│  Actions                                                          │
│  ─────────────────────────────────────────────────────            │
│  [ ✏️  Assign skill ▼ ]                              [ 🚫  Ignore ]│
│                                                                    │
```

## Region map

| # | Region | MudBlazor mapping | Notes |
|---|---|---|---|
| 1 | Back link | `MudButton` `Variant.Text` `StartIcon=ArrowBack` | → `/captures` |
| 2 | Main card | `MudCard` Outlined, max-width ~800px | Single card, all sections |
| 3 | Header row | `MudStack` Row in `MudCardHeader` | `LifecycleBadge` + channel icon + relative time + Skill |
| 4 | Failure alert | `MudAlert` `Severity.Warning` (orphan) or `Severity.Info` (unhandled) | Only for Orphan/Unhandled. Shows `FailureReason` or generic no-match message. |
| 5 | Content | `MudText` Typo.body1 | Full content, no truncation |
| 6 | Metadata | `MudStack` with label/value pairs | ID, Created (absolute), Source, Stage, Skill |
| 7 | Actions | `MudStack` Row with `MudButton`s | Only for Orphan/Unhandled. All **stubbed** → snackbar "Coming in Block 3". |

## Action buttons per stage

| Stage | Retry | Reassign/Assign | Ignore |
|---|---|---|---|
| Completed | — | — | — |
| Routed | — | — | — |
| Raw / Classified | — | — | — |
| **Orphan** | ✅ Retry routing | ✅ Reassign skill (MudMenu dropdown) | ✅ Ignore |
| **Unhandled** | — | ✅ Assign skill (MudMenu dropdown) | ✅ Ignore |

## Stubbed action behavior (Block 2)

```
┌─ MudSnackbar (top-right) ──────────────────────────────────┐
│  ℹ  This action will work once backend Skills are wired    │
│     in Block 3.                                            │
└────────────────────────────────────────────────────────────┘
```

No state change, no mutation.

## Loading state

```
│  ┌─ MudCard ─────────────────────────────────────────────┐     │
│  │  ▒▒▒▒▒▒▒▒▒▒  ▒▒  ▒▒▒▒▒▒▒▒                            │     │
│  │                                                        │     │
│  │  Content                                               │     │
│  │  ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒              │     │
│  │                                                        │     │
│  │  Metadata                                              │     │
│  │  ▒▒▒▒▒▒▒▒  ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒                        │     │
│  │  ▒▒▒▒▒▒▒▒  ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒                        │     │
│  └────────────────────────────────────────────────────────┘     │
```

## Not-found state

```
│  ┌─ MudAlert Severity.Warning ──────────────────────────┐     │
│  │  Capture not found. It may have been deleted.         │     │
│  └───────────────────────────────────────────────────────┘     │
│                                                                 │
│  [ ← Back to Captures ]                                        │
```

## Design decisions

- **Single card** — read-heavy view, one card with sections.
- **Back to Captures** — not browser-back. Predictable.
- **Failure alert only for Orphan/Unhandled** — healthy stages don't need a banner.
- **Actions only for Orphan/Unhandled** — nothing to fix on healthy Captures.
- **All actions stubbed** — per option B. Honest, no fake mutations.
- **`FailureReason`** — new nullable string on `Capture` record, populated only for Orphan stage by the stub.
- **Content in full** — no truncation on the detail view.

## Deliberately deferred

- **Page-flow** (`/ui-flow`, Phase 2).
- **Code** (`/ui-build`, Phase 3).
- **Tests** (`/ui-review`, Phase 4).
