# Submission notes — how the Moodle PDF is produced

Operator notes for the CAS AISE submission. Not part of the submitted artefact itself — internal documentation of the build process and the trade-offs behind it.

## TL;DR

- **Source of truth:** `SUBMISSION.md` (Markdown, German) + `docs/submission/eigenstaendigkeitserklaerung.md` (German, the signed Beilage).
- **Mandatory Moodle uploads:**
  - `SUBMISSION.pdf` — hub-style submission document, ~10 pages, clickable links into the GitHub `main` branch.
  - `Eigenständigkeitserklärung.pdf` — separate signed PDF with Hilfsmittelverzeichnis + Eigenständigkeitserklärung (FFHS mandatory beilage).
- **Optional safety net:** `SUBMISSION-bundle.pdf` (~150–250 pages, everything inlined).
- **Build:** `just pdf-submission`, `just pdf-eigenstaendigkeitserklaerung`, `just pdf-submission-bundle`. All three regenerate from the same Markdown sources, no manual edits to the PDFs.

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
just pdf-submission
# writes SUBMISSION.pdf in the repo root
```

Renders `SUBMISSION.md` only. Links to repo content remain hyperlinks in the PDF and resolve to `https://github.com/freaxnx01/FlowHub-CAS-AISE/...` on the `main` branch.

### Bundle PDF (safety net)

```bash
just pdf-submission-bundle
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

## Submission TODO checklist

Walk this list top-to-bottom. Each step is gated by the previous.

### A — Code freeze (T-7 to T-1 days before deadline)

- [ ] All Block-Nachbereitungen show `[x]` for every rubric item
- [ ] **Lernziele covered — all 5 blocks**: each block's Lernziele/Aufträge (`nachbereitung/AISE_Projektarbeit_Auftraege.md`) are met in the matching `vault/Blöcke/0N … - c) Nachbereitung.md`
- [ ] **Rubric self-check — all 5 blocks**: run the `cas-aise-grade-self-check` skill against every Block-Nachbereitung (1–5); each reports ≥ 88 / 90 (or noted gaps consciously accepted)
- [ ] **Remaining CAS todos cleared**: run `cas-aise-todo-list full` — no open `- [ ]` items left that gate submission
- [ ] `just test` green: **234 / 234** across all 8 test projects
- [ ] `just build` clean with warnings-as-errors enforced
- [ ] `git status` clean on `main`; no uncommitted/untracked files
- [ ] Last CI run on `main` is green: `gh run list --workflow=ci.yml --branch=main --limit 1`
- [ ] Tag `v0.1.0` exists and matches `<Version>0.1.0</Version>` in `Directory.Build.props`
- [ ] GitHub Release for `v0.1.0` published with CHANGELOG content
- [ ] No secret value leaked into the public tree (manual scan or `gitleaks`)
- [ ] **No copyrighted / sensitive Moodle material in the public tree _or_ the bundle.** The repo is public and `SUBMISSION-bundle.pdf` inlines referenced Markdown, so neither may carry instructor-owned content: course slides/scripts, handouts, the verbatim Bewertungskriterien/rubric, exam material, or other students' work. Own paraphrases and notes are fine; verbatim third-party content is not. `.gitignore` already excludes `vault/_files/Moodle/`, `**/Moodle/Modul/`, `*_Moodle.pdf` — verify nothing slipped past:
  - `git ls-files vault | grep -iE 'moodle|handout|folie|slide|skript|pva.?material'`
  - Eyeball tracked binaries for instructor IP: `git ls-files 'vault/_files/*' 'vault/_images/*'` (e.g. course brochures, slide screenshots)
  - Confirm the inline list in `tools/submission-bundle.sh` pulls in none of the above

### B — End-to-end acceptance (T-3 to T-1 days)

- [ ] Run [`docs/runbooks/v0.1.0-final-acceptance.md`](../runbooks/v0.1.0-final-acceptance.md) on a clean host — all 7 steps pass
- [ ] Public demo at `https://demo.flowhub.freaxnx01.ch` reachable, classification + keyword fallback both verified
- [ ] OpenRouter spend dashboard shows the demo key well below the $1 cap

### C — Submission document content (T-2 to T-1 days)

- [ ] `SUBMISSION.md` proofread end-to-end (German *and* English passages)
- [ ] Demo URL in §2 still resolves; demo currently running
- [ ] §3 TOC: every link still resolves (no 404s — `gh repo view` against each path)
- [ ] `docs/submission/eigenstaendigkeitserklaerung.md` §1 Hilfsmittelverzeichnis covers every aid actually used (DeepL, Copilot, ChatGPT, Claude variants, …); ratios in line with `docs/ai-usage.md`
- [ ] `docs/submission/eigenstaendigkeitserklaerung.md` §2 Eigenständigkeitserklärung: **Ort = Sisseln**, Name = Andreas Imboden, Datum auf den Abgabe-Tag aktualisiert
- [ ] Submission deadline (2026-07-04 24:00) reflected in §1 of SUBMISSION.md

### D — Render PDFs (T-1 day)

- [ ] `just pdf-submission` → `SUBMISSION.pdf` regenerated without warnings
- [ ] `just pdf-submission-bundle` → `SUBMISSION-bundle.pdf` regenerated without warnings
- [ ] `just pdf-eigenstaendigkeitserklaerung` → `Eigenständigkeitserklärung.pdf` regenerated without warnings
- [ ] Open `SUBMISSION.pdf` in a viewer:
  - [ ] Table of contents links are clickable
  - [ ] Demo URL is clickable
  - [ ] §6 points to the separate `Eigenständigkeitserklärung.pdf`
- [ ] Open `Eigenständigkeitserklärung.pdf` in a viewer:
  - [ ] Hilfsmittelverzeichnis table renders cleanly
  - [ ] Signature line is visible and reachable for pen or digital signature
- [ ] Spot-check `SUBMISSION-bundle.pdf`: page-break separators present between sections, no missing files

### E — Sign the Eigenständigkeitserklärung (T-1 to T-0)

The signature applies to the **separate `Eigenständigkeitserklärung.pdf`**, not to `SUBMISSION.pdf` itself. Pick **one** of the two paths:

**E1 — Paper signature (classic):**
- [ ] Print `Eigenständigkeitserklärung.pdf` (1–2 pages)
- [ ] Fill in the date next to "Sisseln, _________" by hand
- [ ] Sign with pen on the signature line
- [ ] Scan the signed pages back to PDF, replacing `Eigenständigkeitserklärung.pdf`

**E2 — Digital signature:**
- [ ] Open `Eigenständigkeitserklärung.pdf` in a signature tool (Adobe Acrobat / signed.com / FFHS smartcard certificate)
- [ ] Fill in the date and apply a digital signature on the signature line
- [ ] Save the signed PDF, overwriting the unsigned version
- [ ] Verify the signature shows as valid after re-opening the PDF

### F — Upload to Moodle (T-0, before 2026-07-04 24:00)

- [ ] Log into FFHS Moodle, navigate to *PVA FS26 → Deployment & Abgabe Projektarbeit*
- [ ] Upload `SUBMISSION.pdf` (primary artefact)
- [ ] Upload `Eigenständigkeitserklärung.pdf` (**signed** — mandatory beilage)
- [ ] If Moodle allows further attachments: also upload `SUBMISSION-bundle.pdf` (offline safety net)
- [ ] Confirm the upload — Moodle shows all files and a submission timestamp
- [ ] Take a screenshot of the confirmation (Moodle drops submissions occasionally; the screenshot is your fallback proof)

### G — Post-submission (T+0)

- [ ] Tag `v0.1.0-submitted` on the exact commit used to render the uploaded PDF (so the submitted state stays git-identifiable)
- [ ] Push the tag: `git push origin v0.1.0-submitted`
- [ ] Save the Moodle confirmation screenshot under `vault/_files/Moodle/` (gitignored, local only)
- [ ] Update `vault/Blöcke/05 Deployment/05 Deployment - c) Nachbereitung.md` — mark *PDF auf Moodle hochladen* as `[x]`

### Pre-flight quick check

**Preferred — full dry-run:** run the **`cas-aise-submission-preflight`** skill. It rebuilds `SUBMISSION-bundle.pdf`, verifies every TOC-referenced file resolves, **scans the staged tree for Moodle source-material leaks** (the copyright check from Section A), folds in the `cas-aise-grade-self-check` rubric report, and prints a copy-pasteable manual upload checklist. This is the single most comprehensive gate before clicking upload.

**Minimal — one command (build/test/render only):**

```bash
just build && just test && \
  just pdf-submission && just pdf-eigenstaendigkeitserklaerung && just pdf-submission-bundle && \
  echo "Pre-flight OK — proceed to sign Eigenständigkeitserklärung.pdf and upload"
```

## Outputs are gitignored

`SUBMISSION.pdf` and `SUBMISSION-bundle.pdf` are build artefacts — they are produced on demand from Markdown and **not committed**. Only `SUBMISSION.md` (and the supporting sources) is in version control.
