# CAS AISE — Course Structure & Rhythm

Note: All school materials are in German; AI instructions and prompts are in English.

The course is structured into **5 blocks**, each following the same rhythm: ~2 weeks of *Vorbereitung* (preparation / self-study), followed by an in-person Saturday session (*Präsenzveranstaltung*, PVA), followed by ~2 weeks of *Nachbereitung* (follow-up / implementation).

## Block Schedule (all Saturdays)

| # | Block | PVA Date | Prep (h) | Follow-up (h) |
|---|-------|----------|----------|---------------|
| 1 | Einführung | 21.02.2026 | 24 | 28 |
| 2 | Frontend | 21.03.2026 | 26 | 26 |
| 3 | Services | 25.04.2026 | 30 | 22 |
| 4 | Persistence | 23.05.2026 | 30 | 22 |
| 5 | Deployment | 20.06.2026 | 30 | 22 |

## Implementation Rhythm

FlowHub is implemented incrementally, block by block, aligned to this schedule. Each block's Nachbereitung phase is used to implement the corresponding layer of the application (frontend in block 2, services/API in block 3, persistence in block 4, deployment in block 5).

## Grading (Bewertungskriterien)

The final project submission is graded across five dimensions — Spezifikation, Entwurf, Programmierung, Validierung, and Präsentation. These criteria must be kept in mind throughout every implementation phase, not just at the end, since each block's work contributes to the final grade:

- **Spezifikation:** Key use cases and functional requirements named; non-functional requirements (NfA) specified SMART; solution vision described
- **Entwurf:** Architecture described textually and visually; design covered from structural, behavioral, and interaction perspectives; DB model specified
- **Programmierung:** Code readable, documented, structured by layer/module/subsystem; modern application concepts applied; implementation insights documented; source code in a Git repository
- **Validierung:** Acceptance criteria defined; testing approach and technologies specified; unit tests implemented; test results documented
- **Präsentation:** Clarity and quality of the final project presentation

Scoring uses a 0/1/3/5 (or 0/1/4/7 and 0/3/7/10 for higher-weight items) scale per criterion. The grading rubric should actively guide what gets documented and tested in every block.
