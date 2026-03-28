# Commit — Slash Command

Create a commit following project conventions.

**Target:** $ARGUMENTS

---

## Steps

### Step 1 — Assess changes
- Run `git status` to see untracked and modified files
- Run `git diff` to see staged and unstaged changes
- Run `git log --oneline -5` to see recent commit style

### Step 2 — Stage files
- Stage only files relevant to the current task
- Never stage files that likely contain secrets (`.env`, `credentials.json`, etc.)
- Prefer specific file names over `git add -A` or `git add .`

### Step 3 — Draft commit message
- Follow Conventional Commits: `<type>(<scope>): <summary>`
- Types: `feat` `fix` `test` `refactor` `chore` `docs` `ci` `perf`
- Scope: module or layer name (e.g. `orders`, `auth`, `blazor`)
- Summary: imperative mood, ≤72 chars, no period
- Body (if needed): explain *why*, not *what*
- If arguments were provided, use them as context for the message

### Step 4 — Commit
- Use a HEREDOC for the message to ensure correct formatting
- Do NOT amend existing commits unless explicitly asked
- Do NOT skip hooks (`--no-verify`)

### Step 5 — Verify
- Run `git status` to confirm clean working tree
- Show the commit hash and message

---

## Rules
- If there are no changes to commit, say so and stop
- If a pre-commit hook fails, fix the issue and create a NEW commit (do not amend)
- Never push — use `/push` for that
