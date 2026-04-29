# Working with the claude-ready bot

A scheduled remote Claude agent runs every 2h against `freaxnx01/quicktask-vikunja` and picks up at most one issue per run. It only sees issues with the `claude-ready` label.

## How to queue an issue

1. Write the issue with a clear, self-contained description:
   - **Goal:** one sentence on what should change and why.
   - **Acceptance:** bullet list of what "done" looks like (user-visible behavior, not implementation).
   - **Pointers:** file paths, function names, or related PRs if known.
   - **Out of scope:** anything the agent should NOT touch.
2. Add the label `claude-ready`.
3. Leave the issue **unassigned**. The agent skips assigned issues.

## What the agent does

- Picks the lowest-numbered `claude-ready` issue with no linked open PR and no assignee.
- Swaps the label to `claude-working` (so the next run won't re-pick it).
- Creates branch `claude/issue-<N>-<slug>`, implements, runs tests/lints, commits, pushes, opens a PR with `Closes #N`.
- If the issue is ambiguous, it comments on the issue asking for clarification and removes `claude-ready` instead of guessing.

## How to cancel or pause

- Remove the `claude-ready` label → agent ignores it on the next run.
- Assign the issue to a human → agent skips it.
- Add label `blocked` → agent skips it.

## Reviewing the PR

- The agent's PRs are normal PRs — review, request changes, or close as you would any human PR.
- If you request changes, comment on the PR. The agent does NOT auto-iterate on review feedback (that's a separate workflow).
- Merging or closing the PR is your call.

## Good-fit issues

- Bug fixes with a clear repro.
- Small features with crisp acceptance criteria.
- Refactors confined to a named module.

## Bad-fit issues (don't label)

- Architecture decisions / "what should we do about X".
- Anything requiring product judgment or UX decisions.
- Cross-cutting changes touching many unrelated areas.
- Anything where the spec is "make it better".

---

## Repos currently wired to the bot

- `freaxnx01/quicktask-vikunja`

(Extend this list as new scheduled agents are configured. `/flowhub-issue` consults this file to decide whether to offer the `claude-ready` path.)
