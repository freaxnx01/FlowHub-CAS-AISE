# Architecture Decision Records

Index of all ADRs for the FlowHub project.

| ADR | Title | Status | Date |
|-----|-------|--------|------|
| [0001](0001-frontend-render-mode-and-architecture.md) | Frontend Render Mode and Architecture | Accepted | 2026-02-15 |
| [0002](0002-service-architecture-and-async-communication.md) | Service Architecture and Async Communication | Accepted | 2026-03-01 |
| [0003](0003-async-pipeline.md) | Async Pipeline (MassTransit) | Accepted | 2026-04-10 |
| [0004](0004-ai-integration-in-services.md) | AI Integration — Provider Abstraction | Accepted | 2026-04-15 |
| [0005](0005-persistence.md) | Persistence — EF Core + PostgreSQL | Accepted | 2026-05-01 |
| [0006](0006-vector-search.md) | Vector Search — pgvector + Mistral Embeddings | Accepted | 2026-05-07 |

## Why ADRs

FlowHub is a single-developer CAS thesis project that nonetheless commits to architectural rigor: decisions that shape the codebase for more than one block are captured as ADRs so the reasoning survives the next session, the next slice, and the grader's review. Each ADR records the **context** that forced the decision, the **decision** itself with its alternatives, and the **consequences** (positive and negative) that follow.

ADRs are referenced from three places:

- **Code** — inline comments at the affected seam (e.g. `// see ADR 0005 §"Repository pattern"`).
- **Specification** — `docs/spec/use-cases.md`, `docs/spec/nfa.md`, and `docs/spec/acceptance-criteria.md` cross-link to ADRs for the "why" behind a requirement's realization.
- **Per-block reflection** — each block's Nachbereitung in `vault/Blöcke/` cites the ADR(s) accepted during that block.

## When to write a new ADR

A new ADR is created whenever a decision:

- Changes a public contract (driving port, REST endpoint, persistence schema).
- Selects a third-party dependency the project couples to (EF Core, MassTransit, MEAI, pgvector).
- Defines a policy that more than one feature must obey (e.g. the AI fallback contract in ADR 0004 §D5).

Smaller, reversible choices live in commit messages or inline comments — not every preference needs an ADR.

## Format

ADRs follow the lightweight Nygard format adapted for this project: **Context → Decision → Consequences**, with explicit sections for **Alternatives considered** (and why they were rejected) and **Open questions** carried forward.

**Status values:** `Proposed` · `Accepted` · `Deprecated` · `Superseded by ADR-XXXX`

## Stack-mismatch annotations

The CAS AISE coursework occasionally prescribes a Quarkus / Jakarta-EE-flavored realization (Hibernate / Panache, Jakarta Data, MicroProfile REST Client). FlowHub's .NET 10 stack uses functional equivalents: EF Core 10 + `DbSet<T>` + LINQ, `IHttpClientFactory` + typed clients, MassTransit. ADRs that depart from the prescribed stack call this out explicitly in a "Stack mismatch — N/A justification" section, with the .NET-native counterpart documented alongside (precedent set in ADRs 0002, 0003, 0004, 0005).
