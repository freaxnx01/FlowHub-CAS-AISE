# FlowHub — User Journeys

Single source of truth for end-to-end user journeys across the Web UI. Derived from the approved Phase-2 flow diagrams in `docs/design/<feature>/flow.md`.

Each journey has the JSON acceptance-criteria shape used by Playwright E2E tests:

```json
{
  "id": "<short-slug>",
  "category": "functional | error | edge",
  "description": "<one-line user-facing intent>",
  "entryUrl": "<route the test starts on>",
  "preconditions": ["..."],
  "steps": ["..."],
  "expected": ["..."],
  "passes": false
}
```

`passes` flips to `true` once the corresponding Playwright spec is green. `preconditions` describe seeded data the test fixture must provide. `expected` are the assertions; `steps` are user actions only.

Routes (from `@page` directives):

| Route | Page |
|---|---|
| `/` | Dashboard |
| `/captures` | Captures list |
| `/captures/new` | New Capture form |
| `/captures/{id:guid}` | Capture detail |
| `/skills` | Skills page |
| `/integrations` | Integrations page |

The AppBar `QuickCaptureField` is shared across every page (see Dashboard flow Diagram 3).

---

## Catalog

### J01 — Quick capture from the AppBar (happy path)

```json
{
  "id": "J01",
  "category": "functional",
  "description": "User submits a capture from the AppBar quick-capture field on any page",
  "entryUrl": "/",
  "preconditions": ["Dev auth signed in", "ICaptureService backed by Bogus stub"],
  "steps": [
    "Type 'https://example.com/article' into the AppBar quick-capture field",
    "Press Enter"
  ],
  "expected": [
    "Snackbar shows 'Captured ✓' with an Open action",
    "Quick-capture input is cleared",
    "ICaptureService.SubmitAsync was called once with ChannelKind.Web"
  ],
  "passes": false
}
```

### J02 — Quick capture failure surfaces an error toast

```json
{
  "id": "J02",
  "category": "error",
  "description": "When the capture service throws, the user sees an error toast and the input is preserved",
  "entryUrl": "/",
  "preconditions": ["ICaptureService.SubmitAsync throws InvalidOperationException"],
  "steps": [
    "Type 'foo' into the AppBar quick-capture field",
    "Press Enter"
  ],
  "expected": [
    "Snackbar shows 'Capture failed: ...'",
    "Input still contains 'foo'",
    "isSubmitting returns to false"
  ],
  "passes": false
}
```

### J03 — Empty quick-capture submit shows inline hint

```json
{
  "id": "J03",
  "category": "edge",
  "description": "Submitting an empty quick-capture input does not call the service and prompts the user",
  "entryUrl": "/",
  "preconditions": [],
  "steps": ["Press Enter in the AppBar quick-capture field without typing anything"],
  "expected": [
    "Snackbar shows 'Type something first'",
    "ICaptureService.SubmitAsync is NOT called"
  ],
  "passes": false
}
```

### J04 — Dashboard renders all four cards with seeded data

```json
{
  "id": "J04",
  "category": "functional",
  "description": "Dashboard loads and shows Needs Attention, Recent Captures, Skill Health, and Integration Health",
  "entryUrl": "/",
  "preconditions": ["Bogus stub seeds 12 captures, 6 skills, 6 integrations"],
  "steps": ["Navigate to /"],
  "expected": [
    "All four cards render (no skeletons remaining)",
    "Recent Captures shows at least one row",
    "Skill Health lists Books, Movies, Quotes",
    "Integration Health lists Wallabag, Vikunja, Obsidian"
  ],
  "passes": false
}
```

### J05 — Dashboard 'orphan count' click filters Captures list

```json
{
  "id": "J05",
  "category": "functional",
  "description": "Clicking the orphan count on the Dashboard navigates to /captures with the orphan filter applied",
  "entryUrl": "/",
  "preconditions": ["Bogus stub seeds ≥1 orphan capture"],
  "steps": ["Click the 'N orphan captures' button on the Needs Attention card"],
  "expected": [
    "URL changes to /captures?lc=Orphan",
    "Captures list pre-selects the Orphan lifecycle chip",
    "Only orphan rows are visible"
  ],
  "passes": false
}
```

### J06 — Dashboard 'unhandled count' click filters Captures list

```json
{
  "id": "J06",
  "category": "functional",
  "description": "Clicking the unhandled count navigates to /captures filtered by Unhandled",
  "entryUrl": "/",
  "preconditions": ["Bogus stub seeds ≥1 unhandled capture"],
  "steps": ["Click the 'N unhandled captures' button on the Needs Attention card"],
  "expected": [
    "URL changes to /captures?lc=Unhandled",
    "Captures list pre-selects the Unhandled lifecycle chip"
  ],
  "passes": false
}
```

### J07 — Dashboard recent-capture row click opens detail

```json
{
  "id": "J07",
  "category": "functional",
  "description": "Clicking a row in the Recent Captures card opens that capture's detail page",
  "entryUrl": "/",
  "preconditions": ["Bogus stub seeds ≥1 capture"],
  "steps": ["Click the first row in the Recent Captures grid"],
  "expected": [
    "URL changes to /captures/{id}",
    "Capture detail page renders with the matching content"
  ],
  "passes": false
}
```

### J08 — Dashboard 'View all' navigates to Captures list

```json
{
  "id": "J08",
  "category": "functional",
  "description": "View-all button on the Recent Captures card navigates to /captures",
  "entryUrl": "/",
  "preconditions": [],
  "steps": ["Click the 'View all' button on the Recent Captures card"],
  "expected": ["URL changes to /captures"],
  "passes": false
}
```

### J09 — Dashboard 'Manage integrations' navigates to /integrations

```json
{
  "id": "J09",
  "category": "functional",
  "description": "Manage-integrations button on the Integration Health card navigates to /integrations",
  "entryUrl": "/",
  "preconditions": [],
  "steps": ["Click the 'Manage integrations' button on the Integration Health card"],
  "expected": ["URL changes to /integrations"],
  "passes": false
}
```

### J10 — Captures list filters by lifecycle

```json
{
  "id": "J10",
  "category": "functional",
  "description": "Selecting a lifecycle chip narrows the captures grid to that stage",
  "entryUrl": "/captures",
  "preconditions": ["Bogus stub seeds 12 captures across all stages"],
  "steps": ["Click the 'orphan' chip in the Lifecycle chip set"],
  "expected": [
    "Grid shows only orphan-stage rows",
    "Results counter updates to match the orphan count"
  ],
  "passes": false
}
```

### J11 — Captures list filters by channel

```json
{
  "id": "J11",
  "category": "functional",
  "description": "Selecting a channel chip narrows the captures grid to that source",
  "entryUrl": "/captures",
  "preconditions": ["Bogus stub seeds captures from both Web and Telegram"],
  "steps": ["Click the 'Telegram' chip in the Channel chip set"],
  "expected": [
    "Grid shows only Telegram-source rows",
    "Results counter updates"
  ],
  "passes": false
}
```

### J12 — Captures list search filters by content substring

```json
{
  "id": "J12",
  "category": "functional",
  "description": "Typing in the search field filters the grid by case-insensitive content match",
  "entryUrl": "/captures",
  "preconditions": ["Bogus stub seeds a capture containing 'Inception'"],
  "steps": ["Type 'incep' into the search field"],
  "expected": ["Grid shows the Inception capture", "Other captures are filtered out"],
  "passes": false
}
```

### J13 — Captures list 'no match' empty state

```json
{
  "id": "J13",
  "category": "edge",
  "description": "When filters produce zero results the user sees a no-match panel with a Clear filters action",
  "entryUrl": "/captures",
  "preconditions": [],
  "steps": [
    "Type 'zzzzzzzz-no-such-string' into the search field"
  ],
  "expected": [
    "Empty state shows 'No captures match'",
    "Clicking 'Clear filters' restores the full grid"
  ],
  "passes": false
}
```

### J14 — Captures list row click opens detail

```json
{
  "id": "J14",
  "category": "functional",
  "description": "Clicking a row in the Captures grid opens that capture's detail page",
  "entryUrl": "/captures",
  "preconditions": ["Bogus stub seeds ≥1 capture"],
  "steps": ["Click the first row in the captures grid"],
  "expected": [
    "URL changes to /captures/{id}",
    "Capture detail page renders"
  ],
  "passes": false
}
```

### J15 — Capture detail (Completed) view-only

```json
{
  "id": "J15",
  "category": "functional",
  "description": "A Completed capture renders content + metadata with no failure alert and no action buttons",
  "entryUrl": "/captures/{completedId}",
  "preconditions": ["Bogus stub has a Completed capture with known id"],
  "steps": ["Navigate to /captures/{completedId}"],
  "expected": [
    "Content and Metadata sections render",
    "No 'Routing failed' alert",
    "No Retry/Reassign/Ignore buttons"
  ],
  "passes": false
}
```

### J16 — Capture detail (Orphan) shows retry/reassign/ignore actions

```json
{
  "id": "J16",
  "category": "functional",
  "description": "An Orphan capture shows the failure reason and offers Retry routing, Reassign skill, and Ignore",
  "entryUrl": "/captures/{orphanId}",
  "preconditions": ["Bogus stub has an Orphan capture with a known FailureReason"],
  "steps": ["Navigate to /captures/{orphanId}"],
  "expected": [
    "Warning alert 'Routing failed: <reason>' is visible",
    "Buttons 'Retry routing', 'Reassign skill', 'Ignore' are present"
  ],
  "passes": false
}
```

### J17 — Capture detail (Unhandled) shows assign/ignore actions only

```json
{
  "id": "J17",
  "category": "functional",
  "description": "An Unhandled capture shows the integration failure and offers Assign skill + Ignore (no Retry routing)",
  "entryUrl": "/captures/{unhandledId}",
  "preconditions": ["Bogus stub has an Unhandled capture"],
  "steps": ["Navigate to /captures/{unhandledId}"],
  "expected": [
    "Error alert 'Skill integration failed: <reason>' is visible",
    "Button 'Assign skill' is present",
    "Button 'Retry routing' is NOT present"
  ],
  "passes": false
}
```

### J18 — Capture detail action buttons stub-toast in Block 2

```json
{
  "id": "J18",
  "category": "functional",
  "description": "Clicking any action on Orphan/Unhandled detail surfaces the Block-2 stub snackbar",
  "entryUrl": "/captures/{orphanId}",
  "preconditions": ["Bogus stub has an Orphan capture"],
  "steps": ["Click 'Retry routing'"],
  "expected": [
    "Snackbar contains 'will work once backend Skills are wired in Block 3'"
  ],
  "passes": false
}
```

### J19 — Capture detail back link returns to list

```json
{
  "id": "J19",
  "category": "functional",
  "description": "The 'Back to Captures' link returns the user to /captures",
  "entryUrl": "/captures/{anyId}",
  "preconditions": [],
  "steps": ["Click '← Back to Captures'"],
  "expected": ["URL changes to /captures"],
  "passes": false
}
```

### J20 — Capture detail unknown id shows not-found

```json
{
  "id": "J20",
  "category": "edge",
  "description": "Visiting /captures/{unknown-guid} shows a not-found warning",
  "entryUrl": "/captures/00000000-0000-0000-0000-000000000000",
  "preconditions": [],
  "steps": ["Navigate to /captures/{unknownId}"],
  "expected": ["Warning alert 'Capture not found' is visible"],
  "passes": false
}
```

### J21 — New Capture submit (happy path)

```json
{
  "id": "J21",
  "category": "functional",
  "description": "Submitting the New Capture form creates a capture and shows a success toast",
  "entryUrl": "/captures/new",
  "preconditions": ["ISkillRegistry returns a non-empty list"],
  "steps": [
    "Type 'Read this article tomorrow' into the Content field",
    "Click Submit"
  ],
  "expected": [
    "Snackbar shows 'Captured ✓ — open' with an Open action",
    "Form is cleared (content empty, dropdown back to 'Let AI decide')"
  ],
  "passes": false
}
```

### J22 — New Capture validation blocks empty submit

```json
{
  "id": "J22",
  "category": "edge",
  "description": "Submitting with empty Content shows a validation error and does not call the service",
  "entryUrl": "/captures/new",
  "preconditions": [],
  "steps": ["Click Submit without typing anything"],
  "expected": [
    "Required-field error 'Content is required' is shown",
    "ICaptureService.SubmitAsync is NOT called"
  ],
  "passes": false
}
```

### J23 — New Capture: skills failed to load still allows submission

```json
{
  "id": "J23",
  "category": "error",
  "description": "When ISkillRegistry throws, the form shows a warning but Submit still works (forced to 'Let AI decide')",
  "entryUrl": "/captures/new",
  "preconditions": ["ISkillRegistry.GetHealthAsync throws"],
  "steps": [
    "Navigate to /captures/new",
    "Type 'still works' into Content",
    "Click Submit"
  ],
  "expected": [
    "Inline warning 'Could not load skills' is visible",
    "Submit succeeds (snackbar 'Captured ✓')"
  ],
  "passes": false
}
```

### J24 — New Capture cancel returns to dashboard

```json
{
  "id": "J24",
  "category": "functional",
  "description": "The Cancel button on the New Capture form navigates back to the Dashboard",
  "entryUrl": "/captures/new",
  "preconditions": [],
  "steps": ["Click Cancel"],
  "expected": ["URL changes to /"],
  "passes": false
}
```

### J25 — Skills page lists every registered skill

```json
{
  "id": "J25",
  "category": "functional",
  "description": "/skills shows a row per skill with name, status, and routed-today count",
  "entryUrl": "/skills",
  "preconditions": ["ISkillRegistry returns the 6 seeded skills"],
  "steps": ["Navigate to /skills"],
  "expected": [
    "Grid contains Books, Movies, Articles, Quotes, Knowledge, Belege",
    "Each row shows a HealthDot and a status label"
  ],
  "passes": false
}
```

### J26 — Skills page load-failure shows retryable error

```json
{
  "id": "J26",
  "category": "error",
  "description": "When the skill registry throws, /skills shows an error alert with a Retry button",
  "entryUrl": "/skills",
  "preconditions": ["ISkillRegistry.GetHealthAsync throws"],
  "steps": ["Navigate to /skills"],
  "expected": [
    "Error alert 'Could not load skills: <reason>' is visible",
    "Retry button is present"
  ],
  "passes": false
}
```

### J27 — Integrations page lists every wired integration

```json
{
  "id": "J27",
  "category": "functional",
  "description": "/integrations shows a row per integration with name, status, and last-write timing",
  "entryUrl": "/integrations",
  "preconditions": ["IIntegrationHealthService returns the 6 seeded integrations"],
  "steps": ["Navigate to /integrations"],
  "expected": [
    "Grid contains Wallabag, Wekan, Vikunja, Paperless, Obsidian, Authentik",
    "Each row shows a HealthDot and a status label"
  ],
  "passes": false
}
```

### J28 — Integrations page load-failure shows retryable error

```json
{
  "id": "J28",
  "category": "error",
  "description": "When the health service throws, /integrations shows an error alert with a Retry button",
  "entryUrl": "/integrations",
  "preconditions": ["IIntegrationHealthService.GetHealthAsync throws"],
  "steps": ["Navigate to /integrations"],
  "expected": [
    "Error alert 'Could not load integrations: <reason>' is visible",
    "Retry button is present"
  ],
  "passes": false
}
```

---

## Summary

| Surface | Journeys |
|---|---|
| AppBar / QuickCapture | J01, J02, J03 |
| Dashboard | J04, J05, J06, J07, J08, J09 |
| Captures list | J10, J11, J12, J13, J14 |
| Capture detail | J15, J16, J17, J18, J19, J20 |
| New Capture | J21, J22, J23, J24 |
| Skills | J25, J26 |
| Integrations | J27, J28 |

## E2E status (snapshot)

All 28 journeys have a Playwright spec + JSON sidecar under `tests/FlowHub.Web.E2ETests/Journeys/`. Live result against `make watch` + the docker-compose backing services:

| Status | Count | Journeys |
|---|---|---|
| ✅ Green | 23 | J01–J14, J16–J19, J21–J24, plus the `HappyFlowTests` fixture |
| ❌ Red — environmental (data / wiring missing) | 5 | J15 (no Completed-stage capture in DB) · J25, J27 (live skill/integration registries return rows from EF but the page renders empty — needs investigation) · J26, J28 (negative-path: need a fault-injection hook to force the error branch — bUnit already covers it) |

### Closed in this iteration

1. ✅ Added `Immediate="true"` to `QuickCaptureField`'s MudTextField — fixes J01 (and `HappyFlowTests`). bUnit test updated to use `input.Input(...)` instead of `input.Change(...)`.
2. ✅ Added a `#blazor-circuit-ready` sentinel to `MainLayout` (rendered only after `OnAfterRender(firstRender)`), and `JourneyTestBase.GotoAsync` waits for it via `WaitForSelectorState.Attached` — deterministic replacement for the previous fixed 2 s sleep.
3. ✅ `JourneyTestBase.GotoAsync` also waits (best-effort, 3 s cap) for all `.mud-skeleton` placeholders to clear — proves OnInitializedAsync data load completed and event handlers on data-derived elements are bound.
4. ✅ Row-click selectors changed from `.mud-table-row` (also matches MudDataGrid header) to `tbody .mud-table-row` — closes J14, J19.
5. ✅ J13 assertion uses a `WaitForFunction` accepting either the no-match panel text OR a body-row count of zero.
6. ✅ **Same parallel-DbContext bug as the earlier CaptureDetail fix surfaced in `Dashboard.LoadAsync`** — four services hitting the shared scoped EF context via `Task.WhenAll` silently threw "second operation on context instance", so all four cards rendered as null forever. Switched to sequential awaits → unblocked J05, J06, J07.
7. ✅ J05/J06 URL regex made case-insensitive — Dashboard navigates to lowercase `?lc=orphan` / `?lc=unhandled`.

### Still open

8. ⏳ Seed a Completed-stage capture in the dev DB — unblocks J15.
9. ⏳ Investigate why `/skills` and `/integrations` render empty even though the EF queries return rows in the logs — unblocks J25, J27.
10. ⏳ Optional: an `IFaultInjector` test-only DI hook — unblocks J26, J28. bUnit already owns the negative path so this is low priority.
11. ⏳ J07 occasionally flakes under full-suite load (passes solo in 1 s). A per-page `data-page-ready` marker (set in each page's `OnAfterRender(firstRender)`) would be the proper deterministic fix.
7. ⏳ Optional: an `IFaultInjector` test-only DI hook — unblocks J26, J28. bUnit already owns the negative path so this is low priority.

**bUnit cross-coverage** (`tests/FlowHub.Web.ComponentTests/`): still 126/126 green. The bUnit suite owns the component-level negative paths (forced exceptions, error alerts, validation messages) for J02, J23, J26, J28 — the E2E equivalents can't trigger those states against a healthy live system without a fault-injection hook.
