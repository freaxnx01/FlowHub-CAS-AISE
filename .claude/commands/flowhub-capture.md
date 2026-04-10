Capture a free-form input (URL or text) into the user's Vikunja Inbox.

Input: $ARGUMENTS

Follow the canonical skill body in `.ai/skills/flowhub-capture.md` exactly. It contains the credential lookup, inbox detection, input parsing, payload construction, and error handling steps.

## Quick reference

- Vikunja: `https://todo.home.freaxnx01.ch`
- Inbox detection: `GET /api/v1/user.settings.default_project_id`
- Create endpoint: `PUT /api/v1/projects/{inbox_id}/tasks` with `{"title":..., "description":...}`
- Token source: Passbolt resource id `c9e732ce-7737-49a7-9879-dd81258083af`
- Passbolt master password: read from your memory file `passbolt-password.md` at runtime; never write to disk

## Rules

- If `$ARGUMENTS` is empty: print `usage: /flowhub-capture <url-or-text>` and stop.
- URL detection: `^https?://`. URL → enrich title via WebFetch (`<title>` / `og:title`); fall back to the URL if WebFetch fails.
- Text mode: title = first 60 chars (truncate with `…`); description = full text only if > 60 chars.
- Always escape JSON via `python3 -c 'import json,...'` — never via printf or heredoc string interpolation.
- Confirm with one line: `captured: #<id> <title>  →  Inbox`.
- This skill writes to the Inbox only — it never classifies, moves, or deletes anything. Use `/flowhub-triage` for that.
