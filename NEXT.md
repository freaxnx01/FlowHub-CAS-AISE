# Next Session Prompt

> **2026-05-16 update — see top of file.** The block snapshot below ("submission-ready on paper", 171 tests, `v1.0.0` plan, 2026-07-06 deadline) is **superseded**. Submission tag is now `v0.1.0`, deadline is **2026-07-04 24:00**, total test count is **234**, and the SUBMISSION-document side (TOC, Fazit, NfA SMART, ACs, ER, Hilfsmittelverzeichnis, Eigenständigkeitserklärung, README) has been overhauled — the docs PR is on branch `worktree-doc`, step 1 (operator tooling + README + build pipeline) already merged via commit `07c43ad`. Keep this header section authoritative; the legacy snapshot below is kept for traceability only.

---

## [Open · critical] CI on main is red — 5 Persistence tests fail vs. seed migration

CI run [`25961611444`](https://github.com/freaxnx01/FlowHub-CAS-AISE/actions/runs/25961611444) on `main` (commit `07c43ad`, 2026-05-16) — **5 / 29 fail** in `FlowHub.Persistence.Tests`. **Not** caused by today's docs-only commit; pre-existing since commit `738079d` (*feat(persistence): seed baseline Skills + Integrations via EF migration*, 2026-05-12) introduced an EF seed that the tests still expect to be absent.

**Failing tests** (all in `tests/FlowHub.Persistence.Tests/Repositories/EfSkillRegistryTests.cs`):

- `GetHealthAsync_EmptyDb_ReturnsEmptyList` — expects an empty result; gets `Articles`, plus the four seeded skills (`Movies`, `Quotes`, `Wallabag`, `Vikunja`).
- `GetHealthAsync_ReturnsAllSkills` — expects N seeded by the test; gets `N + 5` because the migration seeds first.
- (+ three more in the same file, same root cause.)

**Why it matters for the submission:**

- SUBMISSION.md §3.3 claims "234 / 234 tests green" and links the CI dashboard. Currently red.
- Two rubric items take a direct hit if not fixed: *Unit-Tests (3 pt)* and *Test-Ergebnisse dokumentiert (3 pt)*. Together that's a 6-point exposure on a 90-point effective max.
- Pre-flight gate A in `submission-notes.md` requires `make test` green.

**Fix options** (pick one — first is cheapest):

1. **Update the tests** to account for the seed. Either swap to `Contains` assertions or set up the test fixture to TRUNCATE-then-reseed-an-empty-set per case. Keeps production migration unchanged.
2. **Guard the seed** behind `if (env != "Testing")` in the migration's `OnModelCreating` / data-seeding entry point. Cleaner conceptually but touches a committed migration.
3. **Use a separate test `DbContext`** that overrides `OnModelCreating` to skip the seed. Most surgical, requires a small infra change in `PostgresFixture`.

Recommendation: **Option 1** unless you want to invest now in a test-context split — Option 1 lands in one commit, fits the next session.

---

## Legacy snapshot — Block 5 Nachbereitung (2026-05-12)

> Kept below for traceability. Reality has moved on; see the 2026-05-16 update at the top of this file.

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
