# Next Session Prompt

Block 5 Nachbereitung is **submission-ready on paper** as of 2026-05-12. The grade self-check estimates ~88 / 90 (rubric items in Spezifikation, Entwurf, Programmierung, Validierung, KI bucket all addressed); the remaining gaps need a human action.

## Repo snapshot (2026-05-12)

- `main` ahead of `origin/main` by 2 commits: `58b316c` (rubric-gap doc fixes) + this CHANGELOG/ai-usage update.
- 171 tests pass (`make test`, excludes AI/BetaSmoke/E2E). `make smoke-prod` green end-to-end including embedding round-trip via Mistral.
- All NEXT.md items 1 + 2 from the previous session closed.

## What still requires a human

1. **Push remaining commits** to `origin/main` — `git push` once you've eyeballed the deltas.

2. **Regenerate the Projektbeschreibung PDF** — `docs/projektbeschreibung/FlowHub_Projektbeschreibung_v4.md` was edited today (PE-1..PE-7 renaming + cross-reference table); the matching `.pdf` is now stale. Whatever toolchain produced `v4.pdf` (Pandoc? a Word export? VS Code "Markdown PDF" extension?) needs one more pass. Confirm the PDF's §7 reflects the new PE-1..PE-7 heading scheme.

3. **Tag `v1.0.0`** when the PDF is regenerated and the CHANGELOG `[Unreleased]` is renamed to `[v1.0.0] — 2026-MM-DD`:

   ```bash
   git tag -a v1.0.0 -m "release: v1.0.0 — CAS AISE Abgabe"
   git push origin v1.0.0
   ```

   This triggers `.github/workflows/release.yml` (GHCR image push + release notes via `git-cliff`).

4. **Upload the PDF to Moodle** before `2026-07-06 00:00`. Repo URL prominently inside the PDF: `github.com/freaxnx01/FlowHub-CAS-AISE`.

## Done in this session

- `make smoke-prod` — full compose-stack probe, six-step. Caught five real defects (`.editorconfig` missing from Docker context, env-casing mismatch, empty-string model fallback, Mistral `dimensions` 422, Makefile/Passbolt shadowing) — each fixed in a separate commit. See `docs/insights/block-5.md` "Defects Found by the Smoke Run".
- `cas-aise-grade-self-check` walked, gap report produced (76/90), six top-leverage gaps closed in commit `58b316c`:
  - ADR drift in v4 § 7 → renamed to PE-1..PE-7 with implementation-ADR cross-reference index.
  - `docs/design/perspectives.md` created — Struktur / Verhalten / Interaktion + Mermaid lifecycle state diagram + hot-path sequence.
  - `docs/spec/use-cases.md` — UC-10 / UC-11 collision renumbered to UC-17 / UC-18; explicit `Akzeptanzkriterien` blocks added to UC-01..UC-11.
  - `docs/design/db/er.md` — Block-5 `Embedding vector(1024)` + HNSW index.
  - `docs/insights/block-5.md` — test result matrix (171 tests), smoke transcript, five defects.
- `CHANGELOG.md` — full Block-5 section appended (Added / Changed / Test Results), ready to be renamed `[v1.0.0]` at tag time.
- `docs/ai-usage.md` — added "Ultrareview-driven correction" + "Smoke-driven correction" subsections to the Block 5 reflection.

## Notes

- The `[v1.0.0]` rename is a deliberate manual step — don't auto-tag from the agent. The user confirms PDF + final content before pushing the tag that triggers a public release.
- `make smoke-prod` should be the **last** thing run before regenerating the PDF, to confirm the deployment claim is reproducible.
