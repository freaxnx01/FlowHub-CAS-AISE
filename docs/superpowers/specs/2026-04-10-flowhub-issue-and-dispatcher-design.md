# FlowHub Issue + Dispatcher CC-Skills — Design

**Status:** Approved (auto-applied, user delegated all decisions)
**Scope:** Sub-projects #2 and #3 of the FlowHub CC-Skill family.
**Prerequisite:** Sub-project #1 (flowhub-capture + flowhub-triage) is implemented on this branch.

---

## Sub-project #2: `flowhub-issue`

### Purpose

Parse a Capture that references a software project, identify the corresponding repo across all forges, and create an issue on that forge.

### Example

```
/flowhub-issue Quicktask show last 15 tasks after adding a task
→ matched: quicktask-vikunja on GitHub (freaxnx01/quicktask-vikunja)
→ created: https://github.com/freaxnx01/quicktask-vikunja/issues/4
```

### Design decisions

| # | Decision | Rationale |
|---|---|---|
| I1 | Input format: `<project-ref> <issue-title-and-body>` | The first recognisable project/repo name in the input is the target; everything after it is the issue content. Simple, no flags needed. |
| I2 | Repo discovery: local directory scan of `~/projects/repos/` | The canonical layout (per Repos Directory Layout.md) already groups repos by forge/owner/visibility. Fast, no network call. |
| I3 | Fuzzy matching: case-insensitive substring on directory names | "Quicktask" matches `quicktask-vikunja`; "flowhub" matches `FlowHub-CAS-AISE`. If multiple match, present choices and ask the user to disambiguate. |
| I4 | Forge detection from path | `github/` → GitHub, `git-forgejo/` → Forgejo, `gitlab/` → GitLab. Determined by the directory structure, not by config files. |
| I5 | GitHub: `gh` CLI | Already authenticated via direnv + GH_TOKEN. Simplest: `gh issue create --repo <owner/repo> --title ... --body ...`. |
| I6 | Forgejo: `curl` + Passbolt token | API at `https://git.home.freaxnx01.ch/api/v1/repos/{owner}/{repo}/issues`. Token: Passbolt resource `a33f24d5-ced6-4921-bc47-9cae20a8d163`. **Prerequisite:** token must have `read:issue` + `write:issue` scopes (current token lacks these — documented as setup step). |
| I7 | GitLab: `curl` + Passbolt token | API at `https://gitlab.freaxnx01.ch/api/v4/projects/{id}/issues`. Token: Passbolt resource `dd9a77a6-4f65-4551-bcde-5ca88325378d` (scope `api`). Project id looked up by path via `GET /api/v4/projects?search=<name>`. |
| I8 | Issue content split: first sentence = title, rest = body | If input is one sentence → title only, body empty. If multi-sentence → first sentence is title, remainder is body. Matches natural language patterns. |
| I9 | Never create repos | Skill only creates issues. If the matched repo doesn't exist or has issues disabled, report the error and stop. |

### End-to-end flow

```
1. Resolve credentials (Passbolt → tokens for each forge, as needed)
2. Scan ~/projects/repos/ recursively (depth 4) for directory names
3. Extract project reference from $ARGUMENTS:
   - Try each word/prefix as a substring against known repo names
   - Case-insensitive, longest match wins
   - If zero matches → "No repo found matching '<ref>'. Available: ..."
   - If multiple matches → "Multiple repos match '<ref>': ... Which one?"
4. Determine forge from matched path:
   - ~/projects/repos/github/<owner>/<vis>/<repo> → GitHub
   - ~/projects/repos/git-forgejo/<repo> → Forgejo
   - ~/projects/repos/gitlab/<owner>/<repo> → GitLab
5. Extract owner + repo name from path
6. Split remaining input into title + body
7. Create issue on the correct forge:
   - GitHub: gh issue create --repo <owner>/<repo> --title "..." --body "..."
   - Forgejo: POST /api/v1/repos/<owner>/<repo>/issues {"title":"...","body":"..."}
   - GitLab: POST /api/v4/projects/<id>/issues {"title":"...","description":"..."}
8. Print: "created: <issue-url>"
```

### Forge credential mapping

| Forge | Passbolt resource UUID | Auth header |
|---|---|---|
| GitHub | `7b243620-...` (public) / `51026ce1-...` (private) | via `gh` CLI (reads GH_TOKEN from env) |
| Forgejo | `a33f24d5-ced6-4921-bc47-9cae20a8d163` | `Authorization: token <pat>` |
| GitLab | `dd9a77a6-4f65-4551-bcde-5ca88325378d` | `PRIVATE-TOKEN: <pat>` |

### Setup prerequisites

1. **Forgejo token scope:** The current Passbolt resource "git-home Forgejo Access Token" lacks `read:issue`/`write:issue`. User must update the token scopes in Forgejo UI (user settings → Applications) or create a new token and update the Passbolt resource. Until this is done, the skill will report "Forgejo token lacks issue scope" and skip.
2. **`gh` CLI authenticated:** `gh auth status` must succeed. Already the case per the user's direnv setup.

---

## Sub-project #3: `flowhub` (dispatcher)

### Purpose

Single entry point for all FlowHub CC-skills. Accepts any Capture, classifies it, and routes to the appropriate sub-skill.

### Example

```
/flowhub Quicktask show last 15 tasks after adding a task
→ detected: repo issue (matched "Quicktask" → quicktask-vikunja)
→ routing to /flowhub-issue

/flowhub https://www.exlibris.ch/de/buecher-buch/.../id/9780066620732/
→ detected: generic capture (no repo match, URL)
→ routing to /flowhub-capture

/flowhub triage
→ detected: triage command
→ routing to /flowhub-triage
```

### Design decisions

| # | Decision | Rationale |
|---|---|---|
| D1 | Classification order: explicit command → repo match → generic capture | Explicit commands (`triage`, `issue`) short-circuit. Repo matching uses the same directory scan as flowhub-issue. Everything else defaults to flowhub-capture (Vikunja inbox). |
| D2 | No new API calls for classification | The dispatcher only scans local directory names (cached in memory once per invocation). No network round-trip for routing — that stays fast. |
| D3 | Transparent routing | The dispatcher prints which sub-skill it's routing to, then follows that skill's steps directly (not by spawning a separate invocation). Same session, same context. |
| D4 | Extensible via keyword table | A simple table in the skill file maps keywords/patterns to sub-skills. Adding a new FlowHub sub-skill means adding one row. |

### Classification logic

```
1. If $ARGUMENTS starts with "triage" (case-insensitive):
   → strip "triage" prefix, route to flowhub-triage with remaining args

2. If $ARGUMENTS starts with "issue" (case-insensitive):
   → strip "issue" prefix, route to flowhub-issue with remaining args

3. Scan ~/projects/repos/ for repo directory names
   For each word in $ARGUMENTS (left to right):
     if word matches a repo name (case-insensitive substring):
       → route to flowhub-issue with full $ARGUMENTS

4. Default:
   → route to flowhub-capture with full $ARGUMENTS
```

### Routing table (extensible)

| Pattern | Routes to | Example input |
|---|---|---|
| `triage [args]` | `/flowhub-triage` | `triage --limit 5` |
| `issue <repo> <text>` | `/flowhub-issue` | `issue flowhub add health check` |
| `<repo-name-match> <text>` | `/flowhub-issue` | `Quicktask show last 15 tasks` |
| *(everything else)* | `/flowhub-capture` | `https://exlibris.ch/...` or `Inception (rewatch)` |

---

## Files created

```
.ai/skills/flowhub-issue.md           ← canonical issue body
.ai/skills/flowhub.md                 ← canonical dispatcher body
.claude/commands/flowhub-issue.md     ← Claude Code shim
.claude/commands/flowhub.md           ← Claude Code shim
docs/superpowers/specs/2026-04-10-flowhub-issue-and-dispatcher-design.md  ← this file
```

---

## Safety guarantees

| Risk | Mitigation |
|---|---|
| Issue on wrong repo | Skill prints the matched repo and forge before creating; user sees it in the output. |
| Multiple repo matches | Skill asks user to disambiguate by number, never picks silently. |
| Missing forge token scope | Skill checks API response; if 403, reports scope issue and stops. |
| Credential leakage | Same as sub-project #1: Passbolt password from memory, tokens from CLI, never on disk. |
| Dispatcher misroutes | Transparent: prints "routing to /flowhub-issue" or "routing to /flowhub-capture" so the user sees and can correct. |

---

## Acceptance criteria

1. `/flowhub-issue Quicktask show last 15 tasks after adding a task` creates an issue on `freaxnx01/quicktask-vikunja` (GitHub) with title "show last 15 tasks after adding a task" and returns the issue URL.
2. `/flowhub Quicktask add search bar` routes to flowhub-issue automatically (repo name detected).
3. `/flowhub https://exlibris.ch/...` routes to flowhub-capture automatically (no repo match, URL).
4. `/flowhub triage` routes to flowhub-triage.
5. No secrets on disk (`git grep` for Passbolt password returns 0 hits).
6. Ambiguous repo match (e.g., "game" matches 3 repos) presents a disambiguation prompt.
