# Submission notes — how the Moodle PDFs are produced

Operator notes for the CAS AISE submission. Not part of the submitted artefact
itself — internal documentation of the build process and the upload procedure.

## TL;DR

- **Source of truth:** Markdown in the repo (German). The uploaded PDFs are
  rendered on demand from these sources — no manual edits to the PDFs.
- **Moodle upload set — 5 files** (Moodle limit: max. 5 Anhänge, 20 MB/Datei):

  | # | Datei | Quelle | Build |
  |---|---|---|---|
  | 1 | `FlowHub_Uebersicht.pdf` (Einstieg/Übersicht + Index, ~8 S.) | `SUBMISSION.md` | `just pdf-submission` |
  | 2 | `FlowHub_Arc42_v2.pdf` (Architektur, as built, ~24 S.) | `docs/architektur/FlowHub_Arc42_v2.md` | `just pdf-arc42` |
  | 3 | `FlowHub_Reflexion.pdf` (KI-Reflexion, ~7 S.) | `docs/reflexion/FlowHub_Reflexion.md` | `just pdf-reflexion` |
  | 4 | `flowhub-praesentation.pdf` (Foliensatz, ~18 S.) | `docs/presentation/flowhub-praesentation.md` (Marp) | Marp-Build |
  | 5 | `Eigenständigkeitserklärung.pdf` (**signiert**, ~2 S.) | `docs/submission/eigenstaendigkeitserklaerung.md` | `just pdf-eigenstaendigkeitserklaerung` |

- **Repo-only Sicherheitsnetz:** `SUBMISSION-bundle.pdf` (~267 S., alle Artefakte
  inline) via `just pdf-submission-bundle` — nicht hochgeladen, nur Archiv.

## Dokumentenmodell

Die Moodle-Vorgabe lautet *„laden Sie diese als PDF hoch. Die Arbeit enthält die
URL auf das Git-Repository Ihrer Lösung"*. FlowHub liefert ein **Mehr-Dokumenten-Set**:

- **`FlowHub_Uebersicht`** ist der Einstieg: öffnet mit der Dokumentenübersicht,
  enthält die Projektzusammenfassung und ein klickbares Inhaltsverzeichnis, das
  jedes Artefakt auf dem `main`-Branch verlinkt (inkl. der Repo-URL).
- **`Arc42`** und **`Reflexion`** sind die inhaltlichen Hauptdokumente
  (Architektur bzw. KI-Reflexion), die ohne GitHub-Browsing lesbar sind.
- **`Präsentation`** ist der Foliensatz.
- **`Eigenständigkeitserklärung`** ist die signierte FFHS-Pflicht-Beilage.
- Das **Bundle** (267 S., alles inline) bleibt das Offline-Archiv im Repo — es
  wird nicht hochgeladen (das Upload-Limit ist mit 5 Dateien erreicht), steht
  aber als Sicherheitsnetz bereit, falls GitHub nicht erreichbar ist.

Diagramme werden über die vendored-Mermaid-Pipeline (`render.mjs`) **offline**
gerendert; ein Render-Guard bricht den Build ab, falls ein Diagramm nicht
rendert — ein grüner Build heisst also: Diagramme intakt.

## Submission TODO checklist

Walk this list top-to-bottom. Each step is gated by the previous.

### A — Code freeze (T-7 to T-1 days before deadline)

- [ ] All Block-Nachbereitungen show `[x]` for every rubric item
- [ ] **Rubric self-check — all 5 blocks**: run `cas-aise-grade-self-check`
      against every Block-Nachbereitung (1–5); rubric is /100 (framework-neutral
      since the June-2026 update), gaps consciously accepted are documented
- [ ] **Remaining CAS todos cleared**: `cas-aise-todo-list full` — no open
      submission-gating `- [ ]` items
- [ ] `just test` green: **294 / 294** across the 6 offline test projects
- [ ] `just build` clean with warnings-as-errors enforced
- [ ] `git status` clean on `main`; no uncommitted/untracked files
- [ ] Last CI run on `main` is green: `gh run list --workflow=ci.yml --branch=main --limit 1`
- [ ] **No secret value** leaked into the public tree (manual scan or `gitleaks`)
- [ ] **No copyrighted / sensitive Moodle material** in the public tree _or_ the
      bundle. The repo is public and `SUBMISSION-bundle.pdf` inlines referenced
      Markdown, so neither may carry instructor-owned content: course
      slides/scripts, handouts, the verbatim Bewertungskriterien/rubric, exam
      material, verbatim Aufträge/Lernziele, or other students' work. Own
      paraphrases are fine. Verify nothing slipped past:
  - `git ls-files vault | grep -iE 'moodle|handout|folie|slide|skript|pva.?material'`
  - Eyeball tracked binaries for instructor IP: `git ls-files 'vault/_files/*' 'vault/_images/*'`
  - Confirm the inclusion list in `tools/submission-bundle.sh` pulls in none of the above

### B — End-to-end acceptance (T-3 to T-1 days)

- [ ] Public demo at `https://demo.flowhub.freaxnx01.ch` reachable; classification
      + keyword fallback both verified
- [ ] OpenRouter spend dashboard shows the demo key well below its cap

### C — Submission document content (T-2 to T-1 days)

- [ ] `SUBMISSION.md`, `Arc42_v2.md`, `Reflexion.md` proofread end-to-end
- [ ] Demo URL in the hub (§2) resolves; demo currently running
- [ ] Hub TOC + Dokumentenübersicht: every link still resolves (no 404s)
- [ ] `eigenstaendigkeitserklaerung.md` §1 Hilfsmittelverzeichnis covers every aid
      actually used (Claude/Copilot/…); ratios in line with `docs/ai-usage.md`
- [ ] `eigenstaendigkeitserklaerung.md` §2: **Ort = Sisseln**, Name = Andreas
      Imboden, Datum auf den Abgabe-Tag aktualisiert
- [ ] Submission deadline (2026-07-04 24:00) reflected in the hub header

### D — Render the upload PDFs (T-1 day, **with network access** for Mermaid CDN-free render)

- [ ] `just pdf-submission` → `FlowHub_Uebersicht.pdf` (no warnings)
- [ ] `just pdf-arc42` → `docs/architektur/FlowHub_Arc42_v2.pdf`
- [ ] `just pdf-reflexion` → `docs/reflexion/FlowHub_Reflexion.pdf`
- [ ] `flowhub-praesentation.pdf` current (rebuild the Marp deck if the source changed)
- [ ] `just pdf-eigenstaendigkeitserklaerung` → `Eigenständigkeitserklärung.pdf`
- [ ] *(optional)* `just pdf-submission-bundle` → `SUBMISSION-bundle.pdf` (archive)
- [ ] Open each PDF: bookmarks/TOC clickable, **all diagrams rendered** (the guard
      fails the build otherwise), no horizontal table clipping

### E — Sign the Eigenständigkeitserklärung (T-1 to T-0)

The signature applies to the **separate `Eigenständigkeitserklärung.pdf`**. Pick one:

**E1 — Paper signature:**
- [ ] Print `Eigenständigkeitserklärung.pdf`, fill the date next to "Sisseln, ____", sign, scan back to PDF (overwrite the file)

**E2 — Digital signature:**
- [ ] Open in a signature tool, fill the date, apply a digital signature, save (overwrite), verify the signature is valid

### F — Upload to Moodle (T-0, before 2026-07-04 24:00)

- [ ] Run `just package-submission` → assembles the numbered set into `./upload/`:
      `00_FlowHub_Uebersicht.pdf` · `01_FlowHub_Arc42.pdf` · `02_FlowHub_Reflexion.pdf` ·
      `03_FlowHub_Praesentation.pdf` · `04_FlowHub_Eigenstaendigkeitserklaerung.pdf`
- [ ] **Sign** `04_FlowHub_Eigenstaendigkeitserklaerung.pdf` (overwrite in `./upload/`)
- [ ] Log into FFHS Moodle → *PVA FS26 → Deployment & Abgabe Projektarbeit*
- [ ] Upload the **5 files** from `./upload/` (the `00–04` prefixes keep them in reading order)
- [ ] Confirm — Moodle shows all 5 files and a submission timestamp
- [ ] Screenshot the confirmation (fallback proof)

### G — Post-submission (T+0)

- [ ] Tag `v0.1.0-submitted` on the exact commit used to render the uploaded PDFs
- [ ] Push the tag: `git push origin v0.1.0-submitted`
- [ ] Save the Moodle confirmation screenshot under `vault/_files/Moodle/` (gitignored)
- [ ] Mark *PDF auf Moodle hochladen* `[x]` in `vault/Blöcke/05 Deployment/05 Deployment - c) Nachbereitung.md`

### Pre-flight quick check

```bash
just build && just test && just package-submission && \
  echo "Pre-flight OK — sign ./upload/04-*.pdf, then upload all 5 from ./upload/"
```

## Outputs are gitignored (or tracked)

`FlowHub_Uebersicht.pdf`, `SUBMISSION-bundle.pdf` and `Eigenständigkeitserklärung.pdf`
are **build artefacts** rendered on demand — gitignored, not committed. The
content PDFs `FlowHub_Arc42_v2.pdf`, `FlowHub_Reflexion.pdf` and
`flowhub-praesentation.pdf` **are committed** so their hub links resolve on
GitHub. Either way, regenerate them before uploading so they reflect the latest
sources.
