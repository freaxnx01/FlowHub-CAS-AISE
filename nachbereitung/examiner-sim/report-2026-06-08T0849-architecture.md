# FlowHub — CAS AISE Examiner Grade Sheet (focus = architecture)

**Run date:** 2026-06-08
**Commit:** e81a468
**Demo URL:** https://demo.flowhub.freaxnx01.ch
**Bundle:** SUBMISSION-bundle.pdf — 288 pages / 6,245,887 bytes (rebuilt this run, mtime 2026-06-08 08:49:14 CEST, renderer-stamped Skia/PDF HeadlessChrome 131)
**Submission PDF:** SUBMISSION.pdf — 5 pages / 163,954 bytes (rebuilt this run)
**Build freshness:** both `just pdf-submission` and `just pdf-submission-bundle` exited 0; `tools/submission-bundle.sh` ran under `set -euo pipefail` and exited 0 (every referenced artifact inlined — no broken refs). No build warnings. The PDF an examiner receives is intact.

---

## 1. Overall result

# FINAL SCORE: 72 / 90

**Grade band: Good (≈ 80% of achievable points) — a strong, defensible submission with one self-inflicted, cheap-to-fix architecture gap (behavioral/interaction diagrams excluded from the bundle).**

The Quarkus / Jakarta-EE programming criterion (max 10) is **consciously excluded** for FlowHub's .NET stack, so the achievable maximum is 90, not 100. This exclusion is declared in the ADRs and carried into the submission bundle, and is the correct, honest call.

**Examiner summary.** FlowHub is a genuinely engineered, AI-native modular monolith whose strongest evidence is first-hand: the live public demo classified an arXiv read-later URL to Wallabag and a "todo:" note to Vikunja within ~4 s, AI-enriched both titles, and degraded gracefully to `Unhandled` with an explicit `failureReason` rather than failing silently — the rubric's "intelligente und flexible Services" item is proven, not asserted. Specification, validation and AI/reflection are top-tier (closed UC-01..UC-18 catalog with per-UC acceptance criteria, a four-layer testing strategy with 223 green tests, and an auditable AI-usage reflection with concrete defect→fix→lesson stories). The submission is held back by an architecture documentation-vs-implementation gap: the behavioral and interaction diagrams (state machine, sequence diagrams) **exist in the repo but never reach the bundle PDF** (0 hits for `stateDiagram-v2`/`sequenceDiagram` in `bundle.txt`), an ADR set that drifts from the built system (a `flowhub.api` container and an Ollama-default LLM that are documented but not built), and a documented README/sub-system claim the tree contradicts. None of these are fatal, but together they cost ~13 points that are mostly recoverable with low-effort edits before the real submission.

---

## 2. Per-bucket result

| Bucket | First-pass | Final | Max |
|---|---:|---:|---:|
| Spezifikation | 13 | 11 | 15 |
| Entwurf | 11 | 8 | 17 |
| Programmierung (Quarkus item N/A excluded) | 12 | 9 | 12 |
| Validierung | 16 | 16 | 16 |
| KI, Sub-Systeme & Reflexion | 28 | 28 | 30 |
| **Total** | **80** | **72** | **90** |

---

## 3. Per-item detail

| Item | Scale | First-pass | Final | Justification (final) | Key evidence | Gap to next level |
|---|---|---:|---:|---|---|---|
| **Spez:** Use-Cases & fachliche Anforderungen benannt | 0/1/3/5 | 5 | 5 | Closed UC-01..UC-18 catalog, each with Actor/Trigger/Precondition/Flow/Postcondition/error paths AND per-UC acceptance criteria — exceeds mere "benennen". | bundle.txt L4019-4660 | At ceiling. Cosmetic: UC-17/18 renumbering note; add actor/UC matrix. |
| **Spez:** NfA nach SMART spezifiziert | 0/1/3/5 | 3 | **1** | Lowered toward skeptic. Requirements are measurable, but full SMART decomposition exists in only 3 of ~22 entries (~14%): "Achievable"/"Time-bound" each appear 3x in the whole bundle. Lists are titled "(SMART)" but the NF table has only ID/Requirement/Measurable-target/Verified-by. Partial (level 1), not "mehrheitlich". | bundle.txt L4426/4660 titles, L4678/4811/4842 the only 3 full blocks; NfA-05 has no measurable target | To 3: rewrite all NfA + NF table along all five SMART dimensions. |
| **Spez:** Vision der Lösung beschrieben | 0/1/3/5 | 5 | 5 | Dedicated Vision chapter + 3 personas + before/after (5+ steps vs 1). | bundle.txt L201-260, L4002-4009 | At ceiling. |
| **Entwurf:** Lösungsansatz & Architektur (bildlich+textuell) | 0/1/4/7 | 4 | 4 | Strong textually + several rendered diagrams (overview, C4 L1, hexagon, infra, deployment-context). Held at 4: C4 stops at L1, rendered C4/§6.1 are stale snapshots, Arc42 only linked not embedded; deep-dive adds doc-vs-code drift (flowhub.api, Ollama). | bundle.txt L632/L3810-3900/L3229-3290/L3940; Arc42 link L72 | To 7: embed Arc42 views, render C4 container/component, refresh stale diagrams. |
| **Entwurf:** Perspektiven (Struktur/Verhalten/Interaktion) | 0/1/4/7 | 4 | **1** | Lowered. Bundle-anchored grading: 0 `stateDiagram-v2`, 0 `sequenceDiagram`, 0 `participant` in bundle.txt. Struktur strong (ER/C4/hexagon render); Verhalten+Interaktion reach the examiner only as prose — canonical artifacts excluded by FILES[] in submission-bundle.sh. Dangling pointer + false ADR-0003 "state-machine diagram" claim. Two of three perspectives effectively missing in-PDF = level 1. | bundle grep 0 hits; submission-bundle.sh L24-68; perspectives.md L65-76/L108-148 exist but unbundled | To 4: add perspectives.md (+sequences/*) to FILES[] and rebuild — one-line fix. |
| **Entwurf:** DB-Modell spezifiziert | 0/1/2/3 | 3 | 3 | Full rendered ER (7 entities, PG types, PK/FK, cardinalities) + FK strategy + delete strategy + index + pgvector/HNSW; verified vs EF model. | bundle.txt L6150-6240/L6567-6629; ADR 0006 L818 | At ceiling. |
| **Prog:** Code lesbar, dokumentiert, nach Layer/Modulen/Sub-Systemen | 0/1/4/7 | 7 | **4** | Lowered. Layering is textbook (Core 0 refs; adapters→Core only; Web single root; per-module DI; warnings-as-errors). But top level unearned: no per-project README despite self-check claim; Integrations/Telegram .gitkeep-only yet drawn as real in C4; stale "empty project folders" C4 text; inline docs only 37/137. On 0/1/4/7 → 4. | find source -iname readme* empty; .gitkeep-only projects; bundle L3924-3928/L9591 | To 7: per-project READMEs, remove/flag empty projects, refresh C4, broaden docs. |
| **Prog:** Erkenntnisse dokumentiert | 0/1/2/3 | 3 | 3 | Section 9 + insights/block-1..5: 5 auditable defect→fix→lesson stories. | bundle.txt L982+/L1067-1099 | At ceiling. |
| **Prog:** Source-Code in Git | 0/2 | 2 | 2 | Public repo, remote confirmed, 137 .cs files. | bundle L4/L57; git remote | At ceiling. |
| **Valid:** Abnahmekriterien definiert | 0/1/3/5 | 5 | 5 | Standalone AC doc, AC-XX-N + "Verified by" artifact, 50/46/4; spot-checked artifacts are real files. | bundle L4857-4887/L5362-5440 | At ceiling. |
| **Valid:** Test-Strategie & Technologien | 0/1/3/5 | 5 | 5 | Four-layer pyramid, exact tech per layer + trait-gating + naming. | bundle L5548-5660 | At ceiling. |
| **Valid:** Unit-Tests programmiert | 0/1/3 | 3 | 3 | 9 projects / 105 test files; real unit inventory. | find tests; bundle L2988-2997 | At ceiling. |
| **Valid:** Test-Ergebnisse dokumentiert | 0/1/3 | 3 | 3 | Dated table: 223 pass/0 fail/6 skipped + per-project + smoke-prod. | bundle L10681-10745 | At ceiling. |
| **KI:** KI-Werkzeuge verwendet & beschrieben | 0/1/7/12 | 12 | 12 | Tool inventory + workflow-shift + ~95% generated tables + ai-usage.md (551 lines). | bundle L982-1059/L6799 | At ceiling. |
| **KI:** Intelligente & flexible Services | 0/2/6 | 6 | 6 | Live-confirmed: schema-validated IChatClient, allow-list re-validation, deterministic fallback (EventId 3010), provider-swap; URL→Wallabag, todo→Vikunja, titles enriched. | source AiClassifier.cs; live captures | At ceiling. |
| **KI:** Sub-Systeme unabhängig als Container | 0/1/3/5 | 3 | 3 | Live multi-container stack (web + migrations init + postgres/rabbitmq/prometheus/grafana) clears "überwiegend"; held below 5: Api folded into web (no flowhub.api), Telegram/Integrations .gitkeep. | docker-compose.yml; bundle L9640-9643; ADR0002 L1835 | To 5: ship one functional subsystem as its own container over the bus. |
| **KI:** Erfahrungen als Fazit reflektiert | 0/1/4/7 | 7 | 7 | Self-critical Fazit + hypothesis test, each grounded in a concrete incident. | bundle L1067-1184 | At ceiling. |

---

## 4. Live demo walkthrough

**Reachability.** `GET /` → 200, `GET /health/live` → 200 "Healthy" over HTTPS (HTTP/2). Real polished MudBlazor SPA: Dashboard (needs-attention counts, recent captures, Skill/Integration health panels for Wallabag/Vikunja/Paperless/Authentik) + Captures list with lifecycle/channel filters, search, pagination.

**Submitted & classified (first-hand):**

| Input | Capture ID | Routing | Title enrichment | Final stage |
|---|---|---|---|---|
| arXiv read-later URL (1706.03762) | db2012d8-d375-4b05-82d3-a6d995f01fee | Wallabag (correct) | "Attention Is All You Need Paper" | Unhandled (no integration — documented posture) |
| "todo: buy milk and call the dentist tomorrow" | 770baa21-adaa-4f45-ad20-6b28ae69f28d | Vikunja, project "Inbox" | "Buy milk and call dentist" | Unhandled |

Both advanced Raw→Unhandled within ~4 s. Two structurally different inputs → two distinct correct routing decisions + enriched titles — strong live support for "intelligente und flexible Services". Unhandled + explicit failureReason is the documented graceful-degradation posture, not a defect.

**Embeddings/search.** `GET /api/v1/captures/search?q=paper` → HTTP 503 with application/problem+json (RFC 9457). Transparent, env-var-gated — but vector search is NOT provable on the live demo.

**Rate-limit.** 20x 200 then 5x 429; `retry-after: 6`; short rolling window. Functional, demo-tuned.

**Reset.** Persistent in-band banner advertises the 15-min self-reset + disabled writes/search — matches observed behavior. Transparent.

**Screenshots:**
- nachbereitung/examiner-sim/screenshots/home-2026-06-08T0850.png
- nachbereitung/examiner-sim/screenshots/captures-list-2026-06-08T0850.png

**Issues:** (1) Inconsistent error contract — 503 is RFC 9457 but 429 is plain text, violating CLAUDE.md "ProblemDetails for all errors". (2) Fresh captures only reach Unhandled (writes disabled); completed/routed only via seed. (3) Semantic search 503 in demo profile — show it elsewhere. (4) Aggressive rate-limit window may surface spurious 429s if SignalR circuit isn't excluded. (5) Container independence not observable over HTTP — rests on compose files, which partly contradict the ADRs.

---

## 5b. ARCHITECTURE DEEP-DIVE (centerpiece)

### Lens 1 — ADR coherence & decision quality — ADEQUATE
**Strengths.** ADRs 0001-0005 complete Context→Decision→Alternatives→Consequences with real weighed alternatives; the hardest decisions are implemented (MassTransit pipeline, events, consumers, retry intervals matching ADR 0003 D5; InMemory/RabbitMq transport switch Program.cs:136); honest block/slice scoping with explicit deferrals.
**Weaknesses (drift).** flowhub.api container documented (ADR 0003 D9, bundle 2455/2713) but not built; ADR 0007 Ollama-default + Local provider asserted but absent (enum {Anthropic, OpenRouter}); env-var contradiction (0007 Embeddings__Provider vs 0004 Ai:Provider for the classifier); ADR 0002 Integrations superseded by Skills with no note; README index wrong dates + omits 0007-0009 (propagates into PVA bundle.txt:8414); ADRs 0007/0008/0009 reference non-existent audit tests while marked Accepted.
**Risks.** "Show me the flowhub.api container" / "Demo the local Ollama default" / date contradictions question record integrity.
**Recommendations.** Reconcile ADR 0003 D9; set 0007 to Proposed or implement; regenerate ADR README + rebuild; add supersession notes; create the audit tests or downgrade.

### Lens 2 — Documented vs actual code structure — STRONG
**Strengths.** Core genuinely pure (0 refs, no EF/Npgsql/MassTransit leakage); acyclic dependency direction; per-module DI real; persistence keeps separate *Entity types from domain; real driven-port adapters (Wallabag/Vikunja ISkillIntegration, AiClassifier:IClassifier); C4 honest about "future" maturity.
**Weaknesses.** Layer table misattributes Wallabag/Vikunja to FlowHub.Integrations (actually in Skills; Integrations is .gitkeep-only, not in slnx); Telegram framed as primary channel but has zero code (implemented channel is Web Quick-Capture).
**Risks.** "Where is FlowHub.Integrations?" / "Show a Telegram message becoming a Capture" / "Why are Wallabag/Vikunja in Skills not Integrations?"
**Recommendations.** Delete/flag empty projects; fix layer table to point at Skills + explain Skill-vs-Integration; pre-empt the Telegram gap (channel = adapter swap); add a Documented-vs-Implemented status table; restate CLAUDE.md tri-layer claim to the flat layout.

### Lens 3 — Behavioral & Interaction perspectives — WEAK (centerpiece deduction)
**Strengths.** perspectives.md has a true stateDiagram-v2 + autonumber sequenceDiagram; sequences/ adds 4 more with explicit failure-path variants (retry-exhaustion→LifecycleFaultObserver); render pipeline works (ER renders as SVG in bundle). Above the rubric bar — in the repo.
**Weaknesses (fatal for the bundle).** 0 stateDiagram-v2 / 0 sequenceDiagram / 0 participant in bundle.txt. Root cause: submission-bundle.sh FILES[] (L24-68) excludes perspectives.md/sequences/journeys/data-flow. Bundle points to "Sequence-Diagramme in docs/design/sequences/" (dangling pointer); ADR 0003 L206 falsely claims a "state-machine diagram in this ADR" (has none). Orphan/Unhandled terminal mapping transposed between ADR 0003 and perspectives.md.
**Risks.** Examiner searches PDF, finds no behavioral/interaction diagram, scores it missing despite the work existing — single highest-leverage, lowest-effort loss.
**Recommendations.** ONE-LINE FIX: add perspectives.md to FILES[], rebuild, re-grep. Add sequences/README + capture-enrichment + skill-routing. Fix ADR 0003 L206. Reconcile the Orphan/Unhandled transposition against LifecycleStage.cs + Web/Pipeline/.

### Lens 4 — Deployment topology & sub-system independence — WEAK
**Strengths.** Honest modular-monolith decision (ADR 0002 with cost + reversibility); runnable compose (web Alpine non-root + postgres/pgvector + rabbitmq + prometheus + grafana, healthchecks, ordered boot); genuine migrations init-container (12-Factor XII); env-driven transport; just smoke-prod exercises the deployed shape; layered override compose; live demo is a real deployed instance; self-aware about deployment-shape failures.
**Weaknesses.** Only one first-party app container + backing services + a migrations job — no independently built/pushed/scaled first-party subsystem. Page-56 sketches flowhub-api:dev that no compose contains (Api is a library). Bundle openly states the compose exists "to satisfy the Bewertungskriterien (max 5 pts)". Only flowhub-web pushed to registry; migrations is a CI artifact. No independent scalability (web+API+consumers share one process). Concept diagram advertises Redis/Ollama/Telegram/paperless/GitLab — none deployed. k3s-ready claim has no manifest.
**Risks.** "Show two of your own services as independent containers" / "Scale the consumers independently of the UI?" / "What gets pushed on release?"
**Recommendations.** Fix page-56 sketch to match compose; reframe the rubric defence around what exists + reversible split path; if a second container is wanted, make Api a real ASP.NET host with its own Dockerfile + compose service over the bus; push the migrations image; disclaim concept diagrams; commit a minimal k3s manifest or downgrade the claim.

### Lens 5 — NFR ↔ architecture alignment — ADEQUATE
**Strengths.** SMART NFRs pinned to concrete mechanisms; RFC 9457 error contract architected (AddProblemDetails, TypedResults.Problem, stable type URIs, ProducesProblem); AI resilience first-class (AiClassifier→KeywordClassifier hard floor, allow-list re-validation, EventId 3010; AiEmbeddingService mirrors); cost guards at composition root; 12-Factor config/secrets; ADR 0009 reasons about telemetry-PII at depth.
**Weaknesses.** NfA-P2 (AI Act Art.50) asserted but unbacked — ClassificationSource/ClassifiedAt/ConfidenceScore columns + migration + UI badge + named bUnit test do not exist (only on the in-flight event). NfA-P1 (residency) over-claims — no Embeddings:Provider key, no local adapter, embeddings default to api.mistral.ai (cloud, opposite of local-by-default). The two audit tests that are the measurable proof don't exist. Tracing dormant — metrics wired but no WithTracing/AddSource(MassTransit)/OTLP exporter, so the distributed-tracing + ADR 0009 PII apparatus + NfA-01 span-p95 have nothing behind them. /health/ready documented but only /health/live mapped (bare AddHealthChecks). CI security gate (dotnet list package --vulnerable) absent. AiClassifier drifted from ADR 0004 (Project+Entities, IVikunjaProjectCatalog, 5-field result) with no amendment.
**Risks.** "Run OutboundCallAuditTests green" / "Where's the AI-classified badge?" / "Default embedding path — where does text go?" (api.mistral.ai) / "Show a trace in Grafana" / "Demonstrate /health/ready failing".
**Recommendations.** Close NfA-P2 (persist 3 fields + migration + API + badge + test); implement or honestly downgrade NfA-P1; implement the two audit tests; wire tracing or downgrade the claims; map /health/ready with a DB check; add the --vulnerable CI gate; amend ADR 0004; tag each NFR documented/implemented/verified.

### Architecture verdict
FlowHub's architecture is well-decided and well-built, but unevenly documented for a bundle-anchored examiner. The hexagonal core is the real thing — a pure, infrastructure-free domain with correct acyclic dependencies and per-module DI — and the hardest decisions (async MassTransit pipeline, env-driven transport, AI classifier port + deterministic fallback) are implemented and proven live. The deductions are not about the built system; they are about the gap between the documents and that system: behavioral/interaction diagrams excluded from the PDF, ADRs describing a flowhub.api container and an Ollama-default LLM that were never built, a layer table pointing at empty placeholders, and SMART privacy NFRs whose backing fields/tests/local-embedding path do not exist. Decision quality is strong; decision-record coherence with the implementation is only adequate. Crucially, the single biggest architecture loss (Verhalten/Interaktion = 1/7) is a packaging defect, not a design defect — the artifacts are one FILES[] line from being graded.

### Prioritized architecture-improvement roadmap
1. (One line, ~5 min, +3 pts) Add docs/design/perspectives.md to submission-bundle.sh FILES[]; rebuild; re-grep bundle.txt. Recovers Entwurf Verhalten/Interaktion 1→4.
2. (Low effort, integrity) Add sequences/README + capture-enrichment + skill-routing; fix ADR 0003 L206; remove the dangling pointer.
3. (Low effort, +up to 3 pts) Per-project READMEs (or drop the claim); remove/flag .gitkeep-only Integrations/Telegram; refresh stale C4 text. Recovers Prog-structure 4→7.
4. (Medium, integrity) Reconcile ADRs with reality (ADR 0003 D9 + page-56; ADR 0007 Proposed-or-implement + env-var namespace; supersession notes; regenerate README dates + rebuild).
5. (Medium, +up to 2 pts + NFR credibility) Implement or downgrade NfA-P1/P2; create OutboundCallAuditTests/TracingPiiAuditTests or mark planned; wire tracing or downgrade; map /health/ready. Also lets NfA-SMART be rewritten to recover Spez 1→3.
6. (Optional, only path to the 5-pt ceiling) Ship FlowHub.Api (or Telegram) as a real independent container over the bus; push the migrations image to GHCR.

---

## 6. Top gaps ranked by point-leverage (cheapest first)

| # | Gap | Item | Recoverable | Effort | Architectural? |
|---|---|---|---:|---|---|
| 1 | Behavioral/interaction diagrams excluded from bundle (FILES[] one-line fix) | Entwurf/Perspektiven 1→4 | +3 | ~5 min | Yes (packaging) |
| 2 | Prog-structure capped by missing READMEs + .gitkeep "sub-systems" + stale C4 | Prog/Code structure 4→7 | +3 | Low | Yes |
| 3 | NfA not specified along all five SMART dimensions | Spez/NfA SMART 1→3 | +2 | Low-Medium | Partly |
| 4 | Architecture doc-vs-code drift caps Lösungsansatz at 4 | Entwurf/Lösungsansatz 4→7 | +up to 3 | Medium | Yes |
| 5 | No independent first-party container (Api is a folded library) | KI/Sub-Systeme 3→5 | +2 | Medium-High | Yes |
| 6 | Privacy NFRs unbacked; audit tests + tracing + /health/ready missing | cross-cutting credibility | (few direct pts) | Medium | Yes |

Cheapest 8 points (gaps 1+2+3) are all low-effort and largely architectural/packaging. The behavioral-diagram fix is the single best ROI.

---

## 7. Defense questions

**Spezifikation**
1. Both quality lists are titled "(SMART)" but only 3 entries decompose all five dimensions — walk me through NfA-05 as SMART.
2. NF-01..NF-13 table vs NfA-01..P2 list — which is canonical, how do they map?

**Entwurf**
3. Open the PDF and show me your Capture state machine and the Submit→Classify→Route sequence diagram.
4. Your C4 says "Block 2 / empty project folders" for projects that now have code — does the diagram describe the built system?
5. Your layer table says the Wallabag adapter lives in FlowHub.Integrations — open that project.

**Programmierung**
6. Your self-check lists "README pro Hauptprojekt" — show me the README under source/FlowHub.Persistence.
7. Why draw .gitkeep-only Telegram/Integrations as realized C4 components?

**Validierung**
8. NfA-P1's measurable #2 is OutboundCallAuditTests — run it green.
9. Your results table has a Coverage column but no percentage — what is your coverage?

**KI/Sub-Systeme/Reflexion**
10. Submit a fresh capture in the demo and drive it to completed.
11. Your 503 is RFC 9457 but your 429 is plain text — CLAUDE.md says ProblemDetails for all errors; why?

**Architecture-defense block**
12. The criterion rewards decomposition but ADR 0002 argues against splitting — show two of your own services as independent containers, or justify why one app container satisfies it.
13. ADR 0003 D9 + bundle p.56 show a flowhub.api container — show it in docker-compose.yml.
14. ADR 0007 says default LLM is local Ollama (NfA-P1) but the enum is {Anthropic, OpenRouter} and embeddings default to api.mistral.ai — where does the text actually go?
15. ADR 0003 and perspectives.md transpose Orphan/Unhandled — which matches LifecycleStage.cs and the consumers?
16. Show me a distributed trace of a Capture in Grafana (no WithTracing/OTLP is wired).

---

## 8. Skeptic disputes & resolution

| Item | First | Skeptic | Final | Resolution |
|---|---:|---:|---:|---|
| Spez — NfA SMART | 3 | 1 | 1 | Upheld skeptic — full SMART in ~14% is a minority, not "mehrheitlich". |
| Spez — Use-Cases | 5 | 5 | 5 | No dispute. |
| Spez — Vision | 5 | 5 | 5 | No dispute. |
| Entwurf — Lösungsansatz | 4 | 4 | 4 | Upheld; drift dents "korrekt" but no rung below 4. |
| Entwurf — Perspektiven | 4 | 4 | 1 | Behavior deep-dive overrides: 0 behavioral/interaction diagrams in bundle → level 1. |
| Entwurf — DB-Modell | 3 | 3 | 3 | No dispute. |
| Prog — Code structure | 7 | 4 | 4 | Upheld skeptic — self-refuting README + .gitkeep sub-systems + stale C4. |
| Prog — Insights / Git | 3 / 2 | 3 / 2 | 3 / 2 | No dispute. |
| Validierung (all 4) | 16 | 16 | 16 | No dispute; soft spots immaterial. |
| KI — Tools/Services/Reflection | 12/6/7 | 12/6/7 | 12/6/7 | No dispute; live demo strengthens Services. |
| KI — Sub-Systeme container | 3 | 3 | 3 | No dispute; correctly held (cannot reach 5, cannot drop to 1). |

---

*Generated by examiner-sim on 2026-06-08 08:49 against commit e81a468. Max achievable = 90 (Quarkus/Jakarta-EE item, max 10, consciously excluded for the .NET stack).*
