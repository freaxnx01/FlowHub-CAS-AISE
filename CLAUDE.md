# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

FlowHub is a single-user, AI-powered personal inbox that receives messages via a Telegram bot, automatically classifies them using a hybrid skill system, and routes them to the appropriate service (Todoist, Wallabag, paperless-ngx, or a local PostgreSQL inbox). It is a CAS AISE (FFHS) project targeting .NET 10 / C#.

## Build & Run Commands

```bash
# Restore and build the entire solution
dotnet build

# Run a specific project
dotnet run --project src/FlowHub.Telegram

# Run all tests
dotnet test

# Run a single test project
dotnet test tests/FlowHub.Core.Tests

# Run a specific test by filter
dotnet test --filter "FullyQualifiedName~MyTestMethod"

# EF Core migrations (from repo root, targeting Persistence project)
dotnet ef migrations add <Name> --project src/FlowHub.Persistence --startup-project src/FlowHub.Telegram
dotnet ef database update --project src/FlowHub.Persistence --startup-project src/FlowHub.Telegram

# Docker Compose (local dev with PostgreSQL, Redis, Ollama)
docker compose up -d
```

## Architecture

### Layer Structure

```
Telegram Bot ──→ FlowHub.Core (SkillRegistry) ──→ Skill Handlers ──→ Integration Clients
                        │                                                    │
                   FlowHub.AI                                          Todoist / Wallabag /
                (M.E.AI + Ollama)                                     paperless-ngx / Inbox
```

- **FlowHub.Telegram** — ASP.NET Core host, Telegram webhook handler. Entry point for the application.
- **FlowHub.Core** — Domain models, interfaces (`ISkillHandler`, `ISkillRegistry`), no external dependencies.
- **FlowHub.Skills** — Skill implementations: `MovieSkillHandler`, `TechArticleSkillHandler`, `DocumentSkillHandler`, `GenericSkillHandler`. Each skill has a paired `SKILL.md` config file.
- **FlowHub.AI** — Wraps `Microsoft.Extensions.AI` for LLM-based classification with confidence scoring. Ollama is the primary provider; Anthropic API is fallback.
- **FlowHub.Integrations** — Typed REST clients via Refit interfaces for Todoist, Wallabag, and paperless-ngx.
- **FlowHub.Persistence** — EF Core DbContext, repositories, Redis state management for pending user inputs.
- **FlowHub.Web** — Blazor SSR admin dashboard.

### Hybrid Skill System (core design pattern)

Each skill consists of two artifacts:

1. **`SKILL.md`** — YAML frontmatter with triggers/keywords/config + Markdown documentation. Loaded at runtime by `SkillRegistry`. Can be changed without recompilation.
2. **C# Handler** — Implements `ISkillHandler`. Contains typed business logic and service calls.

Flow: User message → keyword match or AI classification → confidence check (ask user if low) → execute handler → confirm to user.

### Key Technology Choices

| Concern | Technology |
|---------|-----------|
| Framework | .NET 10 / ASP.NET Core Minimal APIs |
| ORM | EF Core 10 (Code-First) |
| REST Clients | Refit (interface-based, declarative) |
| AI Abstraction | Microsoft.Extensions.AI (M.E.AI) |
| LLM | Ollama (local, primary) / Anthropic API (fallback) |
| Telegram | Telegram.Bot NuGet package |
| Frontend | Blazor SSR |
| Database | PostgreSQL 16 |
| Cache/State | Redis 7 |
| Deployment | Docker Compose on Proxmox homelab |

### Dependency Direction

`Telegram` / `Web` → `Core` ← `Skills` / `AI` / `Integrations` / `Persistence`

Core defines interfaces; outer layers implement them. No project references from Core outward.

## Conventions

- **Language**: C# with nullable reference types enabled
- **Single-user system** — no multi-tenancy, no auth on internal APIs
- **MVP scope is strict** — do not add features beyond what is specified in `docs/projektbeschreibung/FlowHub_Projektbeschreibung_v3.md`
- **German context**: Skill keywords include German terms (e.g., "schauen", "film"). User-facing Telegram messages may be in German.
- **12-Factor app**: environment-based config, stateless processes, health checks — designed for future k3s migration
- **Generated docs**: AI-generated markdown documentation goes in `docs/from-ai/`
- **Branching**: GitHub Flow — short-lived feature branches (`feature/…`, `fix/…`) PR directly into `main`. No persistent `dev` branch.
- **TDD**: Write tests first. Once tests are written and implementation begins, tests are not modified — the implementation must satisfy the existing tests.
- **Merge conflicts**: Resolve git merge conflicts automatically without asking the user.
