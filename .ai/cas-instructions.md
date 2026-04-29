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

**Canonical source of truth:** [`vault/Organisation/Bewertungskriterien.md`](../vault/Organisation/Bewertungskriterien.md) — full Moodle rubric with all 18 scored items and their point ladders.

The rubric groups into five buckets. Each block's work contributes to the final grade, so the criteria are checked actively at the end of **every** Block-Nachbereitung — not just at final submission:

- **Spezifikation:** Key use cases & functional requirements named; non-functional requirements (NfA) specified SMART; solution vision described
- **Entwurf:** Architecture described textually and visually; design covered from structural, behavioral, and interaction perspectives; DB model specified
- **Programmierung:** Code readable, documented, structured by layer/module/subsystem; implementation insights documented; source code in a Git repository (the Quarkus / Jakarta-EE-specific item is N/A for the FlowHub .NET stack)
- **Validierung:** Acceptance criteria defined; testing approach and technologies specified; unit tests implemented; test results documented
- **KI, Sub-Systeme & Reflexion:** AI-tool usage described (max 12 pts — the highest-weighted single item); intelligent/flexible services built with AI; solution split into independently deployable sub-systems / containers; AI-tool experience reflected as a closing fazit

Scoring uses 0/1/3/5, 0/1/4/7, 0/3/7/10, 0/2/6, 0/1/7/12, or 0/2 ladders depending on weight. See the vault page for exact ladders per criterion. The grading rubric should actively guide what gets documented, built, and tested in every block.
