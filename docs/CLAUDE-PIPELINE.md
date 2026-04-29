# Claude Pipeline

This repo delegates autonomous issue implementation to the [`freaxnx01/claude-pipeline`](https://github.com/freaxnx01/claude-pipeline) reusable workflow. The local entry point is `.github/workflows/claude.yml`, which forwards work to `claude-implement.yml` in the pipeline repo.

## What `.github/workflows/claude.yml` does

The workflow is a thin consumer stub. It defines two triggers (`issues: [labeled]` and `workflow_dispatch`), gates execution on the `ai-implement` label or a manual dispatch, and calls the reusable workflow with the target issue number and an attempt counter. Permissions are scoped to `contents: write`, `issues: write`, `pull-requests: write`, and `actions: write` so the called workflow can branch, commit, open a draft PR, comment on the issue, and re-dispatch itself for retries. Runners are pinned to `ubuntu-latest` (no self-hosted runners) and the timeout budget is 60 minutes.

## How a maintainer triggers a run

Apply the `ai-implement` label to any issue you want the pipeline to implement. Only users with write access can attach that label, which is what gates the trigger on a public repo. A manual replay is available via the **Actions → claude → Run workflow** dispatch, which takes an `issue-number` and an `attempt` counter.

## Where to find runtime details

After a run, the pipeline posts a comment on the originating issue containing the run URL, branch name, draft PR link, and any retry hints. That issue comment is the canonical place to inspect what happened — the Actions log is secondary.

## Retry behavior

Reruns are dispatched via `gh workflow run` with an incremented `attempt` input. The retry policy in the called workflow caps reruns and distinguishes between rate-limit errors, transient infrastructure failures, and `max-turns` exhaustion. Rate-limit and transient failures are retried; `max-turns` exhaustion surfaces in the issue comment so a maintainer can decide whether to dispatch another attempt.

## Pinned reusable workflow ref

The workflow currently calls `freaxnx01/claude-pipeline/.github/workflows/claude-implement.yml@main`. Pinning to `@main` is intentional while the pipeline iterates; this should move to a tagged release ref once the pipeline stabilises.
