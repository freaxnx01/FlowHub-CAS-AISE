# Lernziele Coverage — FlowHub vs. CAS AISE learning objectives

Companion to the Bewertungskriterien self-check (which scores the graded rubric).
This document takes the **other axis**: for each Block's *Lernziele* (learning
objectives, Vorbereitung + Nachbereitung), it maps the objective to concrete
**code** and **submission-document** evidence, so the roter Faden from "what the
CAS taught" → "what FlowHub demonstrates" is explicit.

> Objectives are referenced by **paraphrased topic** (the verbatim Moodle
> Lernziele are FFHS course material and are not reproduced here). FlowHub runs
> on **.NET 10**; where a Lernziel names a JVM tool, the concept is met via the
> .NET equivalent — see [Stack mapping](#stack-mapping--consciously-deferred-scope).

Legend: ✅ covered · ⚠️ partial / framing here · ➖ deferred or N/A (see notes).

## Block 1 — Einführung

| Objective (paraphrased) | Code | Submission doc | Status |
|---|---|---|---|
| AI models & generative-AI basics; prompt engineering | LLM-driven workflow throughout | `docs/ai-usage.md`, `docs/insights/block-1.md` (prompt hygiene), this doc ↓ | ✅ |
| AI tooling in the IDE for automated tasks | `CLAUDE.md`, `.ai/` instructions + skills | `docs/ai-usage.md`, `docs/insights/block-1.md` | ✅ |
| Design & communicate a distributed web-app architecture | ADR 0001, ADR 0002; `source/` layout | `docs/adr/`, `docs/projektbeschreibung/`, Arc42 PDF | ✅ |
| Create an app skeleton with AI | `FlowHub.slnx`, `FlowHub.Core`, build props | `docs/insights/block-1.md` (AI-gen ratio) | ✅ |
| Propose a suitable group project | — | — | ➖ solo project (no group) — noted in submission |

**AI-models framing (closes the taxonomy thin-spot):** FlowHub deliberately uses
**generative LLMs** only — chat models for classification/enrichment
(Anthropic Claude natively; OpenRouter as a multi-model aggregator) and a
separate **embedding model** (Mistral) for semantic search. Discriminative/
classical ML and diffusion models were out of scope; the keyword classifier is a
deterministic non-ML fallback. Provider choice and trade-offs: ADR 0004, ADR 0007.

## Block 2 — Frontend

| Objective (paraphrased) | Code | Submission doc | Status |
|---|---|---|---|
| Use web technologies/frameworks; modern-frontend challenges | Blazor SSR + MudBlazor, `source/FlowHub.Web/Components/` | ADR 0001 (render-mode trade-offs), `docs/insights/block-2.md` | ✅ |
| Explain & apply SSR vs CSR | Interactive Server render mode | ADR 0001 §Decision 1 (SSR/Static-SSR/WASM compared) | ✅ |
| Use AI agents to automate workflows | `.ai/skills/ui-*` (4-phase UI workflow), FlowHub skills | `docs/insights/block-2.md`, `docs/ai-usage.md` | ✅ |
| Generate unit tests; test services | `tests/FlowHub.Web.ComponentTests/` (bUnit) | `docs/insights/block-2.md` (~85% AI-gen tests) | ✅ |
| Laws of software architecture | Hexagonal + Modular Monolith in `source/` | `CLAUDE.md` §Clean Code, `docs/spec/modern-app-concepts.md`, this doc ↓ | ⚠️ |
| Low-/No-Code approaches | Bogus stubs, skill-driven scaffolding | `docs/insights/block-2.md` | ⚠️ framing ↓ |
| Double productivity via AI | metrics in insights | `docs/insights/block-2.md` (AI-gen ratios) | ⚠️ |

**Architecture-laws framing:** FlowHub applies SOLID (ports/adapters per module),
DRY + "duplication over wrong abstraction", Command-Query Separation and
guard-clause style (all in `CLAUDE.md` §Clean Code Principles) and Clean/Hexagonal
Architecture (ADR 0001, ADR 0002). **Low-/No-Code:** FlowHub's interpretation is
AI-assisted code generation (skills generating Razor/tests) plus declarative
config (Compose, Traefik labels) rather than visual builders — a conscious
code-first choice. **Productivity:** insights record ~80% AI-generated Razor and
~85% AI-generated tests within the block's hour budget.

## Block 3 — Service

| Objective (paraphrased) | Code | Submission doc | Status |
|---|---|---|---|
| Design microservice / service-based architectures | Modular Monolith w/ logical boundaries, ports/adapters | ADR 0002 | ✅ |
| Read/judge/extend generated code | Scalar OpenAPI, source-gen loggers, gRPC POC stubs | `docs/ai-usage.md` (generated-vs-handwritten + issues) | ✅ |
| Specify machine-readable API contracts for AI | JSON-schema structured output via MEAI `CompleteAsync<T>` | ADR 0004 §Decision 7 | ✅ |
| Async & event-based architecture for resilience | MassTransit pipeline, `CaptureCreated/Classified` events, retries, fault observer | ADR 0003, `docs/spec/use-cases.md` (UC-08..11) | ✅ |
| Build flexible AI services | `AiClassifier` (2 adapters) + `KeywordClassifier` fallback | ADR 0004 §2/§5/§8 | ✅ |
| Protocols: REST / gRPC / SOAP | REST live (Minimal API + `/scalar`); gRPC POC (`poc/restful-api-playground/`); SOAP — none | ADR 0002 §Decision 5 + ↓ | ⚠️ |
| Service-Discovery / Service-Mesh | — | ↓ deferred | ➖ |
| Agentic AI multi-step workflows | single-shot classifier; SK reserved | ADR 0004 §Decision 3 + ↓ | ➖ |

## Block 4 — Persistence

| Objective (paraphrased) | Code | Submission doc | Status |
|---|---|---|---|
| Choose the right persistence form | PostgreSQL + EF Core + pgvector | ADR 0005, `docs/design/db/er.md` | ✅ |
| RDBMS vs NoSQL concepts | relational schema | ADR 0005, this doc ↓ | ⚠️ |
| Benefits of AI/vector databases | pgvector, HNSW, `vector(1024)` column | ADR 0006 | ✅ |
| Abstract DB access via ORM (concept; JPA → EF Core) | EF Core repositories `source/FlowHub.Persistence/Repositories/` | ADR 0005 | ✅ ➖tool |
| Analyse data with AI | `AiEmbeddingService`, embedding pipeline | ADR 0006, `docs/insights/block-4.md` | ✅ |
| Design a fitting data model | 7-entity ER, indexes, delete strategy | `docs/design/db/er.md`, ADR 0005 | ✅ |
| Program dynamic queries efficiently | `CaptureQueryBuilder` (expression trees), cursor pagination | ADR 0005 §6, integration tests | ✅ |

**RDBMS-vs-NoSQL framing:** FlowHub chose a **relational RDBMS (PostgreSQL)** —
the data is highly relational (Capture ↔ Tags ↔ SkillRuns ↔ Integrations) and
benefits from constraints, transactions and ad-hoc queries. The one "NoSQL-shaped"
need — vector similarity — is served **inside** Postgres via the `pgvector`
extension rather than a separate vector store, avoiding a second datastore
(ADR 0006). A document/KV store offered no advantage for this workload.

## Block 5 — Deployment

| Objective (paraphrased) | Code | Submission doc | Status |
|---|---|---|---|
| Containerise & run (Docker / Kubernetes) | multi-stage `Dockerfile`, `docker-compose*.yml`, migrations init container | `docs/ci-cd.md`, `docs/runbooks/public-demo.md` | ✅ Docker · ➖ K8s ↓ |
| GitHub/Copilot CI/CD + deployment automation | `.github/workflows/{ci,release,migrations}.yml` | `docs/ci-cd.md` (incl. GitHub-vs-GitLab + deployment-scope) | ✅ |
| Monitoring & observability; observe + optimise | OTel→Prometheus→Grafana, `/health/live`, `/metrics`, Uptime Kuma (`demo/`) | `docs/ci-cd.md`, `docs/runbooks/public-demo.md`, `SUBMISSION.md` §4 | ✅ |
| Name limits & possibilities of AI-assisted SWE | — | `docs/ai-usage.md`, `vault/Projektarbeit/Learnings.md` | ✅ |
| DevOps / DevSecOps / NoOps concepts | 12-Factor, PII guardrails (ADR 0008/0009) | `CLAUDE.md` §12-Factor, ADR 0008/0009, this doc ↓ | ⚠️ |
| Design cloud-operation solutions | VPS + Compose; cloud LLM providers | ADR 0007, ↓ | ➖ |

**DevOps/DevSecOps/NoOps framing:** FlowHub practises **DevOps** (CI/CD via GitHub
Actions, IaC via Compose, one-command deploy), **DevSecOps** (PII-safe logging
ADR 0008 + telemetry-PII policy ADR 0009, non-root containers, secrets via env,
`dotnet list package --vulnerable` gate) and a **NoOps-leaning** demo
(self-resetting, self-healing `restart: unless-stopped`, KeywordClassifier
fallback). Full managed-platform NoOps (serverless) is out of scope for a
single-operator VPS.

## Stack mapping & consciously-deferred scope

These objectives name JVM tools or describe capability beyond the CAS scope.
Each was evaluated; the decision is recorded so it reads as a choice, not a miss.

| Item | Decision | Rationale / where |
|---|---|---|
| Quarkus (Frontend/Persistence/AI) | replaced by **.NET 10** | Blazor SSR, EF Core, Microsoft.Extensions.AI — block Nachbereitungen' stack-mapping notes |
| JPA (ORM spec) | **EF Core** | same ORM-abstraction concept; ADR 0005 |
| Spring-AI / Koog (agents) | **Microsoft.Extensions.AI** / (Semantic Kernel reserved) | ADR 0004 |
| **SOAP** | **evaluated, not used** | REST + async messaging cover all interfaces; no SOAP consumer exists. (gRPC likewise rejected for an in-process modular monolith — ADR 0002 §Decision 5; a gRPC POC exists under `poc/`.) |
| **Service-Discovery / Service-Mesh** | **deferred (post-CAS)** | single-host Compose needs no discovery/mesh; relevant only with the deferred Kubernetes step. ADR 0002, Block 5 Nachbereitung |
| **Kubernetes** | **deferred (post-CAS)** | Docker Compose satisfies the Block 5 deployment scope; K8s migration is roadmap. Block 5 Nachbereitung (documented out-of-scope) |
| **Agentic AI (multi-step)** | **deferred** | Slice C ships a single-shot classifier; Semantic Kernel is reserved for a future agent loop. ADR 0004 §Decision 3 |
| **Cloud IaaS design (AWS/Azure/GCP)** | **out of scope** | runs on a self-hosted VPS (Compose); "cloud" is addressed at the LLM-provider level (Anthropic/OpenRouter, ADR 0007) |
| **Group project proposal** (Block 1) | **N/A** | solo project |

## How this was produced

Generated with the `cas-aise-lernziele-check` skill (code + submission-doc
evidence sweep per Block). Re-run it after major changes to keep this current.
