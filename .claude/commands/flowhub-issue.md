Parse a Capture that references a software project, identify the repo across all forges, and create an issue there.

Input: $ARGUMENTS

Two accepted shapes:
- `<project-reference> <issue title and optional body>` — free-form capture
- `<task-id>` (bare integer) — Vikunja task id; pulled and converted to an issue, then the task is closed with a link to the created issue. Used by `/flowhub-triage`.

Follow the canonical skill body in `.ai/skills/flowhub-issue.md` exactly.

## Quick reference

- Repo discovery: scan `~/projects/repos/` directory tree (github/, git-forgejo/, gitlab/)
- Matching: case-insensitive substring of each input word against repo directory names
- Forge detection from path: `github/` → `gh` CLI, `git-forgejo/` → Forgejo API, `gitlab/` → GitLab API
- GitHub auth: `gh` CLI (reads GH_TOKEN from direnv)
- Forgejo auth: Passbolt `a33f24d5-ced6-4921-bc47-9cae20a8d163`, header `Authorization: token <pat>`
- GitLab auth: Passbolt `dd9a77a6-4f65-4551-bcde-5ca88325378d`, header `PRIVATE-TOKEN: <pat>`
- Passbolt master password: read from memory file `passbolt-password.md`; never write to disk

## Rules

- Never create repos, only issues.
- If zero repo matches → list known repos and stop.
- If multiple matches → ask user to pick by number, never pick silently.
- If `$ARGUMENTS` is empty → print usage and stop.
- For GitHub, always prefer `gh` CLI over raw curl.
- Always escape JSON via `python3 -c 'import json,...'`.
- Never persist tokens to disk.
- Run the **enrichment interview** (skill Step 5.5) before creating an issue when the task content is thin (filename/URL-only title, empty body, meta/screenshot-only images). Ask the user for problem description, reproduction/acceptance, correct repo, severity. Accept `defer` (write context back to the Vikunja task, leave in inbox) and `cancel` (abort cleanly).
- Run **attachment anonymization** (skill Step 5.7) before uploading any image to a **public GitHub repo** (`github/*/public/<repo>`). LLM-vision detects PII bboxes (names, emails, Swiss addresses, DOBs, phone/IBAN, `*.freaxnx01.ch` hosts, tokens, faces); Pillow applies black rectangles + strips EXIF; user approves the redaction plan before upload. On low confidence → `skip-image` (default). EXIF is stripped on every upload, even for private repos. Private/self-hosted repos (Forgejo, GitLab) skip the PII pass.
- If the target repo is listed in `docs/flowhub/claude-ready-bot.md` ("Repos currently wired to the bot"), the interview uses the 4-section template (Goal / Acceptance / Pointers / Out of scope) and asks whether to add the `claude-ready` label. Good-fit defaults to `y`, bad-fit (architecture, UX, cross-cutting) defaults to `N`.
