---
description: Simulate the CAS-AISE examiner — rebuild the real submission PDFs, grade them against the Moodle rubric with a multi-agent panel, exercise the live demo, and emit a dated grade sheet.
---

# /examiner-sim — Examiner Simulation

Run a realistic dry-run of the CAS-AISE examination against the **real** submission
artifacts and the **live** public demo. Repeatable: every run rebuilds the
submission PDFs first, then grades *those*.

## What it does

This command launches the saved **`examiner-sim` workflow** (a multi-agent panel).
You are explicitly authorized to call the **Workflow** tool for this command.

Steps to perform:

1. **Parse the argument** (everything after `/examiner-sim`):
   - `focus` — `architecture` if the argument contains "arch" (any casing), else
     `balanced` (the default). Architecture mode adds a deep architecture-lens
     phase (ADRs, structure-vs-code fidelity, behavior/interaction views,
     deployment topology, NFR alignment) and makes the design examiners stricter.
   - `demoUrl` — any `http(s)://…` token in the argument overrides the default
     `https://demo.flowhub.freaxnx01.ch`.
   - Examples: `/examiner-sim` · `/examiner-sim architecture` ·
     `/examiner-sim arch https://staging.example`.

2. **Gather run metadata** (optional — the workflow derives these itself in-run):
   `commit` (`git rev-parse --short HEAD`), today's `date`, a `stamp`
   (`YYYY-MM-DDTHHMM`). A quick `curl …/health/live` probe confirms the demo is up.

3. **Launch the workflow**, passing `focus` (and `demoUrl` if overridden) as `args`:

   ```
   Workflow({ name: "examiner-sim", args: { focus, demoUrl } })
   ```

   IMPORTANT — args propagation: if the launched run reports back `focus` that does
   NOT match what was requested (the top-level launcher dropped `args`), relaunch
   through the in-process wrapper instead, which passes args reliably:
   write a one-off script that calls
   `workflow({ scriptPath: ".claude/workflows/examiner-sim.js" }, { focus, demoUrl })`
   and run that. (See `tools/build/examiner-sim-arch-run.js` for the pattern.)

   The workflow itself: rebuilds `SUBMISSION.pdf` + `SUBMISSION-bundle.pdf`,
   extracts the real PDF text, runs five rubric-bucket examiners plus a live-demo
   examiner in parallel (plus the architecture lenses when `focus=architecture`),
   applies an adversarial skeptic pass, then writes a dated grade sheet to
   `nachbereitung/examiner-sim/report-<stamp>[-architecture].md`.

3. **When the workflow completes**, surface to the user:
   - The final score `X / 90` and grade band.
   - The per-bucket breakdown.
   - The top point-leverage gaps.
   - The path to the full report and any screenshots under
     `nachbereitung/examiner-sim/`.

## Notes

- Max achievable is **90** — the Quarkus/Jakarta-EE rubric item (max 10) is
  excluded for FlowHub's .NET stack by design.
- The report is a *prediction/dry-run*, not the official Moodle grade. Treat the
  skeptic-adjusted "final" column as the conservative estimate.
- Argument (optional): an alternate demo URL, e.g. `/examiner-sim https://staging.example`.
- Re-run any time. Reports are timestamped, so successive runs accumulate under
  `nachbereitung/examiner-sim/` for trend comparison.
