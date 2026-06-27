# TODO

- [ ] How to manual test the semantic search (postgres vectorized stuff)?
- [x] Test projects for Vikunja: Quotes, Movies, Ausflugziele — covered by `just test-services` (see `tests/FlowHub.Skills.IntegrationTests/VikunjaCatalogLiveTests.cs`)
- [ ] Test paperless-ngx (flowhub-test-services)

## Path to 100/100 — examiner-sim review 2026-06-27 (currently 96/100)

Source: `nachbereitung/examiner-sim/report-2026-06-27T1201.md`. All recoverable points
come from moving repo-only content into the rendered submission PDFs — no new engineering.

- [x] **+3 pts — Interaction/interface diagram in Arc42** (Entwurf · Mehrere Perspektiven, 4→7).
  Added a dedicated Schnittstellen-/Interaktionssicht as new **Kap. 5.7 (Abbildung 4)** — a
  component-interface contract view distinct from the structure (Abb. 2) and behaviour/sequence
  (Kap. 6) diagrams, plus **Tabelle 4** detailing all 17 contracts (REST `/api/v1`, in-proc
  ports, MassTransit events, downstream HTTP). Renders inline in the Arc42 PDF (now 38 pp).
  - [x] Diagram quality: **every arrow labelled** (protocol + operation + direction, contract
    ids C1–C17 cross-referencing Tabelle 4). Diagram laid out vertically so labels render
    legibly at page width.
- [x] **+1 pt — Full use-case structure inline in Arc42** (Spezifikation · Use-Cases, 4→5).
  Added **Kap. 1.5 — Ausgewählte Anwendungsfälle (Detail)** with the full UC structure
  (Akteur / Auslöser / Vorbedingung / Ablauf / Nachbedingung / Fehler / Akzeptanzkriterien)
  for 5 core functions (UC-01, UC-08, UC-09, UC-10, UC-11), inline in the Arc42 PDF.
- [ ] **Submission-blocking (0 pts, but protects the earned grade):**
  - [x] Fixed the stale convenience bundle: the `tools/submission-bundle.sh` manifest pointed at
    `04 Persitence/04 Persistence - c) Nachbereitung.md`, but the real file is
    `04 Persitence - c) …` (the filename carries the same `Persitence` typo as its directory).
    Corrected the manifest to match the on-disk file; `just pdf-submission-bundle` now exits 0.
  - [ ] **(manual — for the operator)** Sign `upload/04_FlowHub_Eigenstaendigkeitserklaerung.pdf`
    before uploading (rendered artifact is unsigned by design).

Total recoverable on the rendered artifacts: **+4 → 100/100** — diagram + UC done; only the
manual signature remains before upload.
