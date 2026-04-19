# flowhub-issue — Slash Command

Parse a Capture that references a software project, identify the repo across all forges, and create an issue there.

**Input:** `$ARGUMENTS` — accepts either of two shapes:

1. **Free-form:** `<project-reference> <issue title and optional body>` — the first word (or multi-word prefix) that matches a known repo name is the project reference; everything after is the issue content.
2. **Task-id dispatch:** a bare positive integer (e.g. `2184`) — treated as a Vikunja task id. The skill fetches the task from Vikunja and builds the issue from its title, description, and any image attachments (see Step 0 below). Used by `/flowhub-triage` when it proposes an `issue` row.

> **Sibling skills:** `/flowhub-capture` (Vikunja inbox), `/flowhub-triage` (inbox triage), `/flowhub` (dispatcher).

---

## Step 0 — Task-id dispatch (skip if `$ARGUMENTS` is free-form)

If `$ARGUMENTS` is a bare integer `N`:

1. Resolve the Vikunja token (Step 1 token block — same as for Forgejo path, but fetch the Vikunja resource `c9e732ce-7737-49a7-9879-dd81258083af` instead).
2. Fetch the task:
   ```bash
   curl -s -H "Authorization: Bearer $VIKUNJA_TOKEN" \
     "https://todo.home.freaxnx01.ch/api/v1/tasks/$N"
   ```
3. If HTTP 404, print `task $N not found` and stop.
4. Build the synthetic `$ARGUMENTS` for the remaining steps:
   - Derive the project-reference from the task's title/description/attachments. Apply the same enrichment logic the triage skill uses for images (Read tool on downloaded `image/*` attachments). Scan the enriched text for the first substring that matches a known repo name (see Step 2).
   - Issue **title** = task title (trimmed, ≤120 chars; if the title is a bare filename or URL, fall back to a short summary derived from image/URL enrichment).
   - Issue **body** = task description, plus a trailing `---\nSource: Vikunja task #<identifier> (id <N>)` footer.
5. After the issue is successfully created in Step 6, mark the Vikunja task done and append the issue URL to its description:
   ```bash
   BODY=$(python3 -c 'import json,sys; print(json.dumps({"title": sys.argv[1], "done": True, "description": sys.argv[2] + "\n\n---\n**Promoted to issue:** " + sys.argv[3]}))' "$TITLE" "$OLD_DESC" "$ISSUE_URL")
   curl -s -w "\n__HTTP_%{http_code}__" -X POST \
     -H "Authorization: Bearer $VIKUNJA_TOKEN" \
     -H "Content-Type: application/json" \
     -d "$BODY" \
     "https://todo.home.freaxnx01.ch/api/v1/tasks/$N"
   ```
   Also apply the `flowhub-triaged` label (id 17) just like `/flowhub-triage` does — non-critical, warn on failure.

---

## Steps

### Step 1 — Resolve credentials

Retrieve the Passbolt master password from your memory file `passbolt-password.md`.

The skill needs forge-specific tokens depending on which forge is matched (Step 3). **Do not retrieve all tokens upfront** — only fetch the one needed after the repo is identified.

| Forge | How to authenticate |
|---|---|
| **GitHub** | Use the `gh` CLI directly — it reads `GH_TOKEN` from the environment (injected by direnv). No Passbolt call needed. Verify: `gh auth status` returns success. |
| **Forgejo** | Passbolt resource `a33f24d5-ced6-4921-bc47-9cae20a8d163` ("git-home Forgejo Access Token"). Header: `Authorization: token <pat>`. **Prerequisite:** token must have `read:issue` + `write:issue` scopes. If the API returns HTTP 403 with "token does not have at least one of required scope(s)", stop and tell the user to update the token scopes in Forgejo (user settings → Applications). |
| **GitLab** | Passbolt resource `dd9a77a6-4f65-4551-bcde-5ca88325378d` ("GitLab API freaxnx01"). Header: `PRIVATE-TOKEN: <pat>`. |

### Step 2 — Discover known repos

Scan the canonical repo directory tree:

```bash
find ~/projects/repos/ -mindepth 2 -maxdepth 5 -type d -name '.git' 2>/dev/null \
  | sed 's|/\.git$||' \
  | while read -r dir; do
      repo=$(basename "$dir")
      echo "$dir|$repo"
    done
```

Build an in-memory lookup: `repo-name → full-path`. This is the **only** universe of matchable repos.

### Step 3 — Match project reference to repo

Take the words from `$ARGUMENTS` left-to-right and try matching:

1. **Exact match** (case-insensitive): does any word equal a repo name? → use it.
2. **Substring match** (case-insensitive): does any word appear as a substring in a repo name? → use it.
3. **Multi-word prefix**: try progressively longer prefixes (first 2 words, first 3, etc.) in case the project name contains spaces or hyphens that the user wrote as separate words.

**If zero matches:** print `No repo found matching '<input>'. Known repos:` followed by the repo list grouped by forge, and stop.

**If exactly one match:** proceed. Print `matched: <repo-name> on <forge> (<owner>/<repo>)`.

**If multiple matches:** print the matches numbered and ask the user to pick:

```
Multiple repos match "game":
  1. game-esel-running (GitHub, freaxnx01)
  2. game-gorillazz (GitHub, freaxnx01)
  3. game-moki-racer (GitHub, freaxnx01)
Which one? [1-3]
```

### Step 4 — Determine forge from path

Parse the matched repo's full path to extract forge, owner, and repo name:

| Path pattern | Forge | Owner | Repo |
|---|---|---|---|
| `~/projects/repos/github/<owner>/{public,private}/<repo>` | GitHub | `<owner>` | `<repo>` |
| `~/projects/repos/git-forgejo/<repo>` | Forgejo | `freax` (default; Forgejo user is always `freax`) | `<repo>` |
| `~/projects/repos/gitlab/<owner>/<repo>` | GitLab | `<owner>` | `<repo>` |

### Step 5 — Extract issue title and body

After removing the matched project reference word(s) from `$ARGUMENTS`, the remaining text is the issue content.

- **Title:** first sentence (up to the first `.`, `!`, `?`, or end of input). Trim whitespace. Max 120 chars.
- **Body:** everything after the first sentence. If empty, omit the body.

If the remaining text after removing the project reference is empty, stop and print: `usage: /flowhub-issue <project> <issue description>`.

### Step 5.4 — Honour pre-filled enrichment (from a prior simulate run)

Before starting the interview, scan the task description for a fenced enrichment block written by a previous `/flowhub-triage --simulate` pass:

```
<!-- flowhub-triage:enrichment v1 -->
## Pre-filled for issue creation
- repo: <rel>/<repo>
- severity: <...>
- title: <...>

### Body
<...>
<!-- /flowhub-triage:enrichment -->
```

If the block is present **and** the user explicitly ran this skill (task-id dispatch from a real triage apply), parse it and feed the values into Step 5 as `title`, `body`, and `project-reference`. **Skip the interview entirely** — the user already answered.

If the block is *absent* or the user typed a `--refresh` flag to force a new interview, continue to Step 5.5 as normal.

### Step 5.45 — Check if the target is wired to the claude-ready bot

Read `docs/flowhub/claude-ready-bot.md` and look at its "Repos currently wired to the bot" list. If the target repo (`<owner>/<repo>`) is listed, set an internal flag `BOT_AVAILABLE=yes`. Otherwise set `BOT_AVAILABLE=no` and skip all claude-ready-specific logic.

When `BOT_AVAILABLE=yes`, the interview (Step 5.5), the body template (Step 5.6), and the post-create label step (Step 6.5) gain extra behavior — see those sections for details.

### Step 5.5 — Enrichment interview (when content is thin)

Creating an issue is irreversible-ish (the user will at minimum have to close a bad issue and nobody likes phantom tickets). Before calling the forge, decide whether the extracted title and body actually describe something someone could act on. Trigger an **enrichment interview** when any of these hold:

- **Title is a filename, a bare URL, or a timestamped pattern** (e.g. `IMG_20260417_122457.jpg`, `Screenshot_…_ch.freaxnx01.quicktask_vikunja.jpg`, a raw `https://…` with no surrounding prose).
- **Body is empty** *and* title is < 30 chars of meaningful content (excluding the filename / URL / timestamp noise).
- **Content is a meta capture** — an image analysis (from task-id dispatch Step 0 enrichment) shows the image is a UI screenshot of the QuickTask app, a confirmation dialog, or otherwise has no actionable content.
- **The enriched task content exists but reads as a note, not an issue** — e.g. "openclaw reduce Token usage to zero" conveys an intent but no specifics (what approach, what measurement, what acceptance criteria).

When the trigger fires, pause and interview the user with a tight, inline Q&A. Print the known context first so the user can see what you already have:

```
Task #<vikunja-id> → proposed issue on <forge>/<owner>/<repo>
  Known title: "<extracted-title>"
  Known body:  "<extracted-body-first-line-or-(empty)>"
  Image/URL context: <one-line summary from enrichment, if any>

Content looks thin. A few questions before I create the issue:

  1. What's the actual problem or ask? (1–2 sentences)
  2. Steps to reproduce / acceptance criteria (optional, skip with empty):
  3. Correct target repo? [default: <proposed-repo>]  (type a different repo name or enter to accept)
  4. Severity/type? [bug / feature / chore / question]  (default: chore)
```

When `BOT_AVAILABLE=yes` (Step 5.45), the interview instead uses the four sections the claude-ready bot requires so the resulting issue is immediately actionable. Ask all four; empty answers skip that section rather than force a placeholder:

```
  1. Goal  — one sentence: what should change and why?
  2. Acceptance  — bullet list of what "done" looks like (user-visible, not implementation):
  3. Pointers  — file paths, function names, related PRs (optional):
  4. Out of scope  — things the agent must NOT touch (optional):
  5. Correct target repo? [default: <proposed-repo>]
  6. Severity/type? [bug / feature / chore / question]  (default: chore)
  7. Mark as claude-ready for the bot to pick up? [y/N]
```

For Q7, only suggest `y` when the interview answers self-describe a good-fit issue per `docs/flowhub/claude-ready-bot.md` — i.e., a bug with clear repro, a small feature with crisp acceptance, or a module-scoped refactor. If the content reads as an architecture question, a UX call, or a cross-cutting change, default the prompt to `N` and print a one-line reason (e.g., `(not claude-ready: acceptance criteria cover >1 module)`).

Reply handling:

- **User answers:** merge into the final title and body. Q1 overrides the title if more descriptive; Q2 populates `## Acceptance`; Q3 populates `## Pointers`; Q4 populates `## Out of scope`; Q5 can redirect to a different repo from the catalogue; Q6 becomes a label on the created issue (if the forge supports it); Q7 sets the `apply_claude_ready` flag for Step 6.5.
- **User types `defer`:** do not create the issue. Instead, update the Vikunja task's description with the interview context collected so far (as a `## Notes from triage interview` block) and stop. The task stays in the inbox for a future pass.
- **User types `cancel`:** abort without touching Vikunja or the forge.

Do **not** proceed to Step 6 until the interview yields (a) a title ≥ 20 meaningful chars and (b) a non-empty body — or the user explicitly confirms that the thin version is intentional ("create anyway").

### Step 5.6 — Claude-ready body template (when `BOT_AVAILABLE=yes`)

Before calling the forge, assemble the issue body using the exact section headings the bot consumes. Omit a section when the corresponding interview answer was empty.

```markdown
## Goal
<Q1 — single sentence>

## Acceptance
- <Q2 bullet>
- <...>

## Pointers
- <Q3 bullet or plain paragraph>

## Out of scope
- <Q4 bullet>

---
Source: Vikunja task #<identifier> (id <N>)
```

For repos where `BOT_AVAILABLE=no`, fall back to the simpler title + body the earlier steps produced — no forced template.

### Step 5.7 — Attachment anonymization (public repos only)

Before any image attached to the source Vikunja task is uploaded to a forge, evaluate the target's privacy class:

| Target | Class | Action |
|---|---|---|
| `github/*/public/<repo>` | **PUBLIC** | Run anonymization (below) |
| `github/*/private/<repo>` | private | Skip anonymization; strip EXIF only |
| `git-forgejo/<repo>` (self-hosted) | private | Skip anonymization; strip EXIF only |
| `gitlab/*/*` (self-hosted gitlab.freaxnx01.ch) | private | Skip anonymization; strip EXIF only |

EXIF is always stripped — re-save the image with Pillow using `exif=b""` so device, location, and timestamp metadata never leak even to private repos.

#### 5.7a — Public repo: per-image PII review

For each image attachment on the source task:

1. **Detect PII regions** — Read the image and identify regions containing personal or sensitive content. Return a JSON list of redactions with absolute pixel bounding boxes:
   ```json
   [
     {"kind":"email","text":"freax@freaxnx01.ch","x1":114,"y1":244,"x2":410,"y2":280},
     {"kind":"name","text":"Andreas Imboden","x1":188,"y1":190,"x2":530,"y2":240},
     {"kind":"address","text":"Bodenackerstrasse 6C, 4334 Sisseln","x1":75,"y1":530,"x2":505,"y2":635},
     {"kind":"dob","text":"08.04.1978","x1":75,"y1":700,"x2":260,"y2":745},
     {"kind":"host","text":"home.freaxnx01.ch","x1":0,"y1":0,"x2":0,"y2":0}
   ]
   ```
   Minimum default categories to scan for: names (proper nouns in personal-data context), email addresses, Swiss postal addresses, dates of birth, phone numbers, IBANs (`CH\d{19}`), credit-card runs, internal hostnames (`*.home.freaxnx01.ch`, `*.freaxnx01.ch`), API tokens / long base64-ish blobs, visible faces (crude skin-tone region for photos; skip for screenshots).

2. **Present the plan** — print the list with row numbers and ask:
   ```
   Target: <forge>/<owner>/<repo>  (PUBLIC)
   Attachment: <filename>

   Proposed redactions (N):
     [1] email   "..." — bbox (x1,y1 → x2,y2)
     ...

     accept / edit N <x1,y1,x2,y2> / add <x1,y1,x2,y2> [kind] / remove N / skip-image / cancel?
   ```

3. **Redact with Pillow** — on accept, apply black rectangles at the approved bboxes, strip EXIF, and save to `/tmp/flowhub-redacted/task<TASK_ID>_<ATT_ID>.jpg`:
   ```python
   from PIL import Image, ImageDraw
   img = Image.open(src)
   d = ImageDraw.Draw(img)
   for r in redactions:
       d.rectangle([(r['x1'], r['y1']), (r['x2'], r['y2'])], fill='black')
   img.save(dst, exif=b"", quality=90)
   ```
   The original file on disk is untouched — only the redacted copy goes to the forge.

4. **Low-confidence fallback** — if the vision pass cannot reliably identify bboxes (image too small, unreadable, unusual layout), do **not** propose a guess. Instead prompt:
   ```
   Low confidence on automatic redaction for this image. Options:
     (s) skip image — create the issue without this attachment
     (c) cancel the whole issue
   ```
   Default is `s` (per user policy). Do **not** offer a full-blur option.

5. **Skip-image handling** — if the user chooses `skip-image` (or the low-confidence fallback picks `s`), drop the image from the issue and append one line to the issue body: `_Note: source task had an image attachment that was not included (redaction skipped)._`

Never upload an image to a public repo without an explicit user `accept` (or a consented `skip-image`). No silent publication.

### Step 6 — Create the issue

#### GitHub

```bash
gh issue create \
  --repo "<owner>/<repo>" \
  --title "<title>" \
  --body "<body>"
```

The `gh` CLI prints the issue URL on success.

#### Forgejo

```bash
FORGEJO_TOKEN=<from Passbolt step 1>
BODY=$(python3 -c 'import json,sys; print(json.dumps({"title": sys.argv[1], "body": sys.argv[2]}))' "$TITLE" "$ISSUE_BODY")

curl -s -w "\n__HTTP_%{http_code}__" \
  -X POST \
  -H "Authorization: token $FORGEJO_TOKEN" \
  -H "Content-Type: application/json" \
  -d "$BODY" \
  "https://git.home.freaxnx01.ch/api/v1/repos/freax/$REPO/issues"
```

Extract `html_url` from the response.

#### GitLab

```bash
GL_TOKEN=<from Passbolt step 1>

# First, get the project id by path
PROJECT_ID=$(curl -s -H "PRIVATE-TOKEN: $GL_TOKEN" \
  "https://gitlab.freaxnx01.ch/api/v4/projects?search=$REPO&owned=true" \
  | python3 -c 'import json,sys; ps=json.load(sys.stdin); print(next((p["id"] for p in ps if p["path"]==sys.argv[1]),"-1"))' "$REPO")

BODY=$(python3 -c 'import json,sys; print(json.dumps({"title": sys.argv[1], "description": sys.argv[2]}))' "$TITLE" "$ISSUE_BODY")

curl -s -w "\n__HTTP_%{http_code}__" \
  -X POST \
  -H "PRIVATE-TOKEN: $GL_TOKEN" \
  -H "Content-Type: application/json" \
  -d "$BODY" \
  "https://gitlab.freaxnx01.ch/api/v4/projects/$PROJECT_ID/issues"
```

Extract `web_url` from the response.

### Step 6.5 — Apply the `claude-ready` label (when `BOT_AVAILABLE=yes` and user said `y` to Q7)

Only for GitHub targets; the scheduled agent is wired to `freaxnx01/quicktask-vikunja` (see `docs/flowhub/claude-ready-bot.md`).

```bash
gh issue edit <issue-number> --repo "<owner>/<repo>" --add-label claude-ready
```

Verify the issue ended up:
- unassigned (the agent skips assigned issues),
- without a linked open PR (expected for a brand-new issue),
- with exactly one of `claude-ready` / `claude-working` / `blocked` (we want `claude-ready`).

If the label doesn't exist on the repo, create it first:
```bash
gh label create claude-ready --repo "<owner>/<repo>" --color "a0ffa0" --description "Ready for the scheduled Claude agent"
```

Warn (don't fail) if the label application returns non-zero — the issue itself was already created successfully; the user can add the label from the UI.

### Step 7 — Confirm to the user

Print:

```
created: <issue-url>
  repo: <owner>/<repo> (<forge>)
  title: <title>
  labels: <comma-separated list, incl. claude-ready if Step 6.5 ran>
  agent: claude-ready bot will pick this up within ~2h  (only when BOT_AVAILABLE=yes and label applied)
```

---

## Rules

- **Never** create repos, only issues.
- **Never** persist any token to disk — same Passbolt-from-memory pattern as all FlowHub skills.
- **Never** silently pick a repo when multiple match — always disambiguate.
- If `$ARGUMENTS` is empty, print `usage: /flowhub-issue <project> <issue description>` and stop.
- If the forge API returns an error (non-2xx), print the error body and stop.
- For GitHub, prefer the `gh` CLI over raw `curl` — it handles auth, pagination, and rate limits.
- Always escape JSON via `python3 -c 'import json,...'` — never string interpolation.
