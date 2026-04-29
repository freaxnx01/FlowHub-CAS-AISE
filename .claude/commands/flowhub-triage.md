Read open **Captures** (glossary: *Infoschnipsel*) from all wired Channels — Vikunja Inbox plus Telegram `@flowhub_intelliflow_bot` — propose target projects from the live project list, and (on accept) move them.

Args: $ARGUMENTS

**Flags:**
- `--limit N` (default **10**) — caps how many Captures are processed in one run.
- `--simulate` — don't move Captures; attach `triage-target:<...>` + `triage-conf:<high|medium|low>` labels instead. Already-simulated Captures (those carrying any `triage-target:*` label) are skipped on subsequent runs.
- `--only-issues` — after classification, hide rows whose action ≠ `issue`.
- `--no-telegram` — skip the Telegram drain (4a); read Vikunja only.

Follow the canonical skill body in `.ai/skills/flowhub-triage.md` exactly. It contains the credential lookup, project + task fetching, classification rules, proposal table format, edit walk, and apply loop.

## Quick reference

### Channels (inbound Capture sources)

- **Vikunja Inbox** (Integration used as Capture staging for QuickTask / Signal-bridge / manual entries / the Telegram drain).
- **Telegram** `@flowhub_intelliflow_bot` — drained via Bot API `getUpdates` every triage run.
  - Token: Passbolt resource `fd9897e7-544c-4109-8ee9-cc8eb1838ee5` ("Telegram flowhub_bot HTTP API Token").
  - Offset state file: `~/.cache/flowhub/telegram-offset` (plain integer, not secret — cursor only).
  - `getUpdates`: `https://api.telegram.org/bot<TOKEN>/getUpdates?offset=<N+1>&timeout=0`
  - `getFile` → file path → download from `https://api.telegram.org/file/bot<TOKEN>/<file_path>`
  - Source label on each Telegram-drained Capture: `channel:telegram` (`#0088cc`)
  - Reserved `channel:*` family: `channel:telegram`, `channel:quicktask`, `channel:signal`, `channel:manual` — see skill Step 4a for the taxonomy and who sets each.

### Vikunja API

- Vikunja: `https://todo.home.freaxnx01.ch`
- Inbox detection: `GET /api/v1/user.settings.default_project_id`
- Project list: `GET /api/v1/projects?per_page=200`
- Paginated Inbox fetch: `GET /api/v1/projects/{inbox_id}/tasks?per_page=200&page=N`
- Create Capture in inbox: `PUT /api/v1/projects/{inbox_id}/tasks` body `{"title":..., "description":..., "project_id":N}`
- Attach file: `PUT /api/v1/tasks/{id}/attachments` (multipart `files=@...`)
- Move Capture: `POST /api/v1/tasks/{id}` body `{"title":..., "project_id":N}` — **`title` is mandatory** (412 otherwise)
- Create project: `PUT /api/v1/projects` body `{"title":...}`
- Download attachment (for image enrichment): `GET /api/v1/tasks/{task_id}/attachments/{attachment_id}` → raw bytes
- Label lookup: `GET /api/v1/labels?s=<title>` (substring match)
- Create label: `PUT /api/v1/labels` body `{"title":..., "hex_color":"..."}`
- Attach label to task: `PUT /api/v1/tasks/{id}/labels` body `{"label_id":N}`
- Token source: Passbolt resource id `c9e732ce-7737-49a7-9879-dd81258083af`
- Passbolt master password: read from your memory file `passbolt-password.md` at runtime; never write to disk

## Hard rules

- **Tracking label.** After each successful move, apply `flowhub-triaged` label (id 17) via `PUT /api/v1/tasks/{id}/labels` with `{"label_id":17}`. Non-critical — warn on failure but don't stop.
- **Move-only.** Never `DELETE` a task or a project from this skill. The Vikunja memory file warns that project deletes cascade to all child tasks.
- **No invented project ids.** Only ids returned from the live project list are legal targets.
- **No state changes without `y`.** Plan-then-apply: print the proposal table once, ask `[y / N / edit]`, only then apply.
- **`title` always present** in `POST /tasks/{id}` and `POST /projects/{id}` payloads.
- **No silent failures.** If any apply step returns non-2xx, stop the apply loop and report which rows did/did not apply.
- **Never** persist the Passbolt password or Vikunja token to disk.
- This skill never creates new tasks. It only moves existing tasks (and may create new projects on explicit user accept during the edit walk).

## UX shape

Plan-then-apply: one proposal table, one confirm gate. `edit` walks per row to override the target. After apply, print a final summary `Triaged N tasks: M moved, K created+moved, S skipped, F failed`.
