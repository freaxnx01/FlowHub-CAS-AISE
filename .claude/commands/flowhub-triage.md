Read open tasks from the user's Vikunja Inbox, propose target projects from the live project list, and (on accept) move them.

Args: $ARGUMENTS

Optional `--limit N` (default 25) caps how many inbox tasks are processed in one run.

Follow the canonical skill body in `.ai/skills/flowhub-triage.md` exactly. It contains the credential lookup, project + task fetching, classification rules, proposal table format, edit walk, and apply loop.

## Quick reference

- Vikunja: `https://todo.home.freaxnx01.ch`
- Inbox detection: `GET /api/v1/user.settings.default_project_id`
- Project list: `GET /api/v1/projects?per_page=200`
- Inbox tasks: `GET /api/v1/projects/{inbox_id}/tasks?per_page=100` (filter `done=false`)
- Move task: `POST /api/v1/tasks/{id}` body `{"title":..., "project_id":N}` — **`title` is mandatory** (412 otherwise)
- Create project: `PUT /api/v1/projects` body `{"title":...}`
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
