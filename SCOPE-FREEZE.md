# SCOPE FREEZE — CAS-AISE Projektarbeit

**Status:** 🔒 FROZEN for the CAS-AISE submission
**Frozen at:** commit `e7691a6` (`main`), 2026-06-05
**Submission deadline:** **2026-07-04 24:00** (Moodle upload of `SUBMISSION-bundle.pdf`)
**Last Block PVA still ahead:** Block 5 — Deployment, **2026-06-20**

> **Post-submission update (2026-06-08):** This freeze governs the **`v0.1.0`** submission stand (tag
> `v0.1.0`), which remains the frozen, graded artifact. Product/demo work has since resumed on `main`
> (now ahead of the tag) — citation enrichment, public-demo enhancements (example chips, auto-refresh,
> banner "Source" link), and dormant ntfy notifications. These are **post-submission product work**, not
> part of the graded scope. The "out of scope / backlog" lists below describe the v0.1.0 scope; what's
> shipped since is tracked in `FEATURES.md` (🆕 markers).

---

## Why this file exists

FlowHub has two futures that must not be confused before 2026-07-04:

1. **CAS-AISE Projektarbeit** — a *graded deliverable* due 2026-07-04. It is scored on a fixed
   18-item Moodle rubric (`vault/Organisation/Bewertungskriterien.md`), **not** on feature count.
   Per the project's own tooling it is already submission-ready (grade self-check 90/90,
   preflight ✅ READY, build clean, offline tests green).
2. **FlowHub the product** — the personal/productive tool I will keep building *after* submission,
   with many more features.

**The risk is gold-plating #2 and missing the deadline for #1.** This file is the line between them.
When tempted to build something before 2026-07-04, apply the freeze rule below.

## The freeze rule (apply before writing any new feature code)

> **Does this change raise the CAS-AISE rubric score before 2026-07-04 24:00?**
> - **No** → it is product work. Add it to the backlog (below). Do not build it now.
> - **Yes** → it is a rubric gap, not a feature. Allowed — record which rubric item it closes.

Two — and only two — things justify breaking the freeze:
- A **defect** that breaks a claim the submission makes (a documented feature doesn't actually work).
- A **new requirement** that surfaces at the **Block 5 PVA (2026-06-20)** and must land in the
  Block 5 Nachbereitung.

Anything else waits until after upload.

---

## ✅ IN SCOPE — frozen, done, do not extend

The deliverable is `SUBMISSION.md` → `SUBMISSION-bundle.pdf` + the GitHub repo. Its claimed capability
set is frozen as-is:

- **Capture pipeline** — Quick-capture field, REST API (`POST /api/v1/captures`), file upload
  (2 MB demo limit, paperless-ngx *prep* only).
- **Classification & routing** — keyword + URL-pattern + LLM fallback (MEAI; Anthropic + OpenRouter
  adapters), skill routing to Vikunja + Wallabag.
- **Async pipeline** — RabbitMQ consumers (classify, enrich-label, embed).
- **Persistence** — Postgres + EF Core, pgvector embeddings + semantic search.
- **Frontend** — Blazor (MudBlazor, Interactive Server): Dashboard, Captures list/detail, New Capture.
- **Ops** — `/health/*`, `/metrics`, OpenAPI + Scalar, full docker-compose stack (web + postgres +
  rabbitmq + prometheus + grafana + migrations container).
- **Docs/rubric artefacts** — 6 ADRs, spec (use-cases, NfA SMART, acceptance criteria, testing
  strategy, DB model), 5 Block-Nachbereitungen, per-block insights, AI-usage, Learnings,
  Eigenständigkeitserklärung.

Maintenance allowed (does not break the freeze): bug fixes to the above, doc polish, test additions,
the finish-line checklist.

## 🚫 OUT OF SCOPE — deferred to the FlowHub product backlog

These are **product features**, not rubric gaps. They wait until after 2026-07-04. Source: `docs/project/ROADMAP.md`
+ `docs/project/TODO.md`.

- **Capture Enrichment** (post-classification data fetch; `IEnricher`, `CaptureEnriched` event,
  enrichment table) — `docs/project/ROADMAP.md`.
- **Web-search tooling** (Brave/Tavily `AIFunction`, provider-hosted search, `:online` variants) —
  `docs/project/ROADMAP.md`.
- **Additional AI providers** (Gemma, Apertus/Swiss-sovereign, Hugging Face router; `BaseUrl`
  refactor) — `docs/project/ROADMAP.md`.
- **paperless-ngx integration** beyond the upload *prep* already shipped.
- **Open manual-test TODOs** that aren't required for a submission claim — `docs/project/TODO.md`
  (semantic-search manual walkthrough, paperless-ngx test).

> Building any of the above before submission = breaking the freeze. Don't.

---

## Finish-line checklist (the entire remaining CAS-AISE work)

- [ ] **2026-06-20** — attend Block 5 PVA. Fold any new requirement into
      `vault/Blöcke/05 Deployment/05 Deployment - c) Nachbereitung.md` (and only that).
- [ ] **Re-tag the submission stand** — `v0.1.0` currently points at `6d207dc`, which is **31 commits
      behind `main`** (the file-upload feature #20 + ADRs 0007–0009 + NfA-P1/P2 landed after).
      Move/recut the tag onto the final frozen commit so the tag matches what `SUBMISSION.md` links to.
- [ ] **Rebuild `SUBMISSION-bundle.pdf`** after any content change — `just pdf-submission-bundle`.
- [ ] **Final verification** — `just smoke-prod` (deployment claim) + offline tests + re-run the
      `cas-aise-submission-preflight` skill. Target: ✅ READY.
- [ ] **Upload to Moodle before 2026-07-04 24:00** — `SUBMISSION-bundle.pdf`, submission comment with
      repo URL + commit + tag. Confirm Moodle status = *Abgegeben*.

---

## After submission

Lift the freeze. Promote backlog items to ADR + implementation plan as usual. This file can be deleted
or rewritten as the product roadmap entry point.
