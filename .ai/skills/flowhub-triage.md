# flowhub-triage — Slash Command

Read open tasks from the user's Vikunja Inbox, propose target projects from the live project list, and (on the user's accept) move them.

**Optional flag:** `$ARGUMENTS` may contain `--limit N` to cap how many inbox tasks are processed in one run (default 25).

> **Sibling skill:** `/flowhub-capture` writes new captures into the Inbox. This skill only reads + reorganises; it never creates new tasks (only new projects, on explicit accept).

---

## Steps

### Step 1 — Resolve credentials (Passbolt → Vikunja token)

Identical to `/flowhub-capture` step 1. Retrieve the Passbolt master password from your memory file `passbolt-password.md`, then:

```bash
PASSBOLT_PASSWORD='<from memory: passbolt-password.md>'
VIKUNJA_TOKEN=$(passbolt get resource \
  --id c9e732ce-7737-49a7-9879-dd81258083af \
  --serverAddress "https://passbolt.home.freaxnx01.ch" \
  --userPrivateKeyFile ~/.config/passbolt/private.asc \
  --userPassword "$PASSBOLT_PASSWORD" \
  --mfaMode none 2>&1 | awk -F': ' '/Password/ {print $2}')
```

If empty → stop with "Could not retrieve Vikunja API token from Passbolt."

### Step 2 — Resolve Inbox project id

```bash
INBOX_ID=$(curl -s -H "Authorization: Bearer $VIKUNJA_TOKEN" \
  https://todo.home.freaxnx01.ch/api/v1/user \
  | python3 -c 'import json,sys; print(json.load(sys.stdin)["settings"]["default_project_id"])')
```

### Step 3 — Load the live project catalogue

```bash
curl -s -H "Authorization: Bearer $VIKUNJA_TOKEN" \
  "https://todo.home.freaxnx01.ch/api/v1/projects?per_page=200" \
  > /tmp/flowhub-projects.json
```

Read it and remember every `(id, title)` pair *except* the inbox itself. This is the **only** universe of projects the classifier may propose. Never invent project ids or titles outside this list.

### Step 4 — Load open inbox tasks

```bash
curl -s -H "Authorization: Bearer $VIKUNJA_TOKEN" \
  "https://todo.home.freaxnx01.ch/api/v1/projects/$INBOX_ID/tasks?per_page=100" \
  > /tmp/flowhub-inbox.json
```

Filter to entries where `done == false`. Cap at the user-supplied `--limit` (default 25). If the filtered list is empty, print `Inbox is empty — nothing to triage.` and stop.

### Step 5 — Enrich each task (only when needed)

For each task, decide whether to enrich:

- **Enrich** (use the WebFetch tool on the URL inside the title or description) if **either**:
  - the title length is < 30 characters, **or**
  - the title matches `^https?://` (a bare URL).
- **Otherwise** skip enrichment.

Enrichment prompt for WebFetch: *"Return only: page title, plus a one-sentence summary of what this page is about. No commentary."*

Cache the enrichment result in memory for use during classification — do not refetch.

### Step 6 — Classify each task

For each task, propose **one** of:

- **`move <project_id>`** — an existing project from the catalogue (Step 3) that fits the task. The proposal text should show the project's `title` for human readability.
- **`create+move <new-project-name>`** — only if no existing project fits. Choose a clean, short German title that matches the user's existing naming style (look at the catalogue for cues — the user mixes German and English). Examples of names already present: `Bücher`, `Movies`, `Zitate/Quotes`, `IT Homelab`, `Reiseliste`.
- **`skip`** — if the task is genuinely uncategorisable, ambiguous, or appears to be transient noise.

Self-rate confidence as `high` / `medium` / `low`:

- **high** — single, obvious match (a clear movie title, a clear book ISBN URL, a clear quote)
- **medium** — multiple plausible matches, or weak topical signal
- **low** — best guess only; user should review carefully

### Step 7 — Print proposal table

Print a markdown table to stdout. Columns:

```
 # | Title (≤50 chars)                            | →  Proposed                | Conf   | Action
---+----------------------------------------------+----------------------------+--------+-----------
 1 | Sisu: Road to Revenge                        | →  Movies (#55)            | high   | move
 2 | Quote: Information is the resolution …       | →  Zitate/Quotes (#72)     | high   | move
 3 | https://example.com/random-thing             | →  (none) create "Notizen" | low    | create+move
 4 | reminder buy milk                            | →  (none)                  | low    | skip
```

Below the table, print a one-line summary like `5 tasks: 3 move, 1 create+move, 1 skip`.

### Step 8 — Ask once: apply / cancel / edit

Ask the user literally:

```
Apply all? [y / N / edit]
```

- **y** → proceed to Step 9 (apply).
- **N** or empty → print `no changes made.` and stop.
- **edit** → enter Step 8b.

#### Step 8b — Per-row edit walk

For each non-skipped row, ask:

```
[i/N] <title>
   current: → <proposal> (<conf>)
   accept / change <project-id-or-name> / skip / done?
```

- **accept** → keep the current proposal
- **change <id>** or **change <title-substring>** → look up that project in the catalogue (Step 3); if multiple match, ask the user to disambiguate by id; replace the proposal
- **skip** → set the row's action to `skip`
- **done** → break out of the loop early (remaining rows keep their current proposals)

After the walk, re-print the updated proposal table and ask `Apply all? [y / N]` once more.

### Step 9 — Apply

Process rows sequentially. **For every action, always send `title` in the payload** (Vikunja returns HTTP 412 otherwise — confirmed via the Vikunja memory file).

For **`move`**:
```bash
BODY=$(python3 -c 'import json,sys; print(json.dumps({"title": sys.argv[1], "project_id": int(sys.argv[2])}))' "$TASK_TITLE" "$TARGET_ID")
curl -s -w "\n__HTTP_%{http_code}__" -X POST \
  -H "Authorization: Bearer $VIKUNJA_TOKEN" \
  -H "Content-Type: application/json" \
  -d "$BODY" \
  "https://todo.home.freaxnx01.ch/api/v1/tasks/$TASK_ID"
```

For **`create+move`** (do these in two API calls — never in one):
```bash
# 1. Create the new project
PBODY=$(python3 -c 'import json,sys; print(json.dumps({"title": sys.argv[1]}))' "$NEW_PROJECT_TITLE")
NEW_ID=$(curl -s -X PUT \
  -H "Authorization: Bearer $VIKUNJA_TOKEN" \
  -H "Content-Type: application/json" \
  -d "$PBODY" \
  "https://todo.home.freaxnx01.ch/api/v1/projects" \
  | python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])')

# 2. Move the task into the new project (same as the move case above, with $NEW_ID as target)
```

For **`skip`**: do nothing.

After each row, print one line:

```
✓ #1 Sisu: Road to Revenge → Movies
✓ #2 Quote: Information … → Zitate/Quotes
✓ #3 https://example.com/random-thing → Notizen (created project #99)
- #4 reminder buy milk (skipped)
```

If a single API call fails (`__HTTP_<code>__` is not `200`/`201`), stop the entire apply loop, print the failing task and the response body, and report which rows did and did not apply. **Never silently swallow a failure.**

### Step 10 — Final summary

Print:

```
Triaged N tasks: M moved, K created+moved, S skipped, F failed
```

---

## Rules

- **Never** call `DELETE` on `/api/v1/tasks/*` or `/api/v1/projects/*` from this skill.
- **Never** invent a project id; only ids returned by Step 3 are legal targets.
- **Always** include `title` in `POST /tasks/{id}` and `POST /projects/{id}` payloads.
- **Never** persist the Vikunja token or Passbolt password to disk.
- If the user has not approved (`y`), no Vikunja state changes.
- If a task in the inbox has `done == true`, ignore it — don't propose, don't move.
- This skill never creates new *tasks* (only new projects, and only on explicit user accept).
