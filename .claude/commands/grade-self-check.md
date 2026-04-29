Walk the canonical CAS Moodle Bewertungskriterien rubric end-to-end for the current (or specified) Block-Nachbereitung. Classify each scored item, surface gaps, and recommend the highest-leverage fixes — read-only.

Target Block (optional): $ARGUMENTS

## Steps

1. Determine the target Block:
   - If $ARGUMENTS is `3`, `4`, or `5`, use it.
   - Else compute today's Block + Phase from `.ai/cas-instructions.md` → Block Schedule. If today is not in a Nachbereitung window, ask the user before continuing.
2. Read `vault/Organisation/Bewertungskriterien.md` — the rubric (18 items, 5 buckets). The Quarkus/Jakarta-EE programming item is N/A for this .NET project — skip it from the score (max becomes 90).
3. Read the target block's `vault/Blöcke/<NN ...>/<NN ...> - c) Nachbereitung.md` for its current checklist state.
4. Per rubric item, gather evidence from the repo + vault (ADRs in `docs/adr/`, tests in `tests/`, `docs/ai-usage.md`, `docs/insights/`, `docs/test-strategy.md`, `Dockerfile`, `docker-compose.yml`, `.github/workflows/`, vault `Projektarbeit/`).
5. Classify each item on its ladder (e.g. `0 / 1 / 3 / 5` → `nicht / teilweise / überwiegend / vollständig`). Use the actual ladder per item — they differ.
6. Render a Markdown report grouped by bucket with columns: Item · Max · State · Estimated pts · Evidence · Gap. Add a summary line `Estimated current score: X / 90` and a prioritized list of 3–5 highest-leverage gaps with one-line actions.

## Rules

- Read-only — never edit files in this command.
- Never inflate scores. Thin evidence ⇒ classify low.
- Never invent files. If `docs/ai-usage.md` doesn't exist, say so.
- "Vollständig (max)" requires verifiable, complete, current artifacts.
- For Block 5 (final), be especially strict — the rubric assesses the submission, not WIP.
- Cite the rubric line for any classification.

See `.ai/skills/grade-self-check.md` for the full skill definition (evidence-source mapping, output template, classification rules).
