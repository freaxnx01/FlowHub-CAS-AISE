[//]: # (Source of truth: .ai/base-instructions.md — update conventions there first, then reflect changes here)

# CLAUDE.md

Agent context for Claude Code. Read this before taking any action in this repository.

---

## Project Overview

**Name:** FlowHub
**Purpose:** Project work for CAS AISE — incremental full-stack .NET app built across 5 course blocks
**Architecture:** Modular Monolith (Hexagonal within modules where needed)
**Status:** Active development
**Course context:** See [`.ai/cas-instructions.md`](.ai/cas-instructions.md) for block schedule, implementation rhythm, and grading criteria

---

## Knowledge Base (Obsidian Vault)

The CAS coursework, FlowHub concept, and project decisions live in a separate Obsidian vault. This vault is the source of truth for everything **except** code.

- **Path:** `/mnt/c/Users/freax/Documents/Obsidian/cas-aise/`
- **Mode:** Read-only from this repo. Writes happen only when CWD is the vault (per the global vault convention in `~/.claude/CLAUDE.md`).
- **Back-link:** `Projektarbeit/Repository.md` in the vault points back here.

**Primary focus paths** — grep/read these first when project background is needed:

- `Projektarbeit/` — thesis & FlowHub concept (`Idee FlowHub.md`, `Dev.md`, `Skills.md`, `External Services.md`)
- `Blöcke/` — course block notes that drive the incremental build

**Secondary** (search on demand): `Knowledge/`, `Allgemein/`, `Organisation/`, `Notes.md`, `TODO.md`.

**Read trigger:** Before answering questions about CAS scope, modules, deadlines, project decisions, or the FlowHub concept, grep the vault first.

---

## Essential Commands

### Build & Run

```bash
# Restore dependencies
dotnet restore

# Build (warnings as errors)
dotnet build -c Release

# Run locally (with override for dev DB/ports)
docker-compose -f docker-compose.yml -f docker-compose.override.yml up --build

# Run host directly
dotnet run --project src/Host
```

**PDB symbols:** Release builds include embedded PDB symbols (`<DebugType>embedded</DebugType>` in `Directory.Build.props`) so that exception stack traces contain source file names and line numbers in production.

### Testing

```bash
# All tests
dotnet test

# Unit tests only
dotnet test tests/<Module>.UnitTests

# Integration tests (requires Docker for Testcontainers)
dotnet test tests/<Module>.IntegrationTests

# Blazor component tests
dotnet test tests/<Module>.ComponentTests

# E2E (requires running stack)
docker-compose -f docker-compose.yml -f docker-compose.override.yml up -d
dotnet test tests/E2E
docker-compose down

# With coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
```

### Database Migrations

```bash
# Add migration (replace <Module> and <MigrationName>)
dotnet ef migrations add <MigrationName> \
  --project src/Modules/<Module>/Infrastructure \
  --startup-project src/Host

# Apply to local DB
dotnet ef database update \
  --project src/Modules/<Module>/Infrastructure \
  --startup-project src/Host

# Generate SQL script (for production review)
dotnet ef migrations script \
  --project src/Modules/<Module>/Infrastructure \
  --startup-project src/Host \
  --output migrations.sql
```

### Security & Package Checks

```bash
# Check for vulnerable packages (fail on high/critical)
dotnet list package --vulnerable --fail-on-severity high

# Outdated packages
dotnet list package --outdated
```

---

## Repository Structure

```
.
├── src/
│   ├── Modules/
│   │   └── <ModuleName>/
│   │       ├── Domain/
│   │       ├── Application/
│   │       │   ├── Ports/
│   │       │   │   ├── Driving/
│   │       │   │   └── Driven/
│   │       │   └── UseCases/
│   │       └── Infrastructure/
│   │           └── Persistence/
│   │               └── Migrations/
│   ├── Shared/
│   └── Host/                    ← ASP.NET Core entry point
├── tests/
│   ├── <Module>.UnitTests/
│   ├── <Module>.IntegrationTests/
│   ├── <Module>.ComponentTests/  ← bUnit
│   └── E2E/                      ← Playwright
├── bruno/                         ← Bruno API request collections
├── docs/
│   ├── design/                    ← UI wireframes & Mermaid flows per feature
│   │   └── <feature-name>/
│   │       ├── wireframe.md       ← Phase 1 output (ASCII wireframe)
│   │       └── flow.md            ← Phase 2 output (Mermaid diagrams)
│   ├── adr/                       ← Architecture Decision Records
│   └── ai-notes/                  ← AI agent working notes
├── .ai/
│   ├── base-instructions.md      ← canonical conventions reference
│   └── skills/
│       ├── commit.md             ← /commit slash command
│       ├── push.md               ← /push slash command
│       ├── ui-brainstorm.md      ← Phase 1: wireframe
│       ├── ui-flow.md            ← Phase 2: Mermaid flows
│       ├── ui-build.md           ← Phase 3: build
│       └── ui-review.md          ← Phase 4: review
├── .github/
│   ├── copilot-instructions.md
│   └── workflows/
├── docker-compose.yml
├── docker-compose.override.yml
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── CLAUDE.md                     ← this file
├── CHANGELOG.md                  ← Keep a Changelog format
├── README.md                     ← repo root
└── SKILL.md                      ← OpenClaw
```

---

## Architecture Decisions

### Modular Monolith

- Each module is self-contained: Domain, Application, Infrastructure
- Cross-module communication: in-process interfaces defined in `src/Shared/`
- No direct project references between modules
- Modules register their own DI services via `IServiceCollection` extension methods

### Hexagonal Architecture (within modules)

Apply when a module has multiple infrastructure adapters or needs strong testability isolation.

- Driving (inbound) ports: what the outside world calls into the module
- Driven (outbound) ports: what the module calls out to (DB, messaging, HTTP)
- Adapters live in `Infrastructure/Adapters/`

### API

- Minimal API endpoints, registered per module
- FluentValidation at the boundary — domain stays clean
- ProblemDetails (RFC 9457) for all errors
- OpenAPI via `Microsoft.AspNetCore.OpenApi`, Scalar UI at `/scalar`

### Blazor + MudBlazor

- MudBlazor only — no other component libraries
- CSR for full SPA scenarios, SSR for auth-heavy or SEO-critical pages
- bUnit for component testing in isolation

#### MudBlazor Conventions

- Prefer MudBlazor components over raw HTML at all times
- Use `MudDataGrid` for tabular data (not `MudTable` unless legacy)
- Use `MudForm` + `MudTextField` / `MudSelect` for forms with validation
- Use `MudDialog` for confirmations and modals (not custom overlays)
- Use `MudSnackbar` for user feedback / toast messages
- Use `MudSkeleton` for loading states
- Layout: `MudLayout` → `MudAppBar` + `MudDrawer` + `MudMainContent`
- Icons: use `Icons.Material.Filled.*` consistently

#### Component Conventions

- One component per file
- Component files: `PascalCase.razor`
- Code-behind files: `PascalCase.razor.cs` (partial class)
- Services injected via `@inject` or constructor in code-behind
- No business logic in `.razor` files — only binding and UI events
- Reuse components from `/src/Shared/` before creating new ones

#### State & Data Flow

- Components do not call APIs directly — always go through a service
- Services are registered in `Program.cs` with appropriate lifetime
- Use `EventCallback` for child→parent communication
- Use `CascadingParameter` only for truly global state (e.g. auth, theme)

---

## UI Development Workflow (Mandatory Phase Order)

**Never skip phases. Never write component code before wireframe approval.**

| Phase | Skill | Gate |
|---|---|---|
| 1 — Brainstorm | `/ui-brainstorm` | ASCII wireframe approved |
| 2 — Flow | `/ui-flow` | Mermaid diagrams approved |
| 3 — Build | `/ui-build` | Shell → logic → interactions → polish |
| 4 — Review | `/ui-review` | Checklist passes |

Skill files: `.ai/skills/ui-brainstorm.md`, `ui-flow.md`, `ui-build.md`, `ui-review.md`

### What to Check Before Writing UI Code

- [ ] Does a similar component already exist in `/src/Shared/`?
- [ ] Has the ASCII wireframe been approved?
- [ ] Has the Mermaid flow been approved?
- [ ] Are you building the shell first (no business logic yet)?
- [ ] Does the component need a bUnit test?

---

## Testing Rules — Non-Negotiable

1. **Write the failing test first** — then implement
2. **Never modify a test to make it green** — fix the implementation
3. **No shortcuts**: no `// TODO: test later`, no empty test bodies
4. **Never hardcode return values, mock results, or stub logic** to satisfy a test
5. **Never silently swallow exceptions** to make a test green
6. **After implementation, run the full test suite** (`dotnet test`) — not just the new test
7. **If a test fails after 3 attempts, STOP** and explain what's going wrong instead of continuing to iterate
8. Test naming: `MethodName_StateUnderTest_ExpectedBehavior`
9. E2E tests must be idempotent — seed and clean up their own data

---

## Environment Variables

| Variable | Description | Required |
|---|---|---|
| `ConnectionStrings__Default` | DB connection string | Yes |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | OpenTelemetry collector | No (local) |
| `Serilog__MinimumLevel` | Log level override | No |

Never add secrets to `appsettings.json`. Use environment variables or Docker secrets.

---

## Docker

```bash
# Build image
docker build -t <image-name>:local .

# Start full stack (local)
docker-compose -f docker-compose.yml -f docker-compose.override.yml up --build

# Stop and clean volumes
docker-compose down -v
```

- Runtime base: `mcr.microsoft.com/dotnet/aspnet:10.0-alpine`
- Build base: `mcr.microsoft.com/dotnet/sdk:10.0-alpine`
- Runs as non-root user (`appuser`)

---

## Versioning

This project follows [SemVer 2.0.0](https://semver.org/). One global version for all assemblies, defined once in `Directory.Build.props`:

```xml
<Version>1.0.0</Version>
```

- **Single version** — never set `<Version>` in individual `.csproj` files; all assemblies inherit from `Directory.Build.props`
- Git tag on every release: `v<MAJOR>.<MINOR>.<PATCH>`
- Docker images tagged with same version + `latest` on stable
- Conventional Commits drive the bump: `feat` → MINOR · `fix`/`perf` → PATCH · `BREAKING CHANGE:` footer → MAJOR

```bash
# Tag a release
git tag -a v1.2.0 -m "release: v1.2.0"
git push origin v1.2.0

# Generate changelog with git-cliff
git cliff --output CHANGELOG.md
```

---

## Changelog

`CHANGELOG.md` in repo root following [Keep a Changelog](https://keepachangelog.com) format.

- `[Unreleased]` section accumulates changes until a release is cut
- Auto-generated via **git-cliff** from Conventional Commits (`cliff.toml` in repo root)
- CI integration: `orhun/git-cliff-action` in GitHub Actions generates release notes into GitHub Releases
- CI blocks release branches if `[Unreleased]` is empty

---

## 12-Factor Compliance

See [12factor.net](https://www.12factor.net/). Critical rules for this repo:

- **Config (III):** All env-specific config via environment variables — nothing per-environment in `appsettings.json`
- **Logs (XI):** Serilog writes to **stdout only** in Docker — no file sinks inside containers
- **Processes (VI):** Stateless app — no local file state, no sticky sessions
- **Migrations (XII):** EF Core migrations run as a separate init container or pre-deploy step — **never** auto-migrate inside `app.Run()`
- **Build/Release/Run (V):** Multi-stage Docker enforces separation — never build inside a running container
- **Backing services (IV):** DB, cache, messaging treated as attached resources via env var connection strings

---

## Branching & Git

- Branch from `main`, PR back to `main`
- Squash or rebase merge — no merge commits
- Delete branch after merge

### Commit Messages (Conventional Commits)

```
feat(orders): add cancellation endpoint
fix(auth): handle expired token edge case
test(catalog): add handler unit tests
refactor(shared): extract correlation ID middleware
```

Types: `feat` `fix` `test` `refactor` `chore` `docs` `ci` `perf`

---

## Clean Code Principles

- **Small methods** — each method does one thing at one level of abstraction; aim for ≤20 lines
- **Guard clauses** — validate and return/throw early at the top; avoid nested `if/else` pyramids
- **Command-Query Separation** — a method either performs an action (command, returns `void`/`Task`) or returns data (query), never both
- **No flag arguments** — avoid `bool` parameters that switch behaviour; split into two clearly named methods instead
- **Meaningful names** — names reveal intent; no abbreviations (`cnt`, `mgr`, `svc`) except universally understood ones (`id`, `url`, `dto`)
- **One level of abstraction per method** — don't mix high-level orchestration with low-level detail in the same method; extract helpers
- **Fail fast** — detect invalid state as early as possible and throw specific exceptions; don't let bad data travel deep into the call stack
- **DRY (Don't Repeat Yourself)** — if the same logic exists in two places, extract it; but prefer duplication over the wrong abstraction — wait until the pattern is clear before generalising
- **No dead code** — delete unreachable branches, unused parameters, and vestigial methods; git has history

---

## Common Pitfalls — Avoid These

- `Task.Result` / `.GetAwaiter().GetResult()` — always `await`
- `async void` outside Blazor event handlers
- Magic strings — use `const` or `nameof()`
- Direct `HttpClient` instantiation — use `IHttpClientFactory`
- Suppressions of nullable warnings with `!` without a clear comment
- `#nullable disable` or warning suppressions to fix build errors
- Cross-module project references — use shared interfaces
- Secrets in source files or appsettings
- `Console.WriteLine` — use `ILogger<T>` always
- Generic `catch (Exception)` — use specific exception types
- Missing `CancellationToken` on async methods that call external resources
- Commented-out code blocks — delete them, git has history

---

## Agent Guardrails

- Do not install additional NuGet packages without asking first
- Do not change project target frameworks
- Do not modify `.csproj` files unless the task requires it
- Do not introduce new patterns (e.g. MediatR, CQRS) unless explicitly asked
- Do not touch files outside the scope of the current task
- Keep changes minimal and focused — do not refactor unrelated code unless asked

---

## Key Dependencies (from Directory.Packages.props)

<!-- Update versions as packages are updated in the project -->

| Package | Purpose |
|---|---|
| `FluentValidation.AspNetCore` | Input validation |
| `FluentAssertions` | Test assertions |
| `NSubstitute` | Mocking |
| `xunit` | Test framework |
| `bunit` | Blazor component testing |
| `Microsoft.Playwright` | E2E testing |
| `MudBlazor` | UI component library |
| `Serilog.AspNetCore` | Structured logging |
| `OpenTelemetry.AspNetCore` | Traces + metrics |
| `Microsoft.EntityFrameworkCore` | ORM |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | PostgreSQL driver |

---

## Health Endpoints

| Endpoint | Purpose |
|---|---|
| `/health/live` | Liveness (always 200 if process up) |
| `/health/ready` | Readiness (checks DB, dependencies) |
| `/scalar` | API documentation |
| `/metrics` | Prometheus metrics |

---

## API Testing (Bruno)

Use [Bruno](https://www.usebruno.com/) for manual and exploratory REST API testing. Collections are stored in `bruno/` at repo root and committed to Git.

### Collection structure

```
bruno/
├── bruno.json                     ← collection config
├── environments/
│   ├── local.bru                  ← http://localhost:<port>
│   └── staging.bru
└── <module>/
    ├── create-<entity>.bru
    ├── get-<entity>-by-id.bru
    ├── update-<entity>.bru
    └── delete-<entity>.bru
```

### Conventions

- One folder per module, mirroring the API route structure
- Request files named with the action: `create-order.bru`, `get-order-by-id.bru`
- Use Bruno environments for base URL and auth tokens — never hardcode URLs or secrets in `.bru` files
- Keep requests in sync with endpoints — when adding/changing an API endpoint, update or add the corresponding Bruno request
- Include example request bodies with realistic test data
- Add assertions in Bruno where useful (status code, response shape)
