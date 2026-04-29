# flowhub-triage — Slash Command

Read open tasks from the user's Vikunja Inbox, propose target projects from the live project list, and (on the user's accept) move them.

**Flags in `$ARGUMENTS`:**

- `--limit N` — cap how many inbox tasks are processed in one run (**default 10**).
- `--simulate` — do not move tasks. On accept, attach two labels per task instead: `triage-target:<project-or-action>` and `triage-conf:<high|medium|low>`. Inbox fetches in this mode automatically exclude any task that already carries a `triage-target:*` label (idempotent — safe to re-run).
- `--only-issues` — after classification, hide any row whose action ≠ `issue`. Use this to focus the proposal table on issue candidates (typically QuickTask-origin dev todos) and avoid mixing move/create+move rows into the same batch.

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

### Step 4 — Ingest Captures

Captures arrive through multiple **Channels** (glossary: an inbound source). The triage skill unifies them into a single list before classification. Two Channels are wired:

- **4a** — Telegram bot `@flowhub_intelliflow_bot` (drain each run, push to Vikunja Inbox).
- **4b** — Vikunja Inbox (what QuickTask, Signal-bridge, and the Telegram drain all write to).

Downstream steps (5+) don't distinguish origins — every Capture ends up as a Vikunja Inbox task by the end of Step 4.

Flags:
- `--no-telegram` — skip 4a (use when Telegram is down or you're triaging offline).

#### 4a — Drain the Telegram bot

Fetch the bot token from Passbolt (resource id `fd9897e7-544c-4109-8ee9-cc8eb1838ee5`, "Telegram flowhub_bot HTTP API Token"). Reuse the same Passbolt pattern as the Vikunja token in Step 1; never write the token to disk.

```bash
TG_TOKEN=$(passbolt get resource \
  --id fd9897e7-544c-4109-8ee9-cc8eb1838ee5 \
  --serverAddress "https://passbolt.home.freaxnx01.ch" \
  --userPrivateKeyFile ~/.config/passbolt/private.asc \
  --userPassword "$PASSBOLT_PASSWORD" \
  --mfaMode none 2>&1 | awk -F': ' '/Password/ {print $2}')
```

**Offset state (safe to persist):** the `update_id` of the most recent successfully drained update is stored at `~/.cache/flowhub/telegram-offset`. This is *not* a secret — it's the cursor Telegram uses to know which updates you've already acknowledged. Read it; default to `0` on first run.

```bash
mkdir -p ~/.cache/flowhub
OFFSET=$(cat ~/.cache/flowhub/telegram-offset 2>/dev/null || echo 0)
curl -s "https://api.telegram.org/bot${TG_TOKEN}/getUpdates?offset=$((OFFSET+1))&timeout=0" \
  > /tmp/flowhub-tg-updates.json
```

For each update in `result[]` (in order — Telegram returns them chronologically):

1. **Extract Capture content** from `message` (or `edited_message`):
   - **Text**: `message.text`
   - **URL-carrying**: a `message.text` matching `^https?://` (prefer URL mode in Vikunja-Capture).
   - **Photo**: pick `message.photo[-1]` (largest size), record `file_id`.
   - **Document**: `message.document.file_id` + `message.document.file_name`.
   - **Caption**: `message.caption` — use as Capture description when media is present.
   - Ignore messages without any of these (stickers, location, service messages) — log `skipped (non-capture message type)`.

2. **Write to Vikunja Inbox** — same shape as QuickTask/Signal captures today:
   ```bash
   BODY=$(python3 -c 'import json,sys; print(json.dumps({"title": sys.argv[1], "description": sys.argv[2], "project_id": int(sys.argv[3])}))' "$TITLE" "$DESCRIPTION" "$INBOX_ID")
   TASK_ID=$(curl -s -X PUT \
     -H "Authorization: Bearer $VIKUNJA_TOKEN" \
     -H "Content-Type: application/json" \
     -d "$BODY" \
     "https://todo.home.freaxnx01.ch/api/v1/projects/$INBOX_ID/tasks" \
     | python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])')
   ```

3. **Download + attach media** (for photo/document updates) — mirror how QuickTask ends up with Vikunja attachments:
   ```bash
   # 3a. Resolve file_path from file_id
   FILE_PATH=$(curl -s "https://api.telegram.org/bot${TG_TOKEN}/getFile?file_id=${FILE_ID}" \
     | python3 -c 'import json,sys; print(json.load(sys.stdin)["result"]["file_path"])')

   # 3b. Download the file to a local temp path
   LOCAL=$(mktemp --suffix=".${FILE_PATH##*.}")
   curl -s "https://api.telegram.org/file/bot${TG_TOKEN}/${FILE_PATH}" -o "$LOCAL"

   # 3c. Upload as a Vikunja attachment (multipart form)
   curl -s -w "\n__HTTP_%{http_code}__" -X PUT \
     -H "Authorization: Bearer $VIKUNJA_TOKEN" \
     -F "files=@${LOCAL}" \
     "https://todo.home.freaxnx01.ch/api/v1/tasks/${TASK_ID}/attachments"
   ```
   Delete `$LOCAL` after upload.

4. **Source label** — tag the new Vikunja task with `channel:telegram` (get-or-create it using the same helper as `--simulate` — palette `0088cc`, Telegram brand blue). This makes the Channel visible in the Vikunja UI and on the proposal table, and lets downstream classification apply origin bias symmetric to the QuickTask heuristic.

The full `channel:*` label family (reserved prefix — add rows as new Channels come online):

| Label | Color | Set by |
|---|---|---|
| `channel:telegram` | `#0088cc` | Step 4a in this skill |
| `channel:quicktask` | `#7b68ee` | to be added to `/flowhub-capture` when the Android app calls through it (today QuickTask writes Vikunja directly, so untagged — the classifier detects it by filename heuristic instead) |
| `channel:signal` | `#3a76f0` | to be added to `/flowhub-capture` when invoked by the Signal-to-claude bridge |
| `channel:manual` | `#9ca3af` | `/flowhub-capture` when run interactively by the user with no other origin hint |

Only `channel:telegram` is applied today — the others are reserved names so the taxonomy stays coherent when the other Channels are retrofitted. The classifier in Step 6 should treat `channel:telegram` as a weak issue-candidate bias for now (same class as QuickTask), because Telegram messages from you are usually either URLs to file or short dev notes.

5. **Advance the offset** — *only* after the Vikunja write + all attachment uploads for that update succeed. Write the update's `update_id` to `~/.cache/flowhub/telegram-offset`. If any step fails, stop draining (do not advance the offset) and surface the error — the next run will retry the same update.

6. **Soft-fail on Telegram unreachable** — if the `getUpdates` call itself errors (network, HTTP 5xx, invalid token), print one warning line (`warn: Telegram getUpdates failed (<reason>) — continuing without Telegram drain`) and fall through to 4b. Never abort the whole triage run because Telegram is down.

When done, print one summary line: `Telegram: drained N Captures (M text, K with media), offset now X` (or `Telegram: no new captures`).

#### 4b — Load open Captures from the Vikunja Inbox

The Vikunja inbox can easily exceed one page. **Always paginate** — `per_page` is capped server-side and a single request will silently drop later Captures.

```bash
page=1
> /tmp/flowhub-inbox.jsonl
while :; do
  resp=$(curl -s -H "Authorization: Bearer $VIKUNJA_TOKEN" \
    "https://todo.home.freaxnx01.ch/api/v1/projects/$INBOX_ID/tasks?per_page=200&page=$page")
  n=$(echo "$resp" | python3 -c 'import json,sys; print(len(json.load(sys.stdin)))')
  [ "$n" = "0" ] && break
  echo "$resp" | python3 -c 'import json,sys
for t in json.load(sys.stdin): print(json.dumps(t))' >> /tmp/flowhub-inbox.jsonl
  page=$((page+1))
  [ "$page" -gt 20 ] && break   # safety cap: 4000 tasks
done
```

Then filter to entries where `done == false`. Cap at the user-supplied `--limit` (default 10).

When `--simulate` is active, also drop any task that already carries a label whose title starts with `triage-target:`. This keeps the simulation loop idempotent — once you've labeled a task, subsequent runs ignore it until you either remove the labels or perform a real move.

If the filtered list is empty, print `Inbox is empty — nothing to triage.` and stop.

**Issue-candidate bias from origin:** the user's `quicktask-vikunja` Android app captures dev-oriented todos — tasks from that app are strong candidates for `issue` rather than `move`. Detect the origin by any of these signals:

- title matches `^Screenshot_.*_ch\.freaxnx01\.quicktask_vikunja\.(jpg|png)$`
- any attachment filename contains `ch.freaxnx01.quicktask_vikunja`
- description is empty **and** title is a bare URL or ends with a bare URL (Android share-sheet capture)

When an origin signal fires, bump the classifier's preference toward `issue <rel>/<repo>` for that row (see Step 6). Never silently drop these — they are real todos.

### Step 5 — Enrich each task (only when needed)

Enrichment feeds the classifier. Run whichever of the two branches below fits; skip both when the task's own text is already descriptive.

#### 5a — URL enrichment (WebFetch)

Use WebFetch on a URL from the title or description when **either**:
- the title length is < 30 characters, **or**
- the title matches `^https?://` (a bare URL).

Enrichment prompt: *"Return only: page title, plus a one-sentence summary of what this page is about. No commentary."*

#### 5b — Image enrichment (Read tool)

Trigger when the task has at least one attachment with a `file.mime` starting `image/`, **and** the title gives little signal (e.g. `Image`, empty, or < 30 chars). This catches Signal-captured screenshots and photos.

Download each such attachment and Read it (the Read tool accepts JPEG/PNG and returns the visual content to the model):

```bash
# For each (task_id, attachment_id) pair:
curl -s -H "Authorization: Bearer $VIKUNJA_TOKEN" \
  "https://todo.home.freaxnx01.ch/api/v1/tasks/$TASK_ID/attachments/$ATTACHMENT_ID" \
  -o "/tmp/flowhub-images/task${TASK_ID}_file${ATTACHMENT_ID}.${EXT}"
```

Then call the **Read** tool on the downloaded path. Use what you see to extract: a short one-line description of the image's subject, any visible text/URLs, and whether it's a screenshot, a photo, or a scanned document. That becomes the synthetic "enriched title" for classification.

Hard limits — skip image enrichment when any of these hold:
- the file is > 10 MB (skip with reason `image too large`);
- the mime isn't `image/jpeg`, `image/png`, `image/webp`, or `image/gif`;
- the task already has a descriptive title (≥ 30 chars and not literally `Image`).

Cache all enrichment results in memory for use during classification — never refetch, never re-download.

### Step 6 — Classify each task

Load the repo catalogue (local + remote, all forges) for issue candidates — this is the only universe of legal issue targets:

```bash
source /home/freax/projects/repos/github/freaxnx01/public/config/shell/clrepo.sh
_clrepo_remote_list 0 > /tmp/flowhub-repos.list
```

Each line is `<rel>/<repo>` where `<rel>` is `github/<owner>/{public,private}` | `gitlab/<owner>` | `git-forgejo`.

For each task, propose **one** of:

- **`issue <rel>/<repo>`** — the task reads like a bug report, feature request, or dev TODO for a specific repo. Prefer this action when **either** of these holds:
  - the origin signal fires (see Step 4 — QuickTask-origin bias), **or**
  - the enriched content mentions a repo name from the catalogue as a substring, **or**
  - the content has clear dev signals (error messages, stack traces, "fix", "bug", "TODO", "implement", a github.com/gitlab.com URL pointing at one of the user's repos).

  On apply, this dispatches to `/flowhub-issue <task-id>` which reuses the forge-specific issue-creation logic.

- **`move <project_id>`** — an existing project from the catalogue (Step 3) that fits the task. The proposal text should show the project's `title` for human readability.
- **`create+move <new-project-name>`** — only if no existing project fits. Choose a clean, short German title that matches the user's existing naming style (look at the catalogue for cues — the user mixes German and English). Examples of names already present: `Bücher`, `Movies`, `Zitate/Quotes`, `IT Homelab`, `Reiseliste`.
- **`skip`** — if the task is genuinely uncategorisable, ambiguous, or appears to be transient noise.

Prefer `issue` only when confidence is medium or higher; on low confidence, fall back to `move` / `skip` and let the edit walk escalate if the user insists.

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

#### 9-sim — `--simulate` branch (label-only)

When `--simulate` is active, **do not touch `project_id`** for any row. Instead, for each row (including `skip`), attach two labels to the task:

- `triage-target:<project-or-action>` — project title for `move`, `"create:<new-project-name>"` for `create+move`, `"(skip)"` for `skip`. (No `flowhub-issue` case here — issue detection is handled elsewhere in the skill and treated as `move` from the label's perspective.)
- `triage-conf:<high|medium|low>`

Label resolution is get-or-create — Vikunja rejects attaching a label id that doesn't exist yet:

```bash
# Try to find existing label by title; create if absent. Returns the id on stdout.
get_or_create_label() {
  local title="$1"
  local id
  id=$(curl -s -H "Authorization: Bearer $VIKUNJA_TOKEN" \
         "https://todo.home.freaxnx01.ch/api/v1/labels?s=$(python3 -c 'import urllib.parse,sys; print(urllib.parse.quote(sys.argv[1]))' "$title")" \
       | python3 -c 'import json,sys; ls=json.load(sys.stdin); print(next((l["id"] for l in ls if l["title"]==sys.argv[1]), ""))' "$title")
  if [ -z "$id" ]; then
    local body
    body=$(python3 -c 'import json,sys; print(json.dumps({"title": sys.argv[1], "hex_color": sys.argv[2]}))' "$title" "$2")
    id=$(curl -s -X PUT \
           -H "Authorization: Bearer $VIKUNJA_TOKEN" \
           -H "Content-Type: application/json" \
           -d "$body" \
           "https://todo.home.freaxnx01.ch/api/v1/labels" \
         | python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])')
  fi
  echo "$id"
}
```

Suggested palette (second arg to `get_or_create_label`):

| Label prefix | Color |
|---|---|
| `triage-target:` | `a0a0ff` (soft blue) |
| `triage-target:(skip)` | `808080` (grey) |
| `triage-conf:high` | `22c55e` (green) |
| `triage-conf:medium` | `eab308` (amber) |
| `triage-conf:low` | `ef4444` (red) |

Attach both labels to the task:

```bash
for lid in "$TARGET_LABEL_ID" "$CONF_LABEL_ID"; do
  curl -s -w "\n__HTTP_%{http_code}__" -X PUT \
    -H "Authorization: Bearer $VIKUNJA_TOKEN" \
    -H "Content-Type: application/json" \
    -d "{\"label_id\":$lid}" \
    "https://todo.home.freaxnx01.ch/api/v1/tasks/$TASK_ID/labels"
done
```

In simulate mode **do not** attach the `flowhub-triaged` label — that label signals a real move has happened, and tasks carrying it would be hidden from a later non-simulate run.

Per-row output line:

```
~ #3 Sisu: Road to Revenge ↪ label triage-target:Movies + triage-conf:high
```

Final summary line uses `labeled` and `skipped` instead of `moved`:

```
Simulated N tasks: M labeled, S skipped, F failed
```

**Simulate + issue — run the interview, skip the forge call.** The enrichment interview (`/flowhub-issue` Step 5.5) is content-gathering, not a forge side effect, so it **must** run in simulate mode too — otherwise the label captures intent the user never validated, and the next real run re-asks everything from scratch.

Flow for an `issue` row under `--simulate`:

1. Trigger `/flowhub-issue` Step 5.5 interview against the task. Ask the same questions (problem, reproduction, repo, severity) when the content is thin.
2. **Persist the answers into the Vikunja task's description** under a clearly fenced block so the next real apply can re-use them without re-interviewing:
   ```
   <!-- flowhub-triage:enrichment v1 -->
   ## Pre-filled for issue creation
   - repo: <rel>/<repo>
   - severity: <bug|feature|chore|question>
   - title: <refined title>

   ### Body
   <refined body>
   <!-- /flowhub-triage:enrichment -->
   ```
   The next `/flowhub-issue <task-id>` run detects this block and skips the interview, going straight to Step 6 (create).
3. Label the task based on the interview **outcome**, not the initial proposal:
   | Outcome | `triage-target:` label suffix | Notes |
   |---|---|---|
   | accept | `issue:<rel>/<repo>` | fully enriched, ready for real apply |
   | accept with `skip-image` | `issue:<rel>/<repo>:no-image` | image(s) dropped from the plan |
   | defer | `(defer)` | context block still written to description; task stays in inbox |
   | cancel | (no label, row counted as `cancelled`) | nothing written |
4. `triage-conf` reflects the *post-interview* confidence, which is usually higher than the pre-interview estimate (the user just clarified the content). Bump the conf label accordingly.

Still no forge calls and no `flowhub-triaged` label in simulate mode — the forge activity waits for the real apply run.

#### 9-real — normal branch (the rest of this step)

If `--simulate` is **not** active, continue with the move/create+move/skip logic below.

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

For **`issue`**: dispatch to the `/flowhub-issue` skill with the task id:

```
/flowhub-issue <task-id>
```

That skill (see `.ai/skills/flowhub-issue.md`) pulls the task's title/description/attachments from Vikunja, extracts issue title + body, creates the issue on the matched forge, and (on success) marks the Vikunja task done with the issue URL appended to its description. Do not duplicate that logic here; this skill only chooses the repo and hands off the id.

**Interview gate:** `/flowhub-issue` runs an inline enrichment interview (its Step 5.5) whenever the task content is too thin to make a sensible issue — filename-only titles, QuickTask UI screenshots, notes like "openclaw reduce Token usage to zero", and so on. If the user types `defer` during that interview, `/flowhub-issue` writes the collected context back to the Vikunja task description and leaves the task in the inbox for a future pass. Factor this into the triage apply summary: a deferred row counts as `deferred`, not `failed` (it is intentional).

For **`skip`**: do nothing.

After each successful **`move`** or **`create+move`**, apply the `flowhub-triaged` label to the task:

```bash
curl -s -w "\n__HTTP_%{http_code}__" -X PUT \
  -H "Authorization: Bearer $VIKUNJA_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"label_id":17}' \
  "https://todo.home.freaxnx01.ch/api/v1/tasks/$TASK_ID/labels"
```

The label `flowhub-triaged` (id 17, color `#7b68ee`) already exists in Vikunja. If the label assignment fails, log a warning but do **not** stop the apply loop — the move already succeeded and the label is non-critical tracking metadata.

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
