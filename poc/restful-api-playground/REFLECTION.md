# Reflection — RESTful API Playground (Block 3 Vorbereitung)

## Why a Message Broker in This POC?

The playground models two services:

- **`OrderService`** — REST API (`POST /orders`) that persists orders in memory and publishes an `OrderPlaced` event. Also exposes a gRPC endpoint for looking up order details.
- **`NotificationService`** — consumes `OrderPlaced` from RabbitMQ, calls back to `OrderService` via gRPC to enrich the event, and stores a notification.

A direct `OrderService → NotificationService` HTTP call would technically deliver the same end-to-end result. The message broker (RabbitMQ, driven by MassTransit) earns its keep through the following properties.

### Concrete advantages in this system

- **Decoupling of producer and consumer.** `OrderService` has no project reference, URL, or client for `NotificationService`. Any new subscriber (audit log, email gateway, analytics, …) can bind its own queue to the `OrderPlaced` exchange without touching the producer.
- **Fast, non-blocking POST.** The HTTP caller receives `201 Created` as soon as `publish.Publish(new OrderPlaced(...))` returns. The notification work happens outside the request path, so notification latency or load does not bleed into order-creation latency.
- **Resilience to consumer outages.** If `NotificationService` is down, messages accumulate in the `OrderPlacedConsumer` queue and are processed once the consumer recovers. With direct HTTP the producer would have to either fail the order, swallow the error, or implement its own outbox + retry machinery.
- **Load leveling / back-pressure.** A burst of orders does not stampede the consumer; the queue absorbs the spike and the consumer drains at its own sustainable rate. This keeps tail latencies predictable even under bursty traffic.
- **Fan-out via pub/sub.** MassTransit publishes to a topic exchange; each consumer declares its own queue. One `Publish` reaches N consumers with no fan-out logic in the producer.
- **Competing consumers for horizontal scale.** Running multiple `NotificationService` instances against the same queue gives load-balanced processing out of the box — no separate load balancer or sticky routing required.
- **Retry policies and dead-letter handling for free.** MassTransit ships with configurable retry, redelivery, and DLQ semantics. Equivalent behaviour on raw HTTP would require custom infrastructure.
- **Location transparency.** Services only need the broker address. No service discovery or per-downstream HTTP client configuration on the producer side.

### Trade-offs (Leitfrage "Vor- und Nachteile asynchroner Kommunikation")

- **Eventual consistency.** The HTTP caller cannot confirm that the notification was produced — only that the order was accepted.
- **Broker as operational dependency.** RabbitMQ becomes a single point of failure unless clustered; it needs monitoring, upgrades, and credential management.
- **Event-schema versioning.** Consumers and producers evolve independently, so breaking changes to event contracts need an explicit versioning strategy (e.g. additive-only fields, versioned routing keys).
- **Debuggability.** Tracing a request across the broker requires distributed tracing (correlation IDs, OpenTelemetry context propagation). In-process or HTTP call stacks are easier to follow with a debugger alone.
- **Latency tail.** For individual messages the broker adds serialization + network + queueing overhead. Fine for eventing, wrong tool for tight synchronous loops.

### Sync vs async in the same POC

Notably, the playground keeps a **synchronous gRPC call** from the consumer back to `OrderService` for order details. That is a deliberate contrast: async eventing for "something happened" (no answer expected, many listeners possible), synchronous request/response when the consumer needs a specific piece of data *right now*. The two styles complement each other — choosing the wrong one in either direction creates awkward designs (polling for events, or broker round-trips for trivial lookups).

### Takeaways for FlowHub (Block 3)

Candidates in FlowHub that map naturally onto async/eventing:

- **Capture-Enrichment pipeline** — a new `Capture` is persisted synchronously, then enrichment (classification, link preview, tag suggestion) runs via events. UI stays responsive; multiple enrichers can attach independently.
- **Skill routing** — routing a Capture to the target Skill (Wallabag, Wekan, Vikunja, …) is a natural event, with each Skill consuming what it cares about.
- **Health and integration probes** — status changes can be broadcast so the dashboard and logging pipeline both observe them without the probe knowing either consumer.

Read-path lookups (open a Capture detail, load a Skill status snapshot) stay synchronous — request/response with a direct store or gRPC/REST call is the right tool there.

## gRPC or MQ in FlowHub?

Short answer: **probably neither inside FlowHub today, MQ later when async enrichment lands, gRPC unlikely.**

### Today (Block 2 → 3, modular monolith per ADR 0001)

- Internal cross-module talk is in-process interfaces — no transport needed.
- External Skills (Wallabag, Wekan, Vikunja) speak **REST**, not gRPC or AMQP, so the integration boundary is fixed by them.

### Where MQ earns its place in FlowHub

- **Capture → enrichment fan-out** — persist Capture synchronously, then publish `CaptureCreated`; AI classification, link-preview, tag suggestion subscribe independently. Keeps the UI responsive and lets you add enrichers without touching the producer.
- **Skill routing** — `CaptureClassified` → each Skill module decides whether the event is for it. Clean fan-out, natural retry/DLQ for flaky external APIs.
- **Resilience to outages** — if Wallabag is down, the Skill consumer retries from the queue instead of losing the action.

### Where gRPC would earn its place

Only if a service is actually split out of the monolith and needs a typed, low-latency request/response between *FlowHub-internal* services (the POC's "consumer asks producer for details" pattern). For a modular monolith calling external REST APIs, gRPC adds tooling without solving a problem we have.

### Recommendation for the PVA narrative

Keep the monolith. Introduce **MQ** for the Capture-enrichment / Skill-routing pipeline — it directly answers the Block 3 Leitfrage on async vs sync and gives a concrete pattern to demo. Skip gRPC unless a service is also split out; otherwise it is architecture-for-its-own-sake.

### Tradeoff to be aware of

MQ adds an operational dependency (RabbitMQ) and eventual consistency on the enrichment side. Worth it for the decoupling; not worth it if everything in scope is a single synchronous request/response.

## Detaillierte Arbeitsanweisung vs schrittweise Delegation

Two ways to interact with the AI, distinguished by **who decomposes the problem**:

- **Detaillierte Arbeitsanweisung** — *I* decompose. The prompt names file paths, method signatures, exact behaviour, tests. The AI executes my design fast. Fits: small well-defined fix, strong opinion on the design, security- or correctness-sensitive change, unusual constraint the AI would not infer.
- **Schrittweise Delegation** — *the AI* decomposes. I give the goal; it explores, proposes, asks, implements; I approve each step before the next. Fits: exploring a design space, multi-file feature where decomposition *is* the work, greenfield area without fixed conventions, learning unfamiliar territory.

**Rule of thumb.** If I can write the method's docstring myself, write the instruction myself. If I cannot yet articulate the design, do not pretend to — delegate, and use the AI's first proposal to discover what I actually want. The worst mode is the middle: vague instruction + no checkpoints → the AI invents a direction I did not want, context is burned before I notice.

**Concrete in this POC.**

- Detailed instruction worked well for: adding the `OrderGrpcService` method signatures once the `.proto` was fixed; writing the `OrderPlacedConsumerTests` with an explicit "use MassTransit Test Harness + NSubstitute for the gRPC client" directive.
- Step-by-step delegation worked well for: the original Option A vs Option B architectural split decision (I described the goal, the AI proposed two layouts, I picked one); the initial scaffolding where I did not yet know the MassTransit registration shape.
- The 70% Problem (Beyond Vibe Coding Kap. 3+4) showed up concretely: delegation got the scaffold working quickly, but the last 30% — CentralPackageManagement tuning, `Grpc.Tools` `PrivateAssets`, `AddOpenApi` package resolution — needed either me reading the errors carefully or switching to detailed instruction ("add package X at version Y to `Directory.Packages.props` and set `PrivateAssets=all` on the Grpc.Tools reference in `Contracts.csproj`").

## Observed problems with AI-generated code

Concrete issues while scaffolding this POC — useful data points for the Moodle Leitfrage *"Welche Probleme können bei KI-generiertem Code entstehen, und wie vermeidet man sie?"*.

- **CentralPackageManagement collision.** The root repo pins package versions in `Directory.Packages.props`. The AI's initial `dotnet new` scaffold added `<PackageReference Version="…" />` attributes in the playground's `.csproj` files, which triggers `NU1008` under Central Package Management. Fix: the playground got its own `Directory.Packages.props` so it can diverge from the root pinning, and `<PackageVersion>` entries were moved there. Root cause: the AI did not read the surrounding `Directory.Build.props` / `Directory.Packages.props` before generating the new project. Prevention: either share the repo conventions explicitly in the prompt, or review the generated `.csproj` diff before `dotnet build`.

- **Strict analyzer defaults broke the build.** The root `Directory.Build.props` sets `TreatWarningsAsErrors=true` plus the full .NET analyzer ruleset. Generated code tripped on CA1848 (LoggerMessage source-gen), CA1873 (cache culture on date formatting), and CA1050 (missing namespaces in auto-generated Protobuf stubs). Fix: the playground's local `Directory.Build.props` sets `NoWarn` for those specific rules and disables `TreatWarningsAsErrors` for generated `.g.cs` files. Root cause: the AI optimises for "code that compiles on default settings", not "code that compiles under strict project policy". Prevention: include the analyzer policy in the brief, or accept a first failed build and iterate.

- **`AddOpenApi` / `MapOpenApi` without the package.** In .NET 10 the built-in OpenAPI support lives in `Microsoft.AspNetCore.OpenApi` — it is **not** part of the `Microsoft.AspNetCore.App` shared framework. The AI wrote the two calls assuming they would resolve; build failed at `AddOpenApi` cannot be found. Fix: add the package explicitly. Root cause: API surface drift between .NET versions that the AI's training data straddles. Prevention: for any API call on a fresh template, check the NuGet reference before assuming.

- **`Grpc.Tools` leaked into downstream projects.** The AI added `Grpc.Tools` to `Contracts.csproj` without `<PrivateAssets>all</PrivateAssets>`. That propagates the build-time `protoc` tooling into every project that references `Contracts`, bloating them and occasionally causing duplicate `.g.cs` generation. Fix: `PrivateAssets=all` on all `Grpc.Tools` references. Root cause: subtle NuGet metadata the AI skipped over. Prevention: any tooling-only package reference needs a `PrivateAssets` audit.

- **Consumer-owned gRPC channel.** The original consumer created `GrpcChannel.ForAddress(...)` itself. That works at runtime but is untestable — the consumer cannot be unit-tested against a faked client. Refactored to `AddGrpcClient<T>()` + constructor injection, then the test harness uses `NSubstitute` for the gRPC client. Root cause: the AI took the obvious happy-path wiring without thinking about testability. Prevention: testability is a project-level constraint that needs to be stated, not assumed.

**Pattern across these.** The AI is strong at *local* correctness (this file compiles, this test passes) and weak at *contextual* correctness (this file fits the repo's conventions, this wiring survives unit testing). The countermeasure is to surface repo-wide constraints up front (CLAUDE.md, explicit prompt) and to review the diff, not the output, after generation.

## Sync vs async — decision logic

A short decision tree for which transport fits which call.

- **HTTP/REST (synchronous request-response)** — caller needs an answer *now*, response shape is loosely typed, audience includes non-.NET clients or humans with curl. Use when: Web UI → backend read, browser or external automation calls the API, single-shot CRUD against a resource. Example in the POC: `POST /orders` accepts the order request; `GET /notifications` lets the test script verify the flow. In FlowHub: the `/api/captures` surface for Telegram / integrations / CLI.
- **gRPC (synchronous, typed, binary RPC)** — caller and callee are both services we own, we want a typed contract and low-latency request-response, streaming may be useful later. Use when: one internal service asks another for specific data. Example in the POC: `NotificationService` asks `OrderService` for order details via `OrderGrpc.GetOrder(orderId)`. In FlowHub: **none today** — the monolith has no cross-process RPC calls. gRPC becomes relevant only if a capability is lifted into its own process.
- **Message broker / events (asynchronous pub-sub)** — producer and consumer must be decoupled in time, one publish should fan out to many consumers, the producer's latency must not depend on the consumer, retry / DLQ behaviour is wanted. Use when: "something happened" events, background enrichment, flaky external integrations. Example in the POC: `OrderPlaced` event on RabbitMQ, with `OrderPlacedConsumer` running independently. In FlowHub: `CaptureCreated` → enrichment fan-out (classification, link preview, tag suggestion), `CaptureClassified` → `Skills.RoutingHandler` → external integration call.

**Heuristic.** If the caller would block waiting for the answer anyway, use sync (REST to the outside, gRPC internally). If the caller only needs to say *"this happened, someone please deal with it"*, use a broker — and accept eventual consistency in exchange for decoupling and resilience.

This answers the Moodle Leitfrage *"Was sind Vor- und Nachteile asynchroner Kommunikation?"* from the concrete perspective of the POC and of FlowHub's planned event pipeline (see ADR 0002).

## AI-Assistant Notes (Claude Code)

- Used for wire-up of MassTransit + RabbitMQ configuration and gRPC client setup; useful for boilerplate, less useful for choosing the architectural split.
- Verifying the POC against the Block 3 Leitfragen is a human task — the assistant can produce text, but the understanding has to transfer to the PVA.
