# TODO

- [ ] How to manual test the semantic search (postgres vectorized stuff)?
- [x] Test projects for Vikunja: Quotes, Movies, Ausflugziele — covered by `just test-services` (see `tests/FlowHub.Skills.IntegrationTests/VikunjaCatalogLiveTests.cs`)
- [ ] Test paperless-ngx (flowhub-test-services)

## Path to 100/100 — examiner-sim review 2026-06-27 (currently 96/100)

Source: `nachbereitung/examiner-sim/report-2026-06-27T1201.md`. All recoverable points
come from moving repo-only content into the rendered submission PDFs — no new engineering.

- [ ] **+3 pts — Interaction/interface diagram in Arc42** (Entwurf · Mehrere Perspektiven, 4→7).
  Add a dedicated interaction/interface-contract diagram (OpenAPI/contract view or a
  component-interface diagram) *inline in the Arc42 PDF*. Today the contract is prose-only in
  the rendered submission; the OpenAPI/Scalar surface is repo-only. **Highest-leverage gap.**
  - [ ] Diagram quality: when drawing blocks-and-lines, **label every arrow** (relationship /
    protocol / data direction) — unlabeled connectors are a recurring arc42 review deduction.
    Applies to *all* submission diagrams, not just the new interaction view.
- [ ] **+1 pt — Full use-case structure inline in Arc42** (Spezifikation · Use-Cases, 4→5).
  Render the full UC structure (Actor / Trigger / Precondition / Flow / Postcondition / Error /
  Akzeptanzkriterien) for ≥3–5 core functions *in the Arc42 PDF*, not only via the
  `docs/spec/use-cases.md` repo link.
- [ ] **Submission-blocking (0 pts, but protects the earned grade):**
  - [ ] Fix the stale convenience bundle: repair the `just pdf-submission-bundle` manifest path
    typo `vault/Blöcke/04 Persitence/04 Persistence - c) Nachbereitung.md`
    (`Persitence` → `Persistence`) so the 261-page bundle rebuilds (currently exit 1, stale).
  - [ ] Sign `04_Eigenstaendigkeitserklaerung.pdf` before uploading (rendered artifact is unsigned).

Total recoverable on the rendered artifacts: **+4 → 100/100**.
