# Grade Self-Check — Skill Definition

Walk the canonical CAS Moodle rubric (`vault/Organisation/Bewertungskriterien.md`) end-to-end for the current Block (or one explicitly named), classify each scored item as `0 / 1 / mid / max` based on what's actually in the repo + vault, and surface gaps before claiming the Nachbereitung "done".

**Target Block:** `$ARGUMENTS` (e.g. `3`, `4`, `5`, or empty to detect from today's date)

---

## When to use

- End of any Block-Nachbereitung phase (see `.ai/cas-instructions.md` → Block Schedule).
- Before tagging a release / before final submission for Block 5.
- Whenever the user asks "did we cover the Bewertungskriterien?" or similar.

This skill is **read-only and analytical** — it does not modify code or create files. Its output is a gap report. The user (or a follow-up task) closes the gaps.

---

## Inputs to read

1. `vault/Organisation/Bewertungskriterien.md` — the rubric (source of truth).
2. `.ai/cas-instructions.md` → Block Schedule — to map today → current Block.
3. `vault/Blöcke/<NN ...>/<NN ...> - c) Nachbereitung.md` for the target block — the existing checklist + filled boxes.
4. The repo state for evidence (existing ADRs in `docs/adr/`, tests in `tests/`, `docs/ai-usage.md`, `docs/insights/`, `docs/test-strategy.md`, CHANGELOG, Dockerfile, GitHub workflows, etc.).

---

## Steps

### Step 1 — Determine target block
- If `$ARGUMENTS` is `3`, `4`, or `5`, use that.
- Otherwise compute today's Block + Phase from the schedule in `.ai/cas-instructions.md`. If today is **not** in a Nachbereitung window, prompt the user: "Today is in Block N <Phase>, not Nachbereitung. Continue self-check anyway? (specify block 3/4/5 or confirm)".

### Step 2 — Load rubric
- Read `vault/Organisation/Bewertungskriterien.md` in full.
- Extract the 18 scored items grouped by bucket. Note the Quarkus/Jakarta-EE item is consciously N/A and is reported as "skipped (stack mismatch)".

### Step 3 — Per item, gather evidence
For each rubric item, scan the repo + vault for concrete evidence:

| Bucket | Item | Where to look |
|---|---|---|
| Spezifikation | Use Cases | `vault/Projektarbeit/`, ADRs, Block Nachbereitung file |
| Spezifikation | NfA SMART | `vault/Projektarbeit/`, ADRs |
| Spezifikation | Solution Vision | `vault/Projektarbeit/Idee FlowHub.md`, `vault/Projektarbeit/Dev.md` |
| Entwurf | Architektur textuell + bildlich | `docs/adr/`, `docs/design/`, vault Projektarbeit |
| Entwurf | Struktur / Verhalten / Interaktion | ADRs, Mermaid diagrams, OpenAPI spec |
| Entwurf | DB-Modell | `docs/design/db/`, `source/FlowHub.Persistence/` (Block 4+) |
| Programmierung | Code lesbar/strukturiert | `source/`, casual code review |
| Programmierung | ~~Quarkus / Jakarta EE~~ | Skip (N/A) |
| Programmierung | Erkenntnisse dokumentiert | `docs/insights/block-N.md`, CHANGELOG |
| Programmierung | Source in Git | `git remote -v` |
| Validierung | Abnahmekriterien | `docs/acceptance-criteria.md` or per-ADR |
| Validierung | Test-Strategie | `docs/test-strategy.md` |
| Validierung | Unit-Tests | `tests/` projects, `dotnet test` |
| Validierung | Test-Ergebnisse dokumentiert | CHANGELOG, CI artifacts |
| KI/Sub/Refl | KI-Werkzeug-Nutzung beschrieben | `docs/ai-usage.md`, `docs/insights/` |
| KI/Sub/Refl | Intelligente Services mit KI | code that calls `Microsoft.Extensions.AI` / Semantic Kernel |
| KI/Sub/Refl | Sub-Systeme als Container | `Dockerfile`, `docker-compose.yml`, K8s manifests |
| KI/Sub/Refl | KI-Reflexion / Fazit | `docs/insights/`, vault Projektarbeit, submission PDF |

For each item, classify the current state on the rubric's ladder (e.g. `0/1/3/5` → `nicht / teilweise / überwiegend / vollständig`). Use the actual ladder from the vault page — the ladders differ per item.

### Step 4 — Render report

Output a Markdown table grouped by bucket. Columns:

| Item | Max pts | Current state (rubric step) | Estimated pts | Evidence | Gap |
|---|---:|---|---:|---|---|

Then a **summary line**: `Estimated current score: X / 90 (Quarkus item N/A) — Y items at 0 or 1 pt need attention.`

Then a **prioritized gap list**: which 3–5 items would gain the most points if addressed, with a one-line action each. Order by `(max_pts - estimated_pts)` desc.

### Step 5 — Honesty rules

- Never inflate. If evidence is thin, classify low.
- Never invent files. If `docs/ai-usage.md` doesn't exist, say so — don't pretend it does.
- "Vollständig (max)" requires verifiable, complete, current artifacts. "Mostly there with gaps" is `mid`, not `max`.
- For Block 5 (final), be especially strict — the rubric assesses the *submission*, not the working state.

---

## Output format example (abbreviated)

```
# Grade Self-Check — Block 3 (Service · Nachbereitung)
Today: 2026-05-08 (last day of Nachbereitung)

## Spezifikation (15 pts)
| Item | Max | State | Est. | Evidence | Gap |
|---|--:|---|--:|---|---|
| Use Cases & Anforderungen | 5 | teilweise | 1 | Capture/Skill mentioned in ADRs, no formal list | Create `docs/use-cases.md` with all use cases per module |
| NfA SMART | 5 | nicht | 0 | — | Define SMART NfAs (latency, throughput, retry, availability) |
| Solution Vision | 5 | überwiegend | 3 | `vault/Projektarbeit/Idee FlowHub.md` solid; misses Block 3 update | Add Block 3 services chapter to vision |

## (… other buckets …)

---
Estimated current score: 42 / 90 (Quarkus N/A)
6 items at 0 or 1 pt need attention.

## Top gaps (highest leverage)
1. **KI-Werkzeug-Nutzung beschrieben** (0 → potential 12 pts) — create `docs/ai-usage.md` with per-block AI usage, prompt strategies, generated/handwritten ratio.
2. **NfA SMART** (0 → potential 5 pts) — define 5–7 SMART NfAs in `vault/Projektarbeit/Dev.md` or new `docs/nfa.md`.
3. **Test-Strategie** (1 → potential 5 pts) — write `docs/test-strategy.md` with technologies + per-layer approach.
…
```

---

## Rules

- Read-only. Never edit files in this skill.
- Cite the vault rubric line for any classification. If the rubric and evidence disagree, trust the rubric.
- If a Block's `c) Nachbereitung.md` file doesn't exist, surface that and stop — the file is the precondition.
- The "Quarkus / Jakarta EE" item is reported as "N/A — stack mismatch" with 0 / 0 max, not contributing to the percent.
- Output language: same as the rubric (German item names; English commentary is fine).
