Parse a Capture that references a software project, identify the repo across all forges, and create an issue there.

Input: $ARGUMENTS

Format: `<project-reference> <issue title and optional body>`

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
