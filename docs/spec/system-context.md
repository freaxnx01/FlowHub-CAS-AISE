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
        Wekan["Wekan<br/>Kanban"]
        Vikunja["Vikunja<br/>Tasks / lists"]
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
    Skills -- "REST" --> Wekan
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

## Current state (Block 2)

- **Implemented:** FlowHub.Web + FlowHub.Core (6 pages, 3 stub services, DevAuthHandler)
- **Placeholder:** FlowHub.AI, FlowHub.Skills, FlowHub.Telegram, FlowHub.Integrations, FlowHub.Persistence (empty project folders)
- **Not yet wired:** Authentik OIDC (dev bypass only), Ollama, all downstream integrations
- **No REST API yet** — the API for non-UI consumers (Telegram, external automation) lands in Block 3

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
