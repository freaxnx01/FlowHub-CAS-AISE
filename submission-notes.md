# Submission notes — how the Moodle PDF is produced

Operator notes for the CAS AISE submission. Not part of the submitted artefact itself — internal documentation of the build process and the trade-offs behind it.

## TL;DR

- **Source of truth:** `SUBMISSION.md` (Markdown, English).
- **Primary artefact uploaded to Moodle:** `SUBMISSION.pdf` (hub style, ~10 pages, clickable links into the GitHub `main` branch).
- **Secondary artefact (safety net):** `SUBMISSION-bundle.pdf` (~150–250 pages, everything inlined).
- **Build:** `make pdf-submission` and `make pdf-submission-bundle`. Both regenerate from the same Markdown sources, no manual edits to the PDFs.

## Why two PDFs?

The Moodle wording is *"laden Sie diese als PDF hoch. Die Arbeit enthält die URL auf das Git-Repository Ihrer Lösung"* — the submitted PDF must include the repo URL, not necessarily inline every artefact. Two plausible designs:

| Aspect | Hub PDF (primary) | Bundle PDF (safety net) |
|---|---|---|
| Size | ~10 pages | ~150–250 pages |
| Reviewer workflow | clicks links into `main` | scrolls linearly, all offline |
| Maintenance | any repo change is live immediately | rebuild per change, but deterministic snapshot |
| Risk | reviewer doesn't click → submission looks thin | format drift, larger file |
| Moodle conformance | satisfies wording | satisfies wording + redundant |

The Hub is small, current, and the natural fit for the wording. The Bundle eliminates the "what if the reviewer doesn't click" risk and provides a frozen-in-time snapshot at submission tag `v0.1.0`. Producing both costs almost nothing because both render from the same sources.

**Decision:** upload the **Hub PDF** as the primary submission, and the **Bundle PDF** alongside it if Moodle accepts multiple attachments (otherwise keep Bundle as an offline backup, ready on request).

## Building the PDFs

### Hub PDF (primary)

```bash
make pdf-submission
# writes SUBMISSION.pdf in the repo root
```

Renders `SUBMISSION.md` only. Links to repo content remain hyperlinks in the PDF and resolve to `https://github.com/freaxnx01/FlowHub-CAS-AISE/...` on the `main` branch.

### Bundle PDF (safety net)

```bash
make pdf-submission-bundle
# writes SUBMISSION-bundle.pdf in the repo root
```

Internally:

1. `tools/submission-bundle.sh` concatenates `SUBMISSION.md` and every referenced Markdown source in TOC order, inserting page-break separators and per-section headers.
2. The combined Markdown is written to `tools/build/submission-bundle.md` (gitignored).
3. `tools/md-to-pdf/render.mjs` renders the combined file to `SUBMISSION-bundle.pdf` via the same Puppeteer pipeline used for `pdf-projektbeschreibung`.

The bundle inclusion list is defined in `tools/submission-bundle.sh`; edit there to add/remove files. Anything outside the inclusion list stays referenced by URL.

## What is *not* inlined into the bundle

- The `vault/Knowledge/*` background notes (linked only).
- Large historical artefacts (`docs/projektbeschreibung/v2`, `v3` — only v4 is current).
- The `tests/` source code (cited but not embedded — too long, structurally redundant with the test-strategy doc).
- Generated artefacts under `docs/superpowers/{specs,plans}/` (working notes, not deliverables).
- The vault's `_files/Moodle/` directory (gitignored copyright FFHS).

## Pre-flight checklist

Before generating the final submission PDFs:

- [ ] `git status` clean on `main`
- [ ] Tag `v0.1.0` matches the version in `Directory.Build.props`
- [ ] Last CI run on `main` is green (`gh run list --workflow=ci.yml --limit 1`)
- [ ] `make pdf-submission` regenerates `SUBMISSION.pdf` without warnings
- [ ] `make pdf-submission-bundle` regenerates `SUBMISSION-bundle.pdf` without warnings
- [ ] Manual smoke read of `SUBMISSION.pdf` (table of contents links resolve, demo URL renders)
- [ ] Upload before **2026-07-04 24:00** (two weeks after PVA 2026-06-20)

## Outputs are gitignored

`SUBMISSION.pdf` and `SUBMISSION-bundle.pdf` are build artefacts — they are produced on demand from Markdown and **not committed**. Only `SUBMISSION.md` (and the supporting sources) is in version control.
