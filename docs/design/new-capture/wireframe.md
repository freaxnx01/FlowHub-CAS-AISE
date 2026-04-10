# New Capture — Wireframe (Phase 1)

- **Page route:** `/captures/new`
- **Render mode:** Interactive Server (per ADR 0001)
- **Status:** Approved 2026-04-10
- **Phase:** 1 of 4 (`/ui-brainstorm`)
- **Next phase:** `/ui-flow` — Mermaid page-flow diagrams

## Settled inputs

| Decision | Choice |
|---|---|
| Scope | **Medium (option B)** — content field + optional Skill override dropdown. No file upload. |
| After submit | **Stay on page, clear form (option B)** — supports rapid multi-entry. Snackbar confirms each capture with an "Open" link. |
| Skill dropdown default | "— Let AI decide —" (null value) — operator overrides only when needed |
| Cancel target | Navigate to Dashboard (`/`) |

---

## Default state

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│ ☰  FlowHub   [ + Quick capture: paste URL or type… 🖇 attach            ⏎ ]  👤 │
├────┬────────────────────────────────────────────────────────────────────────────┤
│ ⌂  │                                                                            │
│ ⊞  │  New Capture                                                               │
│ +← │  ─────────────────────────────────────────────────────────────────          │
│ ⚙  │                                                                            │
│ ⇄  │  ┌─ MudCard ─────────────────────────────────────────────────────────┐     │
│    │  │                                                                    │     │
│    │  │  Content *                                                         │     │
│    │  │  ┌────────────────────────────────────────────────────────────┐    │     │
│    │  │  │                                                            │    │     │
│    │  │  │  (multi-line MudTextField, 5 rows)                         │    │     │
│    │  │  │  Paste a URL, type a quote, describe what you captured…    │    │     │
│    │  │  │                                                            │    │     │
│    │  │  │                                                            │    │     │
│    │  │  └────────────────────────────────────────────────────────────┘    │     │
│    │  │  Helper: "Paste a URL, type a quote, or describe what              │     │
│    │  │          you want to capture."                                     │     │
│    │  │                                                                    │     │
│    │  │  Skill override (optional)                                         │     │
│    │  │  ┌────────────────────────────────────────────┐                    │     │
│    │  │  │  — Let AI decide —                       ▼ │                    │     │
│    │  │  └────────────────────────────────────────────┘                    │     │
│    │  │  Helper: "Leave on 'Let AI decide' for automatic classification.   │     │
│    │  │          Override only if you know which Skill should handle this." │     │
│    │  │                                                                    │     │
│    │  │  ┌─────────────────┐                                               │     │
│    │  │  │   ⏎  Submit     │   [ Cancel ]                                  │     │
│    │  │  └─────────────────┘                                               │     │
│    │  │                                                                    │     │
│    │  └────────────────────────────────────────────────────────────────────┘     │
│    │                                                                            │
└────┴────────────────────────────────────────────────────────────────────────────┘
```

## Region map

| # | Region | MudBlazor mapping | Notes |
|---|---|---|---|
| 1 | Page title | `MudText` Typo.h5 | "New Capture" — matches drawer's active item |
| 2 | Form card | `MudCard` → `MudCardContent` → `MudForm` | Single card, outlined, max-width ~700px for readability |
| 3 | Content field | `MudTextField` `T="string"` `Lines="5"` `Required="true"` | Multi-line, placeholder text as helper, `RequiredError="Content is required"` |
| 4 | Skill override | `MudSelect<string?>` with a default null item "— Let AI decide —" | Populated from `ISkillRegistry.GetHealthAsync()` — same service the Dashboard uses. Each option shows the Skill name. |
| 5 | Submit button | `MudButton` `Variant.Filled` `Color.Primary` | Disabled when submitting (spinner icon swap). On success → snackbar + clear form + stay on page. |
| 6 | Cancel button | `MudButton` `Variant.Text` | Navigates to `/` (Dashboard) |

## Skill override dropdown — expanded

```
┌────────────────────────────────────────────┐
│  — Let AI decide —                       ▼ │
├────────────────────────────────────────────┤
│  — Let AI decide —                (default) │
│  Books                                      │
│  Movies                                     │
│  Articles                                   │
│  Quotes                                     │
│  Knowledge                                  │
│  Belege                                     │
│  Homelab                                    │
└─────────────────────────────────────────────┘
```

## Success state — after Submit (stay on page, clear form)

```
┌─ MudSnackbar (top-right) ──────────────────────┐
│  ✓  Captured — "Inception (2010)…"    [ Open ] │
└────────────────────────────────────────────────┘

Form clears: content empty, Skill override resets to "— Let AI decide —".
Page stays on /captures/new — ready for the next entry.
```

## Validation error — Content empty

```
  Content *
  ┌────────────────────────────────────────────┐
  │                                            │
  └────────────────────────────────────────────┘
  ⚠ Content is required
```

Standard MudBlazor `Required` + `RequiredError` — no custom logic needed.

## Submitting state — in-flight

```
  ┌─────────────────┐
  │   ⏳ Submitting  │   [ Cancel ]  (disabled)
  └─────────────────┘
```

Submit button shows spinner, both buttons disabled until the service call returns.

## Design decisions

- **Single card, not a wizard.** Two fields don't justify a multi-step flow.
- **"Let AI decide" is the default** — operator overrides only when needed. Fast for the 90% case.
- **Skill dropdown reuses `ISkillRegistry`** — no new service interface. Degraded/Down skills still appear (operator might want to queue).
- **Cancel goes to Dashboard**, not browser-back — predictable, testable.
- **Stay on page after submit** — supports rapid multi-entry. Snackbar confirms each capture. Dashboard shows them all when operator navigates back.
- **No file upload** — deferred to a later block.

## Deliberately deferred

- **Page-flow** (`/ui-flow`, Phase 2): submit flow, validation flow, error handling, service failure.
- **Code** (`/ui-build`, Phase 3).
- **Tests** (`/ui-review`, Phase 4).
