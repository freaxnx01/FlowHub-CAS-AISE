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

1. **Gather run metadata** (do this in the main session before launching):
   - `date` — today's date, `YYYY-MM-DD`.
   - `stamp` — a filesystem-safe stamp for the report filename, `YYYY-MM-DDTHHMM`
     (derive from the date + current time; if unknown, use the date alone).
   - `commit` — `git rev-parse --short HEAD`.
   - `demoUrl` — default `https://demo.flowhub.freaxnx01.ch` unless the user passes
     another URL as an argument.
   - A quick reachability probe of the demo (`curl -s -o /dev/null -w "%{http_code}"
     <demoUrl>/health/live`) is fine to confirm it is up; the demo agent will do the
     full round-trip itself.

2. **Launch the workflow**, passing the metadata as `args`:

   ```
   Workflow({ name: "examiner-sim", args: { date, stamp, commit, demoUrl } })
   ```

   The workflow itself: rebuilds `SUBMISSION.pdf` + `SUBMISSION-bundle.pdf`,
   extracts the real PDF text, runs five rubric-bucket examiners plus a live-demo
   examiner in parallel, applies an adversarial skeptic pass, then writes a dated
   grade sheet to `nachbereitung/examiner-sim/report-<stamp>.md`.

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
