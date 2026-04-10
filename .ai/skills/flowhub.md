# flowhub — Dispatcher Slash Command

Single entry point for all FlowHub CC-skills. Accepts any Capture, classifies it, and routes to the appropriate sub-skill.

**Input:** $ARGUMENTS

---

## Routing logic

Follow this classification in order. The **first** match wins.

### 1. Explicit sub-commands

| Prefix (case-insensitive) | Routes to | Passed args |
|---|---|---|
| `triage ...` | `/flowhub-triage` | everything after "triage" |
| `issue ...` | `/flowhub-issue` | everything after "issue" |
| `capture ...` | `/flowhub-capture` | everything after "capture" |

If `$ARGUMENTS` starts with one of these words, strip it and follow the named skill's canonical body in `.ai/skills/flowhub-<name>.md`.

### 2. Repo name detection → `/flowhub-issue`

Scan `~/projects/repos/` for repo directory names (same discovery as `/flowhub-issue` Step 2).

For each word in `$ARGUMENTS` (left to right), check if it matches any repo name (case-insensitive substring). If a match is found:

- Print: `detected: repo issue (matched "<word>" → <repo-name>)`
- Print: `routing to /flowhub-issue`
- Follow `.ai/skills/flowhub-issue.md` with the full `$ARGUMENTS` (the issue skill handles its own project-reference extraction).

### 3. Default → `/flowhub-capture`

If no explicit sub-command and no repo name match:

- Print: `detected: generic capture (no repo match)`
- Print: `routing to /flowhub-capture`
- Follow `.ai/skills/flowhub-capture.md` with the full `$ARGUMENTS`.

---

## Examples

| Input | Classification | Routed to |
|---|---|---|
| `triage --limit 5` | explicit `triage` | `/flowhub-triage --limit 5` |
| `issue flowhub add health check` | explicit `issue` | `/flowhub-issue flowhub add health check` |
| `Quicktask show last 15 tasks` | repo match ("Quicktask" → quicktask-vikunja) | `/flowhub-issue Quicktask show last 15 tasks` |
| `https://exlibris.ch/.../harari` | no match | `/flowhub-capture https://exlibris.ch/.../harari` |
| `Inception (rewatch)` | no match | `/flowhub-capture Inception (rewatch)` |
| `capture Buy milk` | explicit `capture` | `/flowhub-capture Buy milk` |

---

## Adding a new sub-skill

To extend the dispatcher with a future FlowHub sub-skill (e.g. `flowhub-article` for Wallabag):

1. Create `.ai/skills/flowhub-article.md` + `.claude/commands/flowhub-article.md`
2. Add an explicit sub-command row to the table in section 1 above: `article ...` → `/flowhub-article`
3. Optionally add pattern-based routing in a new section between 2 and 3 (e.g. "if input is a URL matching `heise.de|arstechnica.com|...` → route to `/flowhub-article`")

---

## Rules

- **Transparent routing:** always print which sub-skill is being routed to before following its steps.
- **No API calls for classification.** The dispatcher only reads the local directory tree (cached once) and matches strings. All network calls happen inside the routed sub-skill.
- If `$ARGUMENTS` is empty, print a help message listing available sub-commands and stop:

```
usage: /flowhub <input>

Sub-commands:
  triage [--limit N]         — triage Vikunja inbox
  issue <project> <text>     — create issue on a repo's forge
  capture <url-or-text>      — capture to Vikunja inbox

Or just provide free-form input:
  /flowhub Quicktask fix search  → detected as repo issue, creates GitHub issue
  /flowhub https://example.com   → captured to Vikunja inbox
```

- The dispatcher itself never creates issues, tasks, or projects — it only classifies and delegates.
- After routing, follow the target skill's canonical body exactly (do not skip steps or abbreviate).
