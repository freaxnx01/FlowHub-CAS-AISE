# FlowHub — Explainer Video (Technical)

<!-- scene: hook -->
FlowHub is an integration hub that orchestrates your self-hosted services — instead of you operating each one by hand.

<!-- scene: architecture -->
At its core, a modular monolith: separate modules for domain, AI, persistence, integrations, and the Blazor web frontend.

<!-- scene: airouting -->
Every capture runs through an AI classifier built on Microsoft Extensions AI. It detects the category and routes to the matching skill integration.

<!-- scene: integrations -->
Through ports and adapters, swappable integrations plug in: Wallabag for bookmarks, Vikunja for tasks, Telegram as an inbound channel.

<!-- scene: stack -->
Built on .NET 10 and Blazor, with Entity Framework Core, OpenTelemetry, health endpoints, and end-to-end test coverage.

<!-- scene: close -->
FlowHub — modular, testable, extensible.
