# ADR 0002 — Service Architecture and Async Communication

- **Status:** Accepted
- **Date:** 2026-04-17
- **Block:** Block 3 (Services) — Vorbereitung / early Nachbereitung
- **Decider:** freax
- **Affects:** `source/FlowHub.Core/`, `source/FlowHub.Api/` (to be scaffolded), `source/FlowHub.Skills/`, `source/FlowHub.Integrations/`, `source/FlowHub.AI/`, `source/FlowHub.Web/`, future `infra/docker-compose.*.yml`

---

## Context

The Block 3 Moodle Auftrag (*W4B-C-AS001.AISE.ZH-Sa-1.PVA.FS26: RESTful API* and *Leseauftrag: Microservices- und Service-based-Architekturen*) requires us to:

- Demonstrate understanding of microservice / service-based architectures.
- Build a service that exposes a synchronous REST API.
- Demonstrate at least one form of asynchronous, event-based communication between services.
- Reflect on the trade-offs of synchronous vs asynchronous styles.

ADR 0001 already established that:

- FlowHub is a single-operator, self-hosted application running in a homelab.
- The Web UI runs **in-process** (Blazor Interactive Server) and does **not** consume FlowHub's REST API — the API exists for *other* clients (Telegram, integrations, automation).
- The repository follows a flat **`source/FlowHub.<Capability>/`** layout (not `src/Modules/<Module>/`).

What ADR 0001 did **not** decide is how the capabilities (`Core`, `AI`, `Skills`, `Integrations`, `Telegram`, `Persistence`) talk to each other once they are filled in, and whether they live in one process or several. That is the question Block 3 forces us to answer.

The Block 3 Vorbereitung built a sandbox (`poc/restful-api-playground/`) with `OrderService` + `NotificationService` + RabbitMQ + gRPC to internalise the trade-offs. The reflection from that sandbox (`poc/restful-api-playground/REFLECTION.md`) concluded:

- **MQ** earns its place where producers and consumers must be decoupled, where the producer's response time should not depend on the consumer, where bursts must be absorbed, and where retry / DLQ are needed for flaky downstream calls.
- **gRPC** earns its place only when there is a real cross-service call between services we own and we want a typed, streaming-capable, low-latency contract.
- For a single-language, single-operator monolith calling external REST APIs, gRPC adds tooling without solving a problem we have.

This ADR codifies how those conclusions land in FlowHub's actual code layout.

---

## Decision

### 1. Keep the Modular Monolith — do **not** split into physical microservices for Block 3

FlowHub stays a single deployable process. Each capability remains its own .NET project under `source/FlowHub.<Capability>/`:

- `FlowHub.Core` — domain types, driving ports, domain services
- `FlowHub.AI` — classification adapter
- `FlowHub.Skills` — skill registry + skill implementations
- `FlowHub.Integrations` — outbound adapters to Wallabag, Wekan, Vikunja, …
- `FlowHub.Persistence` — EF Core (Block 4)
- `FlowHub.Telegram` — inbound channel adapter
- `FlowHub.Api` — REST API surface for non-UI consumers (new, scaffolded in this block)
- `FlowHub.Web` — Blazor host (already exists)

Cross-capability communication is via **interfaces declared in `FlowHub.Core`**, implemented by the appropriate capability project, and resolved through the .NET DI container. No project references between sibling capabilities; the only allowed inbound dependency is on `FlowHub.Core`.

This satisfies the Moodle learning objectives at the **logical** level: clear service boundaries, ports and adapters, dependency inversion, and a documented reason for each boundary. It does **not** introduce the operational complexity (separate deployments, network calls, distributed tracing across processes, eventual consistency between data stores) that a physical microservice split would require.

### 2. Introduce an async message bus for the Capture-Enrichment and Skill-Routing pipelines

The flow is:

1. A `Capture` arrives from any Channel (Web, Telegram, future API).
2. `CaptureService.Submit(...)` persists the Capture **synchronously** and returns. The caller (UI, Telegram, API client) sees a `Created` result with the Capture id.
3. `CaptureService` then **publishes** a `CaptureCreated` event onto an in-process bus.
4. Subscribers run independently:
   - `AI.ClassificationHandler` — calls the AI adapter, writes the classification back, publishes `CaptureClassified`.
   - `LinkPreviewHandler` — fetches an OpenGraph preview for URL captures.
   - (future) `TagSuggestionHandler`, `DuplicateDetectionHandler`, …
5. `Skills.RoutingHandler` subscribes to `CaptureClassified` and dispatches to the matching `Skill`. Each `Skill` then calls the relevant `Integration` (Wallabag, Wekan, Vikunja). Failure handling (retry, DLQ) lives at this layer.

**Implementation:**

- Use **MassTransit** as the abstraction so the same producer / consumer code works against multiple transports.
- **Default transport in dev and test:** MassTransit's **in-memory transport**. No broker dependency for `make run` / `make watch` / `dotnet test`.
- **Production / staging transport:** **RabbitMQ**, configured via `Bus__Transport=RabbitMq` and `Bus__RabbitMq__Host=…`. Wired up in Block 5 (Deployment) alongside Docker Compose.
- The transport choice is a `Program.cs` registration concern. Producer and consumer code is transport-agnostic.

This gives Block 3 a clean answer to *"demonstrate asynchronous, event-based communication"* without forcing every developer to run RabbitMQ locally.

### 3. Outbound calls to external Skill integrations stay **synchronous REST**

Wallabag, Wekan, Vikunja, etc. expose REST APIs. Skill implementations call them synchronously inside an MQ consumer. The consumer is the resilience boundary:

- HTTP failures from external integrations are caught at the consumer and trigger MassTransit's retry policy.
- After retries are exhausted the message moves to a dead-letter queue (RabbitMQ in prod, the in-memory transport simulates this via a fault consumer).
- Operator-visible failures surface on the Skill detail page (`/skills`) and the Capture detail page (`/captures/{id}`).

The fact that the *external* call is synchronous is fine — the *FlowHub-internal* event flow upstream of it is async, so the user-facing POST does not wait for Wallabag to respond.

### 4. Build a REST API in `source/FlowHub.Api/` for non-UI consumers

Per ADR 0001, the API exists for Telegram, integrations, automation, CLI, and webhook receivers — not for the Blazor UI. This block scaffolds it:

- Hosted in the same process as `FlowHub.Web` for now (separate ASP.NET Core endpoint groups, shared DI container).
- Minimal API endpoints, FluentValidation at the boundary, ProblemDetails (RFC 9457) for errors, Scalar UI at `/scalar`.
- Initial endpoints (refined during implementation):
  - `POST /api/captures` — submit a Capture from a non-UI channel.
  - `GET /api/captures` / `GET /api/captures/{id}` — read, with filters mirroring the UI list page.
  - `POST /api/captures/{id}/retry` — re-publish a `CaptureCreated` event for a failed Capture.
  - `GET /api/skills` / `GET /api/integrations` — read-only health and metadata.

The API is deliberately **separate** from any internal interfaces. Internal contracts (the methods on `CaptureService` etc.) can change freely; the public REST contract is versioned and breaks visibly.

### 5. **Do not** introduce gRPC in FlowHub for Block 3

Reasons (carried over from `poc/restful-api-playground/REFLECTION.md`):

- FlowHub is a single-language (.NET / C#) stack. The polyglot benefit of gRPC's code-generated multi-language clients does not apply.
- Without a physical service split (Decision 1), there are no FlowHub-internal RPC calls to make. In-process method calls already give us typed, low-latency communication for free.
- External integrations (Wallabag, Wekan, Vikunja) speak REST. The integration boundary is fixed by them.
- Adding gRPC just to satisfy the Moodle reading list would be architecture-for-its-own-sake. The Block 3 reflection covers gRPC in writing instead, with the playground POC as the worked example.

### 6. Versioning and contract stability

- **Internal events** (`CaptureCreated`, `CaptureClassified`, `SkillRouted`, …) live in `FlowHub.Core` and may evolve freely while everything is in one process. Once the bus crosses a process boundary (i.e. when / if we ever split), additive-only changes become mandatory and a `V2` event type is introduced for breaking changes.
- **Public REST API contracts** are versioned via URL prefix (`/api/v1/...`) from day one. A `v2` prefix is added when a breaking change ships.
- **Internal interfaces** (`ICaptureRepository`, `ISkill`, …) carry no versioning — change them with the call sites, in one PR.

---

## Alternatives Considered

### A. Split FlowHub into physical microservices for Block 3

- ❌ Operational cost (multiple deployments, separate databases or schema partitioning, distributed tracing setup, inter-service auth) is wildly out of scale for a single-operator homelab app.
- ❌ ADR 0001 already established that the Web UI runs in-process and reads via direct service calls. Splitting would force the UI onto HTTP for its own data, contradicting that decision.
- ❌ The PVA grading dimension *"Realistische Projektarbeit"* would suffer — a contrived multi-service split for one operator is not realistic.
- ⏸ The decision is reversible: a capability that genuinely outgrows the monolith can be lifted out into its own process later, with the bus already in place to absorb the change.

### B. Use a custom in-process event aggregator (e.g. MediatR notifications) instead of MassTransit

- ⚠ Works for the in-memory case but locks us in. Swapping to a real broker later would mean rewriting every publisher and consumer.
- ⚠ No retry / DLQ semantics out of the box — we would re-implement what MassTransit gives us.
- ✅ Simpler dependency surface today.
- → Rejected because the Block 3 Lernziele explicitly cover async / event-based architectures, and MassTransit makes the broker upgrade path a config change rather than a code change. The cost of MassTransit in dev (one extra package, no broker required thanks to the in-memory transport) is acceptable.

### C. Use gRPC for FlowHub-internal communication

- ❌ See Decision 5. No service split → no internal RPC calls to make.
- ❌ Single-language stack removes the polyglot benefit.

### D. Expose the FlowHub REST API by also pointing the Blazor UI at it

- ❌ Directly contradicts ADR 0001 Decision 2.
- ❌ Adds HTTP serialization for every UI read with no benefit (single operator, no scaling pressure).

### E. RabbitMQ as the only transport (no in-memory option)

- ❌ Forces every developer to run Docker for `make watch` and every CI run to spin up a broker container.
- ❌ Slows the test suite.
- → Rejected. MassTransit's in-memory transport is purpose-built for this case.

---

## Consequences

### Positive

- **One deployment unit** — `make run` still starts everything. CI stays simple. Docker Compose in Block 5 has one app container plus its dependencies (DB, broker), not seven.
- **Clear logical boundaries** with the option to split later — ports in `Core`, adapters in capability projects, no sibling project references.
- **Async pipeline answers the Moodle Lernziel** for event-based / asynchronous communication with a worked example (`CaptureCreated` → enrichment fan-out → routing).
- **Resilience to flaky external integrations** — MQ retry / DLQ wraps every outbound Skill call.
- **UI stays responsive** — `POST /api/captures` and the Web Channel quick-add return immediately; enrichment happens asynchronously.
- **Transport flexibility** — switch dev / prod transports via env var. No code changes in producers / consumers.

### Negative

- **Eventual consistency in the enrichment chain.** The caller cannot tell from the `Created` response whether classification succeeded. Mitigation: surface enrichment status on the Capture detail page; expose a `lifecycle` field that progresses `Raw → Classified → Routed → Completed | Failed`.
- **In-memory transport in dev hides certain bugs** that only show up against a real broker (serialization, connection drops, queue backlog). Mitigation: a dedicated `make integration-test` target spins up RabbitMQ and runs a small set of end-to-end tests against it. Block 5 makes RabbitMQ part of the staging deployment.
- **MassTransit is a non-trivial dependency** (additional NuGet packages, learning curve, opinionated patterns). Mitigation: keep its surface area limited to publishers, consumers, and `Program.cs` registration; do not adopt MassTransit's higher-level features (sagas, state machines) until there is a concrete need.
- **The "microservices" framing in the Moodle Auftrag is answered with a logical split, not a physical one.** This is a defensible position but must be explained clearly in the PVA — write-up risk if it reads as ducking the assignment. Mitigation: this ADR is the explanation, plus a short reflection in the Block 3 Nachbereitung notes.

### Neutral

- The decision to skip gRPC is documented and reversible. If a later block introduces a real cross-process call between FlowHub-owned services, gRPC is the obvious tool — the playground POC shows we know how to wire it up.
- The REST API layer is now a thin facade over the same services the UI uses. Any feature added to the UI is implicitly available to the API once an endpoint is added, and vice versa.

---

## Implementation Notes (for Block 3 Nachbereitung)

The implementation work this ADR unblocks:

1. **Scaffold `source/FlowHub.Api/`** — empty placeholder today. Add Minimal API endpoint groups (`captures`, `skills`, `integrations`), FluentValidation, ProblemDetails, Scalar UI, and the `/api/v1/...` URL prefix.
2. **Scaffold `source/FlowHub.Skills/` and `source/FlowHub.Integrations/`** — both are placeholders. Define `ISkill` and `IIntegration` ports in `FlowHub.Core`, then move at least one implementation (Wallabag is the easiest external service to mock) into the Skills / Integrations projects.
3. **Wire MassTransit** in `source/FlowHub.Web/Program.cs` (the host project for Block 3).
   - Default `Bus__Transport=InMemory` in `appsettings.Development.json`.
   - `Program.cs` reads the env var and selects the transport at startup.
4. **Define internal events** in `source/FlowHub.Core/Events/`: `CaptureCreated`, `CaptureClassified`, `CaptureRouted`, `CaptureCompleted`, `CaptureFailed`.
5. **Replace the Bogus stubs incrementally** — `CaptureService` already exists as a stub; route its `Submit(...)` through the bus instead of returning a fake immediately. `AI.ClassificationHandler` becomes the first real consumer.
6. **Add Bruno collections** under `bruno/api/` for the new REST endpoints, mirroring the URL structure.
7. **Add an integration test** under `tests/FlowHub.Web.ComponentTests/` (or a new `tests/FlowHub.Api.IntegrationTests/`) that publishes `CaptureCreated` against the in-memory transport and asserts the consumer pipeline runs end-to-end.
8. **Document the boundary in the PVA write-up** — Decision 1 is the load-bearing claim and needs a short explicit paragraph in the Block 3 Nachbereitung notes.

Each numbered item above will be its own short brainstorm → plan → implementation cycle. This ADR is the umbrella decision; it does not replace per-feature planning.

---

## References

- Moodle: *W4B-C-AS001.AISE.ZH-Sa-1.PVA.FS26: RESTful API* — assignment text
- Moodle: *Leseauftrag: Microservices- und Service-based-Architekturen* — reading list
- ADR 0001: `docs/adr/0001-frontend-render-mode-and-architecture.md`
- POC: `poc/restful-api-playground/` — RabbitMQ + gRPC playground
- POC reflection: `poc/restful-api-playground/REFLECTION.md`
- `CLAUDE.md` — repo conventions (modular monolith, no cross-module project references, MudBlazor, ProblemDetails, Scalar)
- `.ai/cas-instructions.md` — block schedule and grading dimensions
- Vault: `Blöcke/03 Service/03 Service - a) Vorbereitung.md` — Block 3 prep checklist and Leitfragen
- MassTransit docs: [In-Memory transport](https://masstransit.io/documentation/configuration/transports/in-memory) and [RabbitMQ transport](https://masstransit.io/documentation/configuration/transports/rabbitmq)
