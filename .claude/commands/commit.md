Create a commit following project conventions.

Context: $ARGUMENTS

## Steps

1. Run `git status` to see untracked and modified files
2. Run `git diff` to see staged and unstaged changes
3. Run `git log --oneline -5` to see recent commit style
4. Stage only files relevant to the current task — prefer specific file names over `git add -A`
5. Never stage files that likely contain secrets (`.env`, `credentials.json`, etc.)
6. Draft a Conventional Commits message: `<type>(<scope>): <summary>`
   - Types: `feat` `fix` `test` `refactor` `chore` `docs` `ci` `perf`
   - Scope: module or layer name (e.g. `orders`, `auth`, `blazor`)
   - Summary: imperative mood, ≤72 chars, no period
   - Body (if needed): explain *why*, not *what*
7. Commit using a HEREDOC for the message — do NOT amend unless explicitly asked, do NOT skip hooks
8. Run `git status` to confirm clean working tree
9. Show the commit hash and message

## Rules
- If there are no changes, say so and stop
- If a pre-commit hook fails, fix the issue and create a NEW commit (do not amend)
- Never push — use `/push` for that
