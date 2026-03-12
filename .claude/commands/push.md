Push commits to the remote repository.

Context: $ARGUMENTS

## Steps

1. Run `git status` — if working tree is dirty, suggest `/commit` first
2. Run `git log origin/$(git branch --show-current)..HEAD --oneline` to see what will be pushed
3. If there are no unpushed commits, say so and stop
4. Show the list of commits and target branch — ask for confirmation
5. If pushing to `main`/`master`, warn and ask for explicit confirmation
6. Push with `git push` (use `-u` if the branch has no upstream yet)
7. Never force-push unless explicitly requested — and never to `main`/`master`
8. Show the result

## Rules
- If push fails due to auth, suggest `gh auth login`
- If push fails due to diverged history, suggest `git pull --rebase` rather than force-push
