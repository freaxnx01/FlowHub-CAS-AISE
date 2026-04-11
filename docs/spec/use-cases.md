# FlowHub — Use Cases & Requirements

## Solution Vision

FlowHub is an intelligent integration hub that captures heterogeneous inputs (URLs, text, images) from multiple channels (Telegram, Web UI), classifies them via AI, and routes them to the correct downstream service (Wallabag, Wekan, Vikunja, Paperless-ngx, Obsidian). It automates 80% of the manual workflow the operator currently performs by hand: receive input → decide where it goes → copy-paste into the right tool.

See `Projektarbeit/Idee FlowHub.md` in the CAS Obsidian vault for the original concept and management summary.

---

## Use Cases

### UC-01: Submit a Capture via Web UI (Quick)

**Actor:** Operator
**Trigger:** Operator pastes a URL or types text in the AppBar quick-capture field.
**Precondition:** Operator is authenticated (DevAuthHandler in dev, Authentik OIDC in prod).
**Flow:**
1. Operator types or pastes content into the AppBar quick-capture field.
2. Operator presses Enter or clicks the submit icon.
3. System creates a new Capture with `source = Web`, `stage = Raw`.
4. System shows a success snackbar with a link to the Capture detail.
5. The quick-capture field clears, ready for the next input.

**Postcondition:** A new Capture exists in the system with `LifecycleStage.Raw`.
**Error:** Empty content → inline validation hint "Type something first".
**Error:** Service failure → snackbar "Capture failed: {reason}", field content preserved.

### UC-02: Submit a Capture via Web UI (Long Form)

**Actor:** Operator
**Trigger:** Operator navigates to `/captures/new`.
**Precondition:** Authenticated.
**Flow:**
1. System loads available Skills from `ISkillRegistry`.
2. Operator enters multi-line content in the text field.
3. Optionally, operator selects a Skill override from the dropdown (default: "Let AI decide").
4. Operator clicks Submit.
5. System validates (content required), then creates the Capture.
6. System shows success snackbar. Form clears; page stays for rapid multi-entry.

**Postcondition:** A new Capture exists. If Skill override was selected, the Capture may skip AI classification.
**Error:** Skill registry fails to load → dropdown disabled, "Let AI decide" forced, Submit still works.

### UC-03: Submit a Capture via Telegram

**Actor:** Operator (via Telegram bot)
**Trigger:** Operator sends a message to the FlowHub Telegram bot.
**Precondition:** Telegram bot is configured and running (Block 3+).
**Flow:**
1. Telegram bot receives the message.
2. Bot creates a Capture with `source = Telegram`, `stage = Raw`.
3. Bot confirms receipt in the chat.

**Postcondition:** Capture exists, visible in the Web UI Dashboard and Captures list.
**Note:** Not implemented in Block 2 — placeholder `source/FlowHub.Telegram/`.

### UC-04: Monitor Capture health via Dashboard

**Actor:** Operator
**Trigger:** Operator opens the Dashboard (`/`).
**Flow:**
1. System loads failure counts, recent captures (10), skill health, and integration health in parallel.
2. Dashboard shows the "Needs Attention" widget with orphan and unhandled counts.
3. Dashboard shows the "Recent Captures" grid with lifecycle badges.
4. Dashboard shows Skill and Integration health cards.

**Postcondition:** Operator has an at-a-glance view of system health.
**Empty state:** No captures → "No captures yet" with a call-to-action.
**All-clear state:** Zero failures → calm "All captures routed successfully" message.

### UC-05: Browse and filter Captures

**Actor:** Operator
**Trigger:** Operator navigates to `/captures` (or clicks through from Dashboard).
**Flow:**
1. System loads all Captures.
2. If `?lc=` query param is present, the lifecycle filter pre-selects.
3. Operator filters by lifecycle stage (chip bar), channel (chip bar), and/or content text (search field).
4. Grid shows filtered results with pagination.
5. Operator clicks a row to drill into the Capture detail.

**Postcondition:** Operator can find any Capture by stage, channel, or content.

### UC-06: Inspect and act on a failed Capture

**Actor:** Operator
**Trigger:** Operator opens a Capture detail page (`/captures/{id}`) for an Orphan or Unhandled Capture.
**Flow (Orphan):**
1. System shows the failure reason alert.
2. Operator sees action buttons: Retry routing, Reassign skill, Ignore.
3. Operator clicks an action (Block 2: stubbed → snackbar "Coming in Block 3").

**Flow (Unhandled):**
1. System shows "No Skill matched" alert.
2. Operator sees action buttons: Assign skill, Ignore.
3. Operator clicks an action (Block 2: stubbed).

**Postcondition (Block 3+):** Capture is retried, reassigned, or marked as ignored. Dashboard counts update.
**Block 2:** Actions show intent but do not mutate state.

### UC-07: View Skill and Integration health

**Actor:** Operator
**Trigger:** Operator navigates to `/skills` or `/integrations`.
**Flow:**
1. System loads health data from the respective service.
2. Grid shows each Skill/Integration with status (healthy/degraded/down) and activity metrics.

**Postcondition:** Operator knows which Skills/Integrations are operational.

---

## Non-Functional Requirements (SMART)

| ID | Requirement | Measurable target | Verified by |
|---|---|---|---|
| NF-01 | **Response time** — Dashboard loads within acceptable time on a single-operator workstation | < 500 ms from navigation to all 4 cards rendered (on localhost with Bogus stubs) | Manual observation, future Playwright timing |
| NF-02 | **Availability** — FlowHub runs as a self-hosted service in the operator's homelab | 99% uptime during operator's active hours (not 24/7 SLA) | Docker healthcheck in Block 5 |
| NF-03 | **Concurrency** — system supports a single concurrent operator | 1 concurrent SignalR circuit without degradation | Architecture (Interactive Server, no horizontal scaling needed) |
| NF-04 | **Security** — all pages require authentication | 0 pages accessible without a valid auth session | `[Authorize]` on pages + DevAuthHandler in dev, OIDC in prod |
| NF-05 | **Testability** — all UI components testable in isolation | 100% of page components renderable in bUnit without a running server | bUnit test suite (currently 31 tests) |
| NF-06 | **Maintainability** — code compiles with zero warnings | `TreatWarningsAsErrors=true` in `Directory.Build.props` | `make build` in CI |
| NF-07 | **Portability** — runs on Linux (homelab Docker) and WSL2 (dev) | `make run` works on both environments | Manual verification |
| NF-08 | **Data privacy** — no Capture content leaves the operator's infrastructure | 0 external API calls for data processing (AI classification runs locally via Ollama in future blocks) | Architecture review |
