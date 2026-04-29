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

The CAS coursework, FlowHub concept, and project decisions live in an Obsidian vault that is now part of this repo (subtree-merged from the former `gitlab.freaxnx01.ch/freax/obsidian-cas-aise` repo, which has been retired). The vault is the source of truth for everything **except** code.

- **Path:** `vault/` at the repo root.
- **Mode:** Editable as part of this repo — same commit/push workflow as the rest of FlowHub. The vault keeps its own `CLAUDE.md` describing tagging and per-folder conventions.
- **Back-link:** `vault/Projektarbeit/Repository.md` (kept as historical context; no longer required since the vault and code share a repo).

**Primary focus paths** — grep/read these first when project background is needed:

- `vault/Projektarbeit/` — thesis & FlowHub concept (`Idee FlowHub.md`, `Dev.md`, `Skills.md`, `External Services.md`)
- `vault/Blöcke/` — course block notes that drive the incremental build

**Secondary** (search on demand): `vault/Knowledge/`, `vault/Allgemein/`, `vault/Organisation/`, `vault/Notes.md`, `vault/TODO.md`.

**Read trigger:** Before answering questions about CAS scope, modules, deadlines, project decisions, or the FlowHub concept, grep `vault/` first.

---

## Grading — Bewertungskriterien Are Always In Scope

The CAS project is graded against a fixed Moodle rubric (18 scored items, max 100 pts). The canonical source is **`vault/Organisation/Bewertungskriterien.md`** — every Block-Nachbereitung file mirrors the relevant subset as a checklist with point weights.

**Rule:** When the current date falls in any Block-Nachbereitung phase (see `.ai/cas-instructions.md` → Block Schedule), the rubric is **active context**, not background. Specifically:

- Before claiming a Block-Nachbereitung is "done", invoke the `cas-aise-grade-self-check` skill (from the `freax-claude-code-plugins` marketplace) — or manually walk every item in `vault/Organisation/Bewertungskriterien.md` — and verify each one has a deliverable.
- The KI / Sub-Systeme / Reflexion bucket carries 30 of 100 points — the highest-weighted single item is "Wurden KI-unterstützende Werkzeuge verwendet und deren Nutzung beschrieben" (max 12 pts). Keep `docs/ai-usage.md` (or `docs/insights/block-N.md`) current as work happens — not as a one-shot at submission.
- The Quarkus / Jakarta-EE programming criterion (max 10 pts) is **N/A for FlowHub's .NET stack** and is consciously skipped — note this in the submission PDF rather than stretching coverage.

This applies to *every* Block, not only the final one — each block's work contributes to the final grade.

---

## Essential Commands

### Make targets (preferred)

The repo ships a `Makefile` with the common dev tasks. Plain `make` with no target prints help.

```bash
make            # show help
make run        # run FlowHub.Web on http://localhost:5070 (no hot reload)
make watch      # run FlowHub.Web with hot reload (dotnet watch)
make build      # build the full solution
make test       # run all tests
make test-watch # watch component tests
make restore    # restore NuGet packages
make clean      # remove build artifacts
make format     # apply dotnet format
```

`make run` and `make watch` set `ASPNETCORE_URLS=http://localhost:5070` and pass `--no-launch-profile` so they ignore `launchSettings.json`. Use these in preference to running `dotnet run` directly — the paths and env vars stay consistent.

### Underlying dotnet commands

```bash
# Restore dependencies
dotnet restore FlowHub.slnx

# Build (warnings as errors per Directory.Build.props)
dotnet build FlowHub.slnx

# Run FlowHub.Web directly
dotnet run --project source/FlowHub.Web --no-launch-profile
```

**PDB symbols:** Release builds include embedded PDB symbols (`<DebugType>embedded</DebugType>` in `Directory.Build.props`) so that exception stack traces contain source file names and line numbers in production.

### Testing

```bash
# All tests
dotnet test FlowHub.slnx

# Specific test project
dotnet test tests/FlowHub.Web.ComponentTests

# With coverage
dotnet test FlowHub.slnx --collect:"XPlat Code Coverage" --results-directory ./coverage
```

Test naming convention: `MethodName_StateUnderTest_ExpectedBehavior` (CA1707 is suppressed for test projects so the underscores compile).

### Database Migrations

> ⏳ **Block 4** — EF Core persistence isn't wired yet. The commands below describe the eventual workflow once `FlowHub.Persistence` lands in Block 4. Currently all data is in-memory Bogus stubs.

```bash
# Add migration
dotnet ef migrations add <MigrationName> \
  --project source/FlowHub.Persistence \
  --startup-project source/FlowHub.Web

# Apply to local DB
dotnet ef database update \
  --project source/FlowHub.Persistence \
  --startup-project source/FlowHub.Web

# Generate SQL script (for production review)
dotnet ef migrations script \
  --project source/FlowHub.Persistence \
  --startup-project source/FlowHub.Web \
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

The repo follows a flat **`source/FlowHub.<Capability>/`** layout, not the Modular Monolith `src/Modules/<Module>/` pattern described in the upstream template. This decision is recorded in **ADR 0001** (Q1 = keep flat). Several `source/FlowHub.*/` and `tests/FlowHub.*Tests/` folders are placeholders for capabilities that land in later blocks.

```
.
├── source/
│   ├── FlowHub.Core/                  ← domain types + driving ports (Capture, Skill, Health…)
│   ├── FlowHub.Web/                   ← Blazor Web App, Interactive Server (per ADR 0001)
│   │   ├── Auth/DevAuthHandler.cs     ← dev-only auth bypass (real OIDC in Block 5)
│   │   ├── Components/
│   │   │   ├── App.razor, Routes.razor, _Imports.razor
│   │   │   ├── Layout/                ← MainLayout, QuickCaptureField
│   │   │   ├── Pages/                 ← @page components (Dashboard.razor at /)
│   │   │   ├── DashboardCards/        ← page-specific cards
│   │   │   └── Shared/                ← reusable cross-page components (LifecycleBadge, HealthDot)
│   │   ├── Stubs/                     ← Bogus-backed stub services for Block 2
│   │   └── Program.cs
│   ├── FlowHub.AI/                    ← (placeholder — AI classification, future block)
│   ├── FlowHub.Integrations/          ← (placeholder — Wallabag, Wekan, Vikunja, …)
│   ├── FlowHub.Persistence/           ← (placeholder — EF Core, Block 4)
│   ├── FlowHub.Skills/                ← (placeholder — Skill implementations)
│   └── FlowHub.Telegram/              ← (placeholder — Telegram channel)
├── tests/
│   ├── FlowHub.Web.ComponentTests/    ← bUnit + xunit + FluentAssertions + NSubstitute
│   ├── FlowHub.Core.Tests/            ← (placeholder)
│   ├── FlowHub.Integrations.Tests/    ← (placeholder)
│   └── FlowHub.Skills.Tests/          ← (placeholder)
├── poc/
│   ├── FlowHub.AI.Classification/     ← standalone POC, has its own .sln
│   └── FlowHub-CAS-AISE.sln           ← POC-only solution (not the root sln)
├── docs/
│   ├── adr/                           ← Architecture Decision Records (ADR 0001 = Frontend)
│   ├── design/<feature>/              ← UI workflow output
│   │   ├── wireframe.md               ← Phase 1 output (/ui-brainstorm)
│   │   └── flow.md                    ← Phase 2 output (/ui-flow)
│   ├── superpowers/specs/             ← brainstorming design specs
│   ├── superpowers/plans/             ← implementation plans
│   └── from-ai/                       ← AI agent working notes
├── .ai/
│   ├── base-instructions.md           ← canonical conventions reference
│   ├── cas-instructions.md            ← CAS course rhythm and grading
│   └── skills/                        ← /commit, /push, /flowhub-*, /ui-*, /update-ai-instructions
├── .claude/commands/                   ← Claude Code slash command shims
├── .github/
│   ├── copilot-instructions.md
│   └── workflows/
├── global.json                         ← .NET 10 SDK pin
├── Directory.Build.props               ← nullable, warnings as errors, embedded PDB
├── Directory.Packages.props            ← central package management
├── FlowHub.slnx                        ← root solution (new XML format)
├── Makefile                            ← dev task targets (run, watch, build, test, …)
├── CLAUDE.md                           ← this file
├── README.md
└── SKILL.md                            ← OpenClaw skill definition
```

**Note:** The `## Docker` section further down describes a `docker-compose.yml`-based workflow that lands in **Block 5 (Deployment)** — those files don't exist yet. Current dev workflow is `make run` / `make watch` directly against the host machine.

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
