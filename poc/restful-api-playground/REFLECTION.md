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

## AI-Assistant Notes (Claude Code)

- Used for wire-up of MassTransit + RabbitMQ configuration and gRPC client setup; useful for boilerplate, less useful for choosing the architectural split.
- Verifying the POC against the Block 3 Leitfragen is a human task — the assistant can produce text, but the understanding has to transfer to the PVA.
