# Next Session Prompt

Execute the Block 4 Nachbereitung implementation plan using the subagent-driven development skill.

The plan is split into 5 slice files under `docs/superpowers/plans/`:

- `2026-05-05-block4-nachbereitung.md` — index with full file map and rubric coverage
- `2026-05-05-block4-slice1-foundation.md` — Tasks 1–4 (start here)
- `2026-05-05-block4-slice2-channel-skill.md` — Tasks 5–7
- `2026-05-05-block4-slice3-full-domain.md` — Tasks 8–12
- `2026-05-05-block4-slice4-tests-docker.md` — Tasks 13–17
- `2026-05-05-block4-slice5-docs.md` — Tasks 18–20

Use `/superpowers:subagent-driven-development` and start with Slice 1. Each slice depends on the previous one completing successfully.

Branch: `feat/beta-mvp` (carry-forward — continue on this branch).

Prerequisites before starting:
- Docker must be running (Testcontainers and `make db-up` both need it)
- Run `make build` first to confirm baseline compiles
