# ADR 0010 — LLM & Agentic Threat Model: OWASP LLM Top 10 (2025) + Agentic Top 10 (2026) → FlowHub

- **Status:** Accepted
- **Date:** 2026-06-28
- **Block:** Block 3 (AI) / Block 5 (Deployment) — Querschnitt, Nachbereitung
- **Decider:** freax
- **Affects:** `source/FlowHub.AI/` (Classifier, Prompt), `source/FlowHub.Web/Pipeline/` (Routing, Fault-Observer), `source/FlowHub.Skills/` (Integrations, SSRF-Guard), `demo/docker-compose*.yml` (Traefik Rate-Limit), `source/FlowHub.Persistence/Repositories/` (Capture-Scope)

---

## Context

A defensive STRIDE pass already exists over the demo's **infrastructure** (Issues #100, #93: Traefik rate-limit, Wallabag-SSRF). FlowHub is, however, also an **LLM-routed, agentic** application: an AI classifier (`AiClassifier`, ADR 0004) reads attacker-controllable Capture content and decides which **real downstream action** runs — a task in Vikunja, a saved article in Wallabag, an uploaded document in Paperless. That makes the model an *actuator*, not just a text generator, so the codebase needs to be measured against the two OWASP GenAI lists that target this exact shape:

- **OWASP LLM Top 10 (2025)** — risks of a model that classifies / generates.
- **OWASP Top 10 for Agentic Applications (2026)** (prefix `ASI` = Agentic Security Initiative; v2026, published 2025-12-09) — risks that only arise once the model *plans and acts*.

This ADR is a **threat model + gap list**, not a feature. It records FlowHub's posture per category, grounded in the actual code (file:line), and carries the residual gaps forward as scoped follow-ups. It is consciously honest about controls that live *outside* the repo (operator/account-side) versus controls enforced *in* the repo.

> The issue (#121) proposed filing this as `0007`; that number was already taken by *LLM Hosting*, so it is filed as **0010**.

### System shape that bounds the agentic surface

FlowHub is a **single-agent, single-step** pipeline, not a multi-agent autonomy loop:

```
Capture → IClassifier.ClassifyAsync → ClassificationResult{MatchedSkill ∈ closed enum}
        → CaptureClassified (MassTransit) → SkillRoutingConsumer
        → exact-Name lookup of one ISkillIntegration → HandleAsync → SkillResult
        → lifecycle Completed | Unhandled
```

There is **no** agent-to-agent communication, **no** persistent agent memory feeding back into decisions, **no** runtime tool/MCP discovery, and **no** model-generated code execution. Several Agentic-Top-10 categories are therefore structurally N/A — documented as such below rather than stretched.

---

## Decision

The mappings below are the threat model. Each row carries a **posture**:
*Mitigated* (control enforced in repo) · *Operator* (control exists but lives account/deploy-side, not in repo) · *Documented* (accepted as-is, no code action) · *Gap* (residual risk, follow-up filed) · *N/A* (does not apply to FlowHub's shape).

### Table 1 — OWASP LLM Top 10 (2025) → FlowHub

| # | Category | FlowHub touchpoint | Posture | Evidence / note |
|---|----------|--------------------|---------|-----------------|
| **LLM01** | Prompt Injection | `AiClassifier` sees attacker-controllable Capture content | **Mitigated** | Output is constrained to a fixed skill enum and re-validated in code; injection cannot widen the action set. `source/FlowHub.AI/AiClassificationResponse.cs:13` (`[AllowedValues("Wallabag","Vikunja","")]`), `source/FlowHub.AI/AiClassifier.cs:63-66` (`Array.IndexOf(AllowedSkills, …) >= 0`). On any parse/validation failure → deterministic `KeywordClassifier` fallback (`AiClassifier.cs:88-95`). |
| **LLM02** | Sensitive Information Disclosure | OpenRouter sees Capture content verbatim | **Gap** | Demo uses anonymous user content; prod depends on operator. No outbound redaction guardrail. Follow-up G1. |
| **LLM03** | Supply Chain | Single provider (OpenRouter), pinned model | **Documented / Operator** | `KeywordClassifier` safety net on 429/error (provider-agnostic config `Ai:Provider` + keys). Provider rotation is a roadmap item (*Additional AI Providers*). |
| **LLM04** | Data & Model Poisoning | No fine-tuning, no embedding training | **N/A** | FlowHub consumes a hosted model and computes embeddings only for retrieval; no training pipeline exists. |
| **LLM05** | Improper Output Handling | Classifier output → routing decision | **Mitigated** | `SkillRoutingConsumer` does an **exact `Name` match** against registered `ISkillIntegration`; an unknown value falls through to `MarkUnhandledAsync` (Capture stays in Inbox). `source/FlowHub.Web/Pipeline/SkillRoutingConsumer.cs:38-48`. The raw classification is surfaced to the user as a transparency trace. |
| **LLM06** | Excessive Agency | Skills take real actions (Vikunja task, Wallabag fetch, Paperless upload) | **Mitigated** | Per-skill capability surface is narrow and HTTP-only — no shell/FS escape. Wallabag's user-URL fetch is SSRF-guarded (`source/FlowHub.Skills/Wallabag/WallabagSkillIntegration.cs:119-178`: rejects loopback, link-local, RFC1918, CGNAT, unspecified; fails closed on unresolvable host). Paperless is wired but **not exposed to the classifier** (not in `AllowedValues`, not in the prompt) — defense-in-depth. See agency table below. |
| **LLM07** | System Prompt Leakage | `AiClassifier` system prompt is in the repo | **Documented** | Prompt is open-source and contains **no secrets** (`source/FlowHub.AI/AiPrompts.cs:11-40`); only the closed skill set + runtime-interpolated Vikunja bucket names. Leakage has no security value. |
| **LLM08** | Vector & Embedding Weaknesses | pgvector embeddings (ADR 0006) | **Gap** | Embedding generation is best-effort and **off the request path** (`source/FlowHub.Web/Pipeline/CaptureEmbeddingConsumer.cs:30-43`) — non-blocking, so no DoS via embedding failure. **But** capture reads/searches are **not user-scoped** (`source/FlowHub.Persistence/Repositories/EfCaptureRepository.cs`; `source/FlowHub.Api/Endpoints/CaptureReadEndpoints.cs`). Acceptable in the current single-user/demo scope (`DevAuthHandler`); a hard prerequisite before multi-tenancy. Follow-up G2. |
| **LLM09** | Misinformation | Mislabel → wrong skill → visible to user | **Mitigated** | Bounded by the structured-output enum + the user-visible classification trace; worst case is `Unhandled`, not a silent wrong action. |
| **LLM10** | Unbounded Consumption | LLM cost + per-IP request volume | **Mitigated (in-repo) / Operator (cost)** | Traefik rate-limit on `flowhub.web` = **10 req/min/IP, burst 20** (`demo/docker-compose.yml`, `demo/docker-compose.vps.yml`). The OpenRouter **$1/month budget is an account-side cap, not enforced in the repo** — corrected from the issue draft. Follow-up G3 documents it as an operator runbook item. |

#### Agency boundary per skill (LLM06 / ASI02 detail)

| Skill | Classifier-reachable | Action | Transport / auth | Shell / FS |
|---|---|---|---|---|
| **Wallabag** | yes | save user-supplied URL (`POST /api/entries.json`) | HTTP + bearer, SSRF-guarded | none |
| **Vikunja** | yes | create task (`PUT /api/v1/projects/{id}/tasks`) | HTTP + bearer | none |
| **Paperless** | **no** (registered, hidden from classifier) | upload attachment (`POST /api/documents/post_document/`) | HTTP + token | reads via `IAttachmentStorage` only |

No skill chains into another, recurses, or runs more than one action per Capture. Each integration holds a static, per-instance token scoped to a demo backend.

### Table 2 — OWASP Top 10 for Agentic Applications (2026, `ASI`) → FlowHub

| # | Category | FlowHub relevance | Posture | Note |
|---|----------|-------------------|---------|------|
| **ASI01** | Agent Goal Hijack | Injection in Capture content could try to redirect the action | **Mitigated** | Agentic face of LLM01. The single-step, closed-enum output means a hijack cannot expand beyond {Wallabag, Vikunja, ∅} or chain steps. Same evidence as LLM01. |
| **ASI02** | Tool Misuse & Exploitation | Skills are the "tools" | **Mitigated** | No dynamic tool composition or recursion; exactly one allow-listed skill per Capture (agency table above). |
| **ASI03** | Identity & Privilege Abuse | Skills act with their own credentials | **Documented / Gap** | No end-user identity is delegated downstream — each skill uses a static, instance-scoped token (confused-deputy surface is small). Demo auth is `DevAuthHandler`; real OIDC + per-skill scope review lands in Block 5. Follow-up G4. |
| **ASI04** | Agentic Supply Chain | Provider + skill set | **Mitigated** | No runtime tool/MCP discovery — skills are **compile-time registered** (`SkillsServiceCollectionExtensions`); the model provider is pinned. Trust is decided at build time, not live. |
| **ASI05** | Unexpected Code Execution (RCE) | — | **N/A** | The model emits a classification enum; it neither generates nor executes code. |
| **ASI06** | Memory & Context Poisoning | pgvector store | **Mitigated / watch** | Embeddings are retrieval-only and do **not** feed back to steer classification — there is no persistent agent memory loop to poison. If embeddings ever inform routing, revisit with G2. |
| **ASI07** | Insecure Inter-Agent Communication | — | **N/A** | Single agent; no agent-to-agent protocol. (Internal MassTransit events are infra messaging, covered by ADR 0003, not agent comms.) |
| **ASI08** | Cascading Failures | Pipeline fault propagation | **Mitigated** | Linear single-step pipeline; MassTransit retry + terminal `Unhandled` state via `LifecycleFaultObserver` bound the blast radius — no autonomous chain to cascade. |
| **ASI09** | Human-Agent Trust Exploitation | User trusts the classification | **Mitigated / roadmap** | The user-visible classification trace is the transparency control; the planned Telegram "ask back with 2–3 options" on low confidence is the human-checkpoint (roadmap, not yet wired — stated honestly). |
| **ASI10** | Rogue Agents | — | **N/A** | No autonomous, goal-seeking, multi-step or multi-agent loop; deterministic classify-then-route. |

---

## Consequences

### Residual gaps (follow-ups to file as separate scoped issues)

- **G1 (LLM02):** Prod outbound-redaction guardrail before Capture content reaches a third-party LLM, plus an ADR on the data-flow/trust boundary. *Out of scope for the demo (anonymous content); design item for any real-data deployment.*
- **G2 (LLM08):** Per-user/tenant scoping on capture read + embedding-search repositories. **Hard prerequisite before FlowHub serves more than one real user.** Currently bounded by the single-user demo (`DevAuthHandler`).
- **G3 (LLM10):** Document the OpenRouter account-side budget cap as an operator runbook step — it is *not* enforced by repo code, only the Traefik request rate-limit is.
- **G4 (ASI03):** When Block-5 OIDC lands, review each skill's token scope and document the delegated-authority boundary.

### Rubric coverage (Bewertungskriterien)

- **KI / Sub-Systeme / Reflexion** — this ADR is the explicit security reflection for the AI-routed/agentic surface; it pairs the infra STRIDE work (#93, #100) with the model-layer and agent-layer threat models the rubric expects for an AI app.
- **Architektur / Entscheidungen dokumentiert** — recorded in the ADR series, cross-linked from #121, honest about in-repo vs. operator-side controls.

### Out of scope

- **Code changes** — this ADR produces only the audit. Mitigations G1–G4 become separate, small issues.
- **Embedding multi-tenancy** — a roadmap item that only matters if/when embeddings serve multiple real tenants.
- **The Quarkus/Jakarta-EE security stack** — N/A for FlowHub's .NET 10 realization (per the repo-wide stack-mismatch convention).

---

## References

- Issue #121 — OWASP LLM Top 10 (2025) + Agentic Top 10 (2026) mapping (this ADR is its deliverable)
- Issues #93 (Wallabag SSRF), #100 (Traefik rate-limit) — infrastructure STRIDE precedent
- ADR 0004 — AI Integration (provider abstraction, `KeywordClassifier` fallback contract)
- ADR 0006 — Vector Search (pgvector embeddings)
- ADR 0007 — LLM Hosting (outbound boundary)
- ADR 0009 — Telemetry & PII Policy (no prompt/body in traces)
- OWASP GenAI Security Project hub: https://genai.owasp.org/
- OWASP LLM Top 10 (2025): https://genai.owasp.org/llm-top-10/
- OWASP Top 10 for Agentic Applications (2026, v2026 / Dec 2025): https://genai.owasp.org/resource/owasp-top-10-for-agentic-applications-for-2026/
