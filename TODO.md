# TODO

Session wrap-up 2026-07-08. Working tree is clean, `main` is up to date with
`origin/main`, and both PRs opened this session are merged (#182, #183). The
items below are **follow-ups**, not unfinished in-tree work.

## Submission PDFs (optional — only if ever rebuilt/re-uploaded)

- [ ] The already-uploaded Moodle PDFs still carry the pre-fix wording/links:
  the non-word "öffenbar" (fixed in `SUBMISSION.md` via #182) and the old
  `04 Persitence` URLs (fixed via #183). No rubric item scores this, so
  re-upload is **not required**. If the submission set is ever regenerated
  (`just package-submission` / `just pdf-submission-bundle`), the corrections
  will flow through automatically — the bundle manifest in
  `tools/submission-bundle.sh` now points at the renamed `04 Persistence` file.

## Deliberate decision (no action — recorded so it isn't "re-fixed")

- [ ] Dated historical artifacts still reference the old `04 Persitence` path on
  purpose — they are timestamped audit snapshots: `docs/superpowers/specs/*`,
  `docs/superpowers/plans/*`, `nachbereitung/examiner-sim/report-2026-06-27*`,
  and the `docs/project/TODO.md` narrative of the earlier manifest fix. Leave
  as-is; rewriting them would falsify the record.
