[//]: # (Source of truth: .ai/base-instructions.md — update conventions there first, then reflect changes here)

# SKILL.md — OpenClaw Agent Skill

This skill configures OpenClaw for .NET 10 / ASP.NET Core / Blazor / MudBlazor projects using Modular Monolith architecture.

---

## Skill Identity

**Name:** dotnet-modular-blazor  
**Version:** 1.0.0  
**Language:** C# / .NET 10  
**Domain:** Backend API + Blazor Frontend + EF Core

---

## Activation Triggers

Activate this skill when the task involves any of:

- Creating or modifying `.cs`, `.razor`, `.csproj`, `.sln` files
- ASP.NET Core Minimal API endpoint generation
- Blazor component creation or modification
- Entity Framework Core entity/migration work
- xUnit test generation (unit, integration, bUnit, Playwright)
- Docker / docker-compose file generation
- GitHub Actions CI workflow generation
- Bruno API request collection creation or updates
- Architecture guidance for this stack

---

## Stack Reference

```
Language:       C# 13 / .NET 10
Backend:        ASP.NET Core Minimal API
Frontend:       Blazor CSR or SSR + MudBlazor
ORM:            Entity Framework Core
DB (small):     SQLite
DB (large):     PostgreSQL
Validation:     FluentValidation
Logging:        Serilog (structured)
Observability:  OpenTelemetry
API Docs:       OpenAPI + Scalar
API Testing:    Bruno (collections in bruno/)
Container:      Docker (Alpine), docker-compose
Testing:        xUnit + FluentAssertions + NSubstitute + bUnit + Playwright
```

---

## Code Generation Rules

### Always Apply

- File-scoped namespaces
- Primary constructors for DI
- `sealed` on concrete classes
- `record` for DTOs, commands, queries, value objects
- Nullable reference types — no `!` suppression without comment
- No `var` where type is non-obvious
- `global using` for framework namespaces in each project
- Central Package Management via `Directory.Packages.props` — no versions in `.csproj`
- `ILogger<T>` for logging — never `Console.WriteLine`
- `CancellationToken` in all async methods that call external resources
- Specific exception types — not generic `catch (Exception)`

### Never Generate

- `async void` (except Blazor event handlers)
- `Task.Result` or `.GetAwaiter().GetResult()`
- Magic strings — use `const` or `nameof()`
- Direct `HttpClient` instantiation
- Secrets or connection strings in source files
- Cross-module project references (use shared interfaces)
- Tests that are modified to pass (fix the implementation instead)
- Hardcoded return values, mock results, or stub logic to satisfy a test
- Silently swallowed exceptions to make a test green
- `#nullable disable` or warning suppressions to fix build errors
- Commented-out code blocks — delete them, git has history

---

## Module Structure Template

When generating a new module, use this structure:

```
src/Modules/<ModuleName>/
├── Domain/
│   ├── <Entity>.cs
│   └── Events/
├── Application/
│   ├── Ports/
│   │   ├── Driving/          ← I<ModuleName>Service.cs
│   │   └── Driven/           ← I<ModuleName>Repository.cs
│   ├── UseCases/
│   │   ├── Create<Entity>/
│   │   │   ├── Create<Entity>Command.cs
│   │   │   └── Create<Entity>Handler.cs
│   │   └── Get<Entity>/
│   └── DTOs/
└── Infrastructure/
    ├── Adapters/
    │   └── Persistence/
    │       ├── <ModuleName>DbContext.cs
    │       ├── <Entity>Configuration.cs
    │       └── Migrations/
    └── DependencyInjection.cs
```

---

## Endpoint Pattern

```csharp
// Pattern for Minimal API endpoint class
public static class <Entity>Endpoints
{
    public static IEndpointRouteBuilder Map<Entity>Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/<entity-plural>")
                       .WithTags("<Entity>")
                       .WithOpenApi();

        group.MapPost("/", Create<Entity>Async).WithName("Create<Entity>");
        group.MapGet("/{id:guid}", Get<Entity>ByIdAsync).WithName("Get<Entity>ById");
        group.MapPut("/{id:guid}", Update<Entity>Async).WithName("Update<Entity>");
        group.MapDelete("/{id:guid}", Delete<Entity>Async).WithName("Delete<Entity>");

        return app;
    }

    private static async Task<IResult> Create<Entity>Async(
        Create<Entity>Request request,
        IValidator<Create<Entity>Request> validator,
        I<Entity>Service service,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var result = await service.CreateAsync(request, ct);
        return result.IsSuccess
            ? Results.CreatedAtRoute("Get<Entity>ById", new { id = result.Value.Id }, result.Value)
            : Results.Problem(result.Error);
    }
}
```

---

## Test Generation Rules

### TDD Rules

- After implementation, run the full test suite (`dotnet test`) — not just the new test
- If a test fails after 3 attempts, STOP and explain what's going wrong instead of continuing to iterate

### Unit Test Template

```csharp
public sealed class <Handler>Tests
{
    private readonly I<Repository> _repository = Substitute.For<I<Repository>>();
    private readonly <Handler> _sut;

    public <Handler>Tests() => _sut = new <Handler>(_repository);

    [Fact]
    public async Task Handle_<State>_<Expected>()
    {
        // Arrange

        // Act
        var result = await _sut.Handle(<command>, CancellationToken.None);

        // Assert
        result.<assertion>.Should().<be>();
    }
}
```

### bUnit Component Test Template

```csharp
public sealed class <Component>Tests : TestContext
{
    [Fact]
    public void <Component>_<State>_<Expected>()
    {
        // Arrange
        Services.AddSingleton(Substitute.For<I<Service>>());

        // Act
        var cut = RenderComponent<<Component>>(p =>
            p.Add(c => c.<Parameter>, <value>));

        // Assert
        cut.Find("<selector>").<assertion>;
    }
}
```

**Additional bUnit techniques:**
- Test event handlers: `cut.Find("button").Click()` then assert resulting state
- Test parameter changes: `cut.SetParametersAndRender(p => p.Add(x => x.Param, newValue))`
- Test async lifecycle: use `cut.WaitForState(() => condition)` to handle loading states

### Playwright E2E Template

```csharp
public sealed class <Feature>Tests : PageTest
{
    [Test]
    public async Task <Action>_<State>_<Expected>()
    {
        var page = new <Feature>Page(Page);
        await page.GotoAsync();

        // Act
        await page.<Action>Async();

        // Assert
        await Expect(page.<Element>).ToBeVisibleAsync();
    }
}
```

---

## API Testing (Bruno)

Use [Bruno](https://www.usebruno.com/) for manual and exploratory REST API testing. Collections are stored in `bruno/` at repo root and committed to Git.

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

- One folder per module, mirroring the API route structure
- Request files named with the action: `create-order.bru`, `get-order-by-id.bru`
- Use Bruno environments for base URL and auth tokens — never hardcode URLs or secrets in `.bru` files
- Keep requests in sync with endpoints — when adding/changing an API endpoint, update or add the corresponding Bruno request
- Include example request bodies with realistic test data
- Add assertions in Bruno where useful (status code, response shape)

---

## EF Core Rules

- One `DbContext` per module
- Entity configuration via `IEntityTypeConfiguration<T>` only
- No data annotations on domain models
- `AsNoTracking()` on all read queries
- Never use `EF.Functions` in domain/application layers — only in infrastructure queries
- Seed data via `IEntityTypeConfiguration.HasData()` or dedicated seeder run at startup
- Migrations in `Infrastructure/Persistence/Migrations/`

---

## Blazor Rules

- MudBlazor only — no other UI libraries
- Keep `@code` blocks minimal
- Extract logic to services or ViewModels
- `EventCallback<T>` for child-to-parent events
- `[Parameter]` only for public component API

### MudBlazor Conventions

- Prefer MudBlazor components over raw HTML at all times
- Use `MudDataGrid` for tabular data (not `MudTable` unless legacy)
- Use `MudForm` + `MudTextField` / `MudSelect` for forms with validation
- Use `MudDialog` for confirmations and modals (not custom overlays)
- Use `MudSnackbar` for user feedback / toast messages
- Use `MudSkeleton` for loading states
- Layout: `MudLayout` → `MudAppBar` + `MudDrawer` + `MudMainContent`
- Icons: use `Icons.Material.Filled.*` consistently

### Component Conventions

- One component per file
- Component files: `PascalCase.razor`
- Code-behind files: `PascalCase.razor.cs` (partial class)
- Services injected via `@inject` or constructor in code-behind
- No business logic in `.razor` files — only binding and UI events
- Reuse components from `/src/Shared/` before creating new ones

### State & Data Flow

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

Skill files located in `.ai/skills/`.

---

## Versioning Rules

- [SemVer 2.0.0](https://semver.org/): `MAJOR.MINOR.PATCH`
- One global version for all assemblies — defined once in `Directory.Build.props` as `<Version>`, never in individual `.csproj` files
- Conventional Commits drive version bumps — **git-cliff** is the changelog and release notes tool:

| Commit | Bump |
|---|---|
| `BREAKING CHANGE:` footer or `!` after type | MAJOR |
| `feat` | MINOR |
| `fix`, `perf` | PATCH |
| all others | none |

- Git tags: `v<MAJOR>.<MINOR>.<PATCH>` on `main` after merge
- Docker images: tagged with version + `latest` on stable

---

## Changelog Rules

- `CHANGELOG.md` in repo root — [Keep a Changelog](https://keepachangelog.com) format
- Always maintain an `[Unreleased]` section
- Sections per release: `Added` · `Changed` · `Deprecated` · `Removed` · `Fixed` · `Security`
- Auto-generate with `git cliff --output CHANGELOG.md` from Conventional Commits (`cliff.toml` in repo root)
- CI integration: `orhun/git-cliff-action` in GitHub Actions generates release notes into GitHub Releases
- When generating PR descriptions or release notes, follow this format

---

## 12-Factor Compliance Checklist

When generating or reviewing code, verify:

- [ ] Config: no env-specific values in `appsettings.json` — env vars only
- [ ] Logs: Serilog sink is stdout (not file) in Docker context
- [ ] Stateless: no writes to local filesystem for app state
- [ ] Migrations: not called inside `app.Run()` — separate init container or script
- [ ] Backing services: DB/cache/broker via connection string env vars, swappable
- [ ] Build/release/run: multi-stage Dockerfile, no build step in running container

---

## Commit & Branch Conventions

### Branch naming

```
feature/<issue-id>-short-description
fix/<issue-id>-short-description
chore/<description>
```

### Commit format (Conventional Commits)

```
<type>(<scope>): <summary>

[body: why, not what]

[Closes #<issue>]
```

Types: `feat` `fix` `test` `refactor` `chore` `docs` `ci` `perf`

---

## PR Checklist (generate or verify)

- [ ] Unit tests added/updated
- [ ] bUnit component tests if Blazor changes
- [ ] E2E test if user-facing flow changed
- [ ] No vulnerable packages (`dotnet list package --vulnerable`)
- [ ] No secrets committed
- [ ] Migrations included if schema changed
- [ ] OpenAPI spec valid

---

## Build Conventions

- Release builds include embedded PDB symbols (`<DebugType>embedded</DebugType>` in `Directory.Build.props`)
- This ensures exception stack traces contain source file names and line numbers in production
- Never strip PDB symbols from release or Docker builds

---

## Docker Conventions

- Runtime: `mcr.microsoft.com/dotnet/aspnet:10.0-alpine`
- SDK: `mcr.microsoft.com/dotnet/sdk:10.0-alpine`
- Multi-stage build always
- Non-root user in final stage
- Secrets via environment variables — never baked in

---

## Security Checklist (for code review tasks)

- [ ] No secrets in source or config files
- [ ] All inputs validated with FluentValidation
- [ ] Error responses use ProblemDetails (no raw messages)
- [ ] HTTPS enforced
- [ ] No direct EF queries in domain layer
- [ ] Vulnerable package check passes

---

## Documentation Structure

```
docs/
├── design/                    ← UI wireframes & Mermaid flows per feature
│   └── <feature-name>/
│       ├── wireframe.md       ← Phase 1 output (ASCII wireframe)
│       └── flow.md            ← Phase 2 output (Mermaid diagrams)
├── adr/                       ← Architecture Decision Records
└── ai-notes/                  ← AI agent working notes
```

- `README.md` and `CHANGELOG.md` live in the repo root
- `bruno/` in repo root for Bruno API request collections
- UI design artifacts are saved per feature during the UI workflow phases
- AI agents write working notes to `docs/ai-notes/`, not `.ai/`
- `.ai/` is reserved for agent instructions and skill files only

---

## Agent Guardrails

- Do not install additional NuGet packages without asking first
- Do not change project target frameworks
- Do not modify `.csproj` files unless the task requires it
- Do not introduce new patterns (e.g. MediatR, CQRS) unless explicitly asked
- Do not touch files outside the scope of the current task
- Keep changes minimal and focused — do not refactor unrelated code unless asked
- If a test fails after 3 attempts, STOP and explain what's going wrong

---

## Observability Checklist

- [ ] Serilog structured logging with `{ModuleName}` + `{CorrelationId}`
- [ ] OpenTelemetry traces wired
- [ ] `/health/live` and `/health/ready` endpoints exist
- [ ] `/metrics` Prometheus endpoint exposed
