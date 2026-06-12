# Next Session Prompt

> **2026-05-16 update — see top of file.** The block snapshot below ("submission-ready on paper", 171 tests, `v1.0.0` plan, 2026-07-06 deadline) is **superseded**. Submission tag is now `v0.1.0`, deadline is **2026-07-04 24:00**, total test count is **234**, and the SUBMISSION-document side (TOC, Fazit, NfA SMART, ACs, ER, Hilfsmittelverzeichnis, Eigenständigkeitserklärung, README) has been overhauled — the docs PR is on branch `worktree-doc`, step 1 (operator tooling + README + build pipeline) already merged via commit `07c43ad`. Keep this header section authoritative; the legacy snapshot below is kept for traceability only.

---

## [Tooling · 2026-05-16] New `cas-aise-submission-preflight` skill

Released `cas-aise-submission-preflight` v0.1.1 in the [freax-claude-code-plugins](https://github.com/freaxnx01/claude-code-plugins) marketplace — sibling to `cas-aise-grade-self-check` and `cas-aise-todo-list`. Read-only dry-run before clicking submit on Moodle: rebuilds `SUBMISSION-bundle.pdf`, verifies TOC integrity, scans for Moodle-content leaks, folds in the rubric self-check, and prints a copy-pasteable manual upload checklist.

Caught a real bundle-build failure on first invocation against Block 5 (three missing TOC targets in `tools/submission-bundle.sh` — `acceptance-criteria.md`, `db-model.md`, `v0.1.0-final-acceptance.md`). Driving fix cascade led to today's submission-readiness work:

- `docs/spec/acceptance-criteria.md` — consolidated 50 ACs across 5 categories with per-criterion verification pointers (46 verified / 4 deferred-historical). Closed the rubric **Abnahmekriterien** line.
- `tools/submission-bundle.sh` — re-pointed `db-model.md` to existing `docs/design/db/{entities,er}.md`; dropped the unwritten `v0.1.0-final-acceptance.md` entry.
- 5 short stubs fleshed out: `docs/adr/README.md`, `docs/insights/block-{1,2,3}.md`, `vault/Blöcke/01 Einführung/...Nachbereitung.md` — now match the structure of block-4/5 insights with per-slice AI usage, metrics tables, and KI-Reflexion sections.
- `vault/Organisation/Termine.md` — added the Block 5 `Abgabe (Block 5 / Final): 2026-07-04 24:00` entry so the deadline source is no longer NEXT.md-only.
- `vault/Knowledge/Coursework-Glossary.md` — tracked.

Latest preflight verdict against `main@71d14d5`: **✅ READY**, all gates pass, no warnings. `cas-aise-grade-self-check` Block 5 estimate: **90 / 90** (pessimistic floor ~86; Quarkus N/A).

Release: <https://github.com/freaxnx01/claude-code-plugins/releases/tag/cas-aise-submission-preflight/v0.1.1>

---

## [Closed · 2026-05-16] CI on main was red — Persistence tests + E2E-in-CI

Two related issues, both closed today:

1. **Persistence tests vs. seed migration** (option 1 above) — fixed by commit `343c07c` (squash of PR #16). `PostgresFixture.CreateFreshDbAsync(bool seedCatalog = true)` now TRUNCATEs `Skills` + `Integrations` after migration when callers opt out; the 2 affected test classes pass `seedCatalog: false`. `FlowHub.Persistence.Tests` is back to 29 / 29.
2. **E2E project ran in CI without a web server** — `just test` excludes `Category!=AI&Category!=BetaSmoke&Category!=E2E`, but `.github/workflows/ci.yml` did not. Fixed by adding the same filter to the CI test step. The full 28-journey Playwright suite still runs locally against `just watch` (and in any pipeline that boots the web container first).

---

## Legacy snapshot — Block 5 Nachbereitung (2026-05-12)

> Kept below for traceability. Reality has moved on; see the 2026-05-16 update at the top of this file.

Block 5 Nachbereitung is **submission-ready on paper** as of 2026-05-12. The grade self-check estimates ~88 / 90 (rubric items in Spezifikation, Entwurf, Programmierung, Validierung, KI bucket all addressed); the remaining gaps need a human action.

## Repo snapshot (2026-05-12)

- `main` ahead of `origin/main` by 2 commits: `58b316c` (rubric-gap doc fixes) + this CHANGELOG/ai-usage update.
- 171 tests pass (`just test`, excludes AI/BetaSmoke/E2E). `just smoke-prod` green end-to-end including embedding round-trip via Mistral.
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

- `just smoke-prod` — full compose-stack probe, six-step. Caught five real defects (`.editorconfig` missing from Docker context, env-casing mismatch, empty-string model fallback, Mistral `dimensions` 422, justfile/Passbolt shadowing) — each fixed in a separate commit. See `docs/insights/block-5.md` "Defects Found by the Smoke Run".
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
- `just smoke-prod` should be the **last** thing run before regenerating the PDF, to confirm the deployment claim is reproducible.
