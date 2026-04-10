# FlowHub Vikunja CC-Skills — Design

**Status:** Approved (auto-applied recommendations 2026-04-09)
**Scope:** Sub-project #1 of the FlowHub CC-Skill family — Vikunja Inbox triage.
**Out of scope:** the QuickTask repo-issue skill, the multi-skill dispatcher, the .NET FlowHub app itself.

---

## 1. Glossary disambiguation

The word **Skill** is overloaded in this project:

| Term | Meaning |
|---|---|
| **FlowHub-Skill** *(C# class, future)* | An `ISkill` implementation in the .NET app that classifies a Capture and writes to an Integration. Defined in `Projektarbeit/Glossary.md`. |
| **CC-Skill** *(this design)* | A Claude Code skill (markdown file in `.ai/skills/` + `.claude/commands/`) executed by the assistant. Manual workflow stand-in until the .NET FlowHub-Skills exist. |

This document is exclusively about **CC-Skills**. They are *not* the .NET `ISkill` classes. They share intent, not code.

---

## 2. What we are building

Two single-purpose CC-Skills:

| CC-Skill | Purpose | Direction |
|---|---|---|
| `flowhub-capture` | Take a free-form input (URL or text) and create a task in the user's Vikunja Inbox | write |
| `flowhub-triage` | Read open tasks from the Vikunja Inbox, propose target projects from the live project list, and (on accept) move them | read + write |

Both skills target one Integration only: **Vikunja** at `https://todo.home.freaxnx01.ch`.

---

## 3. Design decisions (locked)

| # | Decision | Rationale |
|---|---|---|
| D1 | Two CC-skills, not one | Single-responsibility; `flowhub-capture` will later be the seam where the .NET FlowHub plugs in for any Channel. |
| D2 | Vikunja-only for v1 | Focus. Other Integrations (Wallabag, Wekan, Obsidian) get their own CC-skills later. |
| D3 | Dynamic project discovery — no hardcoded category list | The classifier picks from the user's actual Vikunja project list at runtime. Mirrors FlowHub's "no hardcoded routing table" intent. |
| D4 | Inbox detection via `GET /api/v1/user.settings.default_project_id` | Vikunja API does not expose `is_inbox` on `/projects`; the user's `default_project_id` is the canonical inbox pointer. Verified empirically — currently `2`. |
| D5 | Auth via Passbolt at runtime | Per `feedback_credentials_passbolt_only`. Token resource UUID `c9e732ce-7737-49a7-9879-dd81258083af` ("Vikunja API Token 'api-token'"). |
| D6 | Passbolt master password injected from Claude memory at execution time | Per `feedback_check_passbolt_first`. Never written to disk in the skill files or environment. The skill instructs the assistant to retrieve the password from memory file `passbolt-password.md` immediately before invoking the `passbolt` CLI. |
| D7 | URL enrichment is on-demand, not always | Fetch `<title>` only when the task title is < 30 chars or matches `^https?://` (bare URL). Saves WebFetch round-trips when the task title is already informative. |
| D8 | "No fitting project" → propose creating a new project inline | Mirrors FlowHub glossary "unhandled Capture → suggests new Skill", one level down. User accepts/rejects per row in the edit step. |
| D9 | Triage UX = plan-then-apply | Single proposal table, single confirm gate (`y/N/edit`). On `edit`, walk per-row to override target. Matches `git add -p` review-then-apply ergonomics. |
| D10 | Capture input = free-form text or URL, single positional arg | URL detected via `^https?://`. URL → title fetched from page; text → first 60 chars become title, full text becomes description. |
| D11 | Move-only — never delete | Per Vikunja `vikunja.md` memory: cascade deletes on projects are dangerous. Triage moves tasks; it never deletes tasks or projects. |
| D12 | Always include `title` in update payloads | Per Vikunja memory: omitting `title` on `POST /tasks/{id}` returns HTTP 412. |
| D13 | Idempotent triage | Re-running triage is safe — moved tasks leave the inbox and won't reappear. Done tasks (`done=true`) are filtered out. |
| D14 | Skill files live in `.ai/skills/<name>.md` + `.claude/commands/<name>.md` | Matches existing repo convention (`commit`, `push`, `ui-*`). |

---

## 4. Vikunja API contract (verified empirically)

All endpoints called by the CC-skills, verified against `https://todo.home.freaxnx01.ch` on 2026-04-09:

| Operation | Method + Path | Notes |
|---|---|---|
| Get current user (incl. inbox id) | `GET /api/v1/user` | `settings.default_project_id` = inbox project id |
| List projects | `GET /api/v1/projects?per_page=200` | Returns flat array; each has `id`, `title`, `parent_project_id` |
| List tasks in inbox | `GET /api/v1/projects/{inbox_id}/tasks?per_page=100` | Filter `done=false` client-side |
| Create task in inbox | `PUT /api/v1/projects/{inbox_id}/tasks` body `{"title":"…","description":"…"}` | Returns 200 with the task body |
| Move task between projects | `POST /api/v1/tasks/{task_id}` body `{"title":"…","project_id":<target>}` | **`title` is mandatory** — omitting → HTTP 412 |
| Create project | `PUT /api/v1/projects` body `{"title":"…"}` | Returns 201 |

Auth: `Authorization: Bearer <token>` on every call. Token retrieved from Passbolt at startup.

---

## 5. End-to-end flows

### 5.1 `flowhub-capture <input>`

```
1. Resolve credentials (Passbolt → token)
2. Resolve inbox project id (user.settings.default_project_id)
3. Detect input type:
   - if matches ^https?:// → URL mode
     - WebFetch the URL, extract <title> / og:title
     - title = page title (fallback: the URL itself)
     - description = the URL
   - else → text mode
     - title = first 60 chars (whitespace-trimmed, ellipsis if longer)
     - description = full text (only if input > 60 chars)
4. PUT /api/v1/projects/{inbox_id}/tasks with {title, description}
5. Print: "captured: #<id> <title>  →  Inbox"
```

### 5.2 `flowhub-triage [--limit N]`

```
1. Resolve credentials + inbox id
2. GET /api/v1/projects?per_page=200  → live project catalogue
3. GET /api/v1/projects/{inbox_id}/tasks?per_page=100 → inbox tasks
4. Filter to done=false; cap at --limit (default 25)
5. For each task:
   a. If title is bare URL or len < 30 → WebFetch enrichment
   b. Classify against the live project catalogue:
      - assistant proposes one project_id (or "create new <name>" or "skip")
      - assistant self-rates confidence: high | medium | low
6. Print proposal table:
     # | Title (50)                 | → Proposed                      | Conf   | Action
   Then ask: "Apply all? [y/N/edit]"
7a. y → execute every "move" or "create+move" sequentially, print result per row
7b. N → "no changes made", exit
7c. edit → walk per-row:
       "[2/5] Sisu: Road to Revenge → Movies (high)
        accept / change <project-name-or-id> / skip / done?"
     Then re-print summary, then ask "Apply all? [y/N]"
8. Print final summary:
     "Triaged N tasks: M moved, K created+moved, S skipped"
```

---

## 6. Component breakdown

Both skills are markdown instructions executed by the assistant. There is **no daemon, no compiled binary, no library**. Each skill file gives the assistant a deterministic sequence of bash + WebFetch + decision steps.

Files created:

```
.ai/skills/flowhub-capture.md         ← canonical capture body
.ai/skills/flowhub-triage.md          ← canonical triage body
.claude/commands/flowhub-capture.md   ← Claude Code shim
.claude/commands/flowhub-triage.md    ← Claude Code shim
docs/superpowers/specs/2026-04-09-flowhub-vikunja-skills-design.md  ← this file
```

The shims contain `$ARGUMENTS` and a thin instruction to follow the canonical body. Same pattern as `commit.md` / `push.md`.

---

## 7. Safety guarantees

| Risk | Mitigation |
|---|---|
| Accidental task deletion | Skills never call `DELETE /tasks/*`. Move-only. |
| Accidental project deletion | Skills never call `DELETE /projects/*`. Triage may *create* projects on accept; never deletes. |
| HTTP 412 from omitted `title` | Move payload always includes `title`. |
| Token leakage to disk / env | Token & Passbolt password never persisted; both retrieved fresh from Passbolt + memory at each invocation. Token never echoed to stdout. |
| Wrong-project moves | All moves gated behind explicit user `y` after review. `edit` mode lets user override per-row. |
| Misclassification of ambiguous tasks | Confidence column surfaces low-confidence rows; user can downgrade them to `skip` in edit mode. |

---

## 8. Acceptance criteria

A successful v1 implementation can:

1. **Capture (text):** `flowhub-capture "Inception (rewatch)"` creates a task in Inbox with title `Inception (rewatch)` and no description. Verified by GET on the inbox.
2. **Capture (URL):** `flowhub-capture https://example.com` creates a task whose title is fetched from `<title>` and whose description is the URL. Verified by GET.
3. **Triage happy path:** with at least one obviously-classifiable task in inbox (e.g. a clearly-titled movie), `flowhub-triage` proposes the correct existing project, and on `y` the task lands in that project (verified by GET on that project). Inbox no longer contains the task.
4. **Triage no-fit path:** with a task whose content matches no existing project, the proposal is `create new <name>` and on accept a new project is created and the task moved into it.
5. **Triage skip path:** on `N` no API state changes; on edit→skip the row is excluded from the apply.
6. **Idempotency:** re-running `flowhub-triage` immediately after a successful run reports zero open tasks remaining (or only those manually skipped).
7. **No secrets on disk:** `git grep` for the Passbolt master password and any API token returns zero hits across the worktree.

---

## 9. Out-of-scope (deferred to later sub-projects)

- Other Integrations (Wallabag, Wekan, Obsidian, Paperless-ngx)
- The QuickTask / repo-issue CC-skill
- A dispatcher that picks which CC-skill to invoke given a Capture
- Telegram / Web channel ingestion → those will pipe into `flowhub-capture` later
- Translating these CC-skills into `ISkill` C# classes inside the .NET FlowHub app
