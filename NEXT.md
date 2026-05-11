# Next Session Prompt

Block 4 + Block 5 are merged to `main` (PRs #12, #13, #14, all squash-merged). Repository, branches, and worktrees are clean as of 2026-05-11.

## Repo snapshot (2026-05-11)

- **Branches:** `main` + `feat/block5-nachbereitung` only (both local and on origin).
- **Both branches at the same commit:** `744a98c` (`docs: add ROADMAP.md (#14)`).
- **Worktrees:** main repo + `.claude/worktrees/block5-nachbereitung`.
- **Stale branches deleted today:** `feat/block4-nachbereitung`, `docs/block3-rubric-gaps`, `docs/claude-pipeline-overview` (commit cherry-picked first), `feat/slice-c-ai-integration`, `worktree-ai` (content merged via PR #14).
- **`docs/ai-usage.md`** now includes the "Custom Skills (self-authored)" section listing `cas-aise-todo-list`, `cas-aise-grade-self-check`, and `sync-ai-instructions` with the plugin source repo.

## Recommended next tasks (in order)

1. **Smoke-test the production compose stack** (now on `main`)
   - `docker compose up --build`
   - Verify: `flowhub.migrations` exits 0 → `flowhub.web` becomes healthy → `curl http://localhost:5070/health/live` → 200; `curl http://localhost:5070/metrics` → Prometheus exposition with `dotnet_*` and `http_*` series; submit a Capture via REST and confirm an embedding appears (`CaptureEmbeddingConsumer`).
   - This was the only deployment-stack claim never exercised end-to-end before the 2026-05-07 ultrareview fix cycle.

2. **Run `cas-aise-grade-self-check` on Block 5 Nachbereitung**
   - Surface any remaining rubric gaps before final submission.
   - Read-only — produces a gap report.

3. **Final submission packaging** (when Block 5 Nachbereitung is rubric-green)
   - Confirm `docs/insights/block-5.md` covers what the rubric expects.
   - Verify `docs/ai-usage.md` is current (including the ultrareview-driven embedding-pipeline shift from sync-on-submit → `CaptureEmbeddingConsumer`).
   - Generate the Projektbeschreibung PDF (see `docs/projektbeschreibung/`).

## Notes

- This worktree (`feat/block5-nachbereitung`) is currently identical to `main`. Either continue Block 5 Nachbereitung work here, or fold back to `main` and delete the worktree.
- Per CLAUDE.md: invoke `cas-aise-grade-self-check` *before* claiming any Block-Nachbereitung is done — rubric is active context during the Nachbereitung phase.
