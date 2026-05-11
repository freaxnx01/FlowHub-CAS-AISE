# Push — Slash Command

Push commits to the remote repository.

**Target:** $ARGUMENTS

---

## Steps

### Step 1 — Pre-flight checks
- Run `git status` to confirm working tree is clean (if not, suggest `/commit` first)
- Run `git log origin/$(git branch --show-current)..HEAD --oneline` to see what will be pushed
- If there are no unpushed commits, say so and stop

### Step 2 — Confirm with user
- Show the list of commits that will be pushed and the target branch
- If pushing to `main`/`master`, warn and ask for explicit confirmation
- If arguments include `--force` or `-f`, warn about consequences and ask for confirmation

### Step 3 — Push
- Use `git push` (with `-u` if the branch has no upstream yet)
- Never force-push unless the user explicitly requested it
- Show the result

---

## Rules
- Never push without showing what will be pushed first
- Never force-push to `main`/`master`
- If push fails due to auth, suggest `gh auth login` or SSH setup
- If push fails due to diverged history, suggest `git pull --rebase` rather than force-push
