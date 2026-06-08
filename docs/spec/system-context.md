# FlowHub — System Context (C4 Level 1)

## Context Diagram

```mermaid
graph TD
    Operator["👤 Operator<br/>(single user)"]

    subgraph FlowHub ["FlowHub (this system)"]
        Web["FlowHub.Web<br/>Blazor Interactive Server<br/>Dashboard, Captures, Skills, Integrations"]
        Core["FlowHub.Core<br/>Domain types + driving ports"]
        AI["FlowHub.AI<br/>AI classification<br/>(Ollama, future)"]
        Skills["FlowHub.Skills<br/>Skill implementations<br/>(future)"]
        Telegram["FlowHub.Telegram<br/>Telegram bot channel<br/>(future)"]
    end

    subgraph Downstream ["Downstream Integrations (self-hosted)"]
        Wallabag["Wallabag<br/>Read-later"]
        Vikunja["Vikunja<br/>Tasks / lists / kanban"]
        Paperless["Paperless-ngx<br/>DMS"]
        Obsidian["Obsidian<br/>Markdown notes via git"]
    end

    Authentik["Authentik<br/>SSO / OIDC IdP<br/>(homelab)"]
    Ollama["Ollama<br/>Local LLM inference<br/>(homelab)"]
    TelegramAPI["Telegram Bot API<br/>(external)"]

    Operator -- "browser (SignalR)" --> Web
    Operator -- "Telegram message" --> TelegramAPI
    TelegramAPI -- "webhook" --> Telegram
    Web -- "in-process DI" --> Core
    Telegram -- "in-process DI" --> Core
    Core --> AI
    AI -- "REST (local)" --> Ollama
    Core --> Skills
    Skills -- "REST" --> Wallabag
    Skills -- "REST" --> Vikunja
    Skills -- "REST" --> Paperless
    Skills -- "git push" --> Obsidian
    Web -- "OIDC" --> Authentik
```

## Key relationships

| From | To | Protocol | Notes |
|---|---|---|---|
| Operator → FlowHub.Web | HTTP + SignalR (WebSocket) | Blazor Interactive Server; single long-lived circuit per session |
| Operator → Telegram Bot API | HTTPS | Operator sends message to bot; Telegram forwards via webhook |
| FlowHub.Web → FlowHub.Core | In-process DI | No HTTP — `@inject` services directly (per ADR 0001 §2) |
| FlowHub.Telegram → FlowHub.Core | In-process DI | Same process, same pattern |
| FlowHub.Core → FlowHub.AI | In-process | Classification service calls Ollama REST API for inference |
| FlowHub.AI → Ollama | HTTP REST (local) | `http://ollama:11434` — runs on the same homelab, never leaves the network |
| FlowHub.Skills → Integrations | HTTP REST / git | Each Skill writes to one or more downstream services via their APIs |
| FlowHub.Web → Authentik | OIDC (HTTPS) | Auth code flow; tokens in cookie; SignalR circuit reads cookie |

## Current state (Block 5 — submission)

The solution (`FlowHub.slnx`) contains six implemented projects; two folders remain
intentional, not-yet-implemented placeholders.

- **Implemented (in the solution, with code):**
  - `FlowHub.Web` — Blazor Web App (Interactive Server) + the REST API host
  - `FlowHub.Core` — domain types and driving/driven ports
  - `FlowHub.Api` — REST endpoint contracts for non-UI consumers
  - `FlowHub.AI` — LLM-backed classifier behind the `IClassifier` port (provider abstraction + keyword fallback)
  - `FlowHub.Persistence` — EF Core + PostgreSQL repositories and migrations
  - `FlowHub.Skills` — Wallabag and Vikunja `ISkillIntegration` adapters
- **Placeholder (folder only, not in the solution, planned for a later iteration):**
  `FlowHub.Telegram`, `FlowHub.Integrations` — each carries a `README.md` describing its planned role.
- **Not yet wired:** Authentik OIDC (dev bypass only), Ollama-hosted inference (see ADR 0007), the Telegram channel.
- **REST API:** available since Block 3 (`/api/v1/captures`, exercised live on the public demo).

## Deployment context (Block 5, future)

```mermaid
graph TD
    subgraph Homelab ["Homelab (Docker Compose)"]
        FH["FlowHub container<br/>ASP.NET 10 Alpine"]
        PG["PostgreSQL<br/>(Block 4)"]
        OL["Ollama<br/>LLM inference"]
        Auth["Authentik<br/>SSO"]
    end

    CF["Cloudflare Tunnel"]
    Traefik["Traefik<br/>reverse proxy"]

    CF -- "HTTPS" --> Traefik
    Traefik -- "HTTP" --> FH
    FH -- "TCP" --> PG
    FH -- "HTTP" --> OL
    FH -- "OIDC" --> Auth
```

## Persistence Layer

FlowHub uses PostgreSQL 17 as its primary database, accessed via EF Core 10 with the Npgsql provider. The schema follows a migrations-first workflow: all schema changes are expressed as EF Core migration files committed to Git and applied as an idempotent SQL script at deploy time.

The Repository pattern separates domain logic from database access. Repository interfaces are defined in `FlowHub.Core` (returning domain types), with EF Core implementations in `FlowHub.Persistence`. Application-layer services (`ICaptureService`, `ISkillRegistry`, `IIntegrationHealthService`) compose repositories; they never reference `FlowHubDbContext` directly.

Local development uses `docker compose up postgres` to start a PostgreSQL container. `just db-migrate` applies pending migrations. `just run` starts the application, which auto-migrates via `MigrationRunner` for convenience.
