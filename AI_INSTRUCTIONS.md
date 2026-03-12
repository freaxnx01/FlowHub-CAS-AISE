# AI Coding Agent Template

Template repository for .NET 10 / ASP.NET Core / Blazor / MudBlazor projects with full AI agent configuration.

## Agent Files

| File | Tool | Purpose |
|---|---|---|
| `.ai/base-instructions.md` | All | Canonical conventions — single source of truth |
| `.github/copilot-instructions.md` | GitHub Copilot | Coding style, patterns, anti-patterns |
| `CLAUDE.md` | Claude Code | Commands, structure, architecture context |
| `SKILL.md` | OpenClaw | Skill definition, code generation templates |

## How to Use

1. Clone or use as template
2. Fill in the `<!-- TODO -->` sections in `CLAUDE.md` (project name, purpose, env vars)
3. Update `Directory.Packages.props` with current package versions
4. Adjust `docker-compose.yml` for your services
5. All agent files are ready to use as-is for the tech stack

## Tech Stack

- .NET 10 / C# 13
- ASP.NET Core Minimal API
- Blazor CSR/SSR + MudBlazor
- Entity Framework Core (SQLite / PostgreSQL)
- xUnit + FluentAssertions + NSubstitute + bUnit + Playwright
- Serilog + OpenTelemetry
- Docker + Alpine base images

## Architecture

Modular Monolith by default. Hexagonal (Ports & Adapters) within modules where appropriate.

See `.ai/base-instructions.md` for full conventions reference.
