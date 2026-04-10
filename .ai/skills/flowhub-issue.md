# flowhub-issue — Slash Command

Parse a Capture that references a software project, identify the repo across all forges, and create an issue there.

**Input:** $ARGUMENTS

Format: `<project-reference> <issue title and optional body>`

The first word (or multi-word prefix) that matches a known repo name is the project reference. Everything after it is the issue content.

> **Sibling skills:** `/flowhub-capture` (Vikunja inbox), `/flowhub-triage` (inbox triage), `/flowhub` (dispatcher).

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

### Step 7 — Confirm to the user

Print:

```
created: <issue-url>
  repo: <owner>/<repo> (<forge>)
  title: <title>
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
