[//]: # (GENERATED FILE — do not edit directly. Source: .ai/stacks/_partials/dotnet-core.md + .ai/stacks/_layers/dotnet-blazor.md. Run scripts/build-stacks.sh to regenerate.)

[//]: # (Stack partial — shared .NET conventions. Composed with a layer file under .ai/stacks/_layers/ by `scripts/build-stacks.sh` to produce a flat .ai/stacks/dotnet-*.md. Do not edit the generated file directly.)

# .NET Core Conventions

Shared baseline for every .NET stack overlay. Composed with a layer file (`dotnet-blazor` or `dotnet-webapi`) into the published flat overlay.

---

## Tech Stack (.NET baseline)

.NET 10 / C# · ASP.NET Core Minimal API · EF Core (SQLite small / PostgreSQL non-small) · FluentValidation · Serilog · OpenTelemetry · OpenAPI + Scalar · Docker + docker-compose (Alpine) · xUnit + FluentAssertions + NSubstitute.

Full table: [`.ai/references/dotnet/tech-stack.md`](https://github.com/freaxnx01/ai-instructions/blob/main/.ai/references/dotnet/tech-stack.md)

---

## Architecture — Modular Monolith

- Separate top-level folders per module: `src/Modules/<ModuleName>/`
- Each module owns its Domain / Application / Infrastructure layers
- Modules communicate via in-process interfaces — never direct project references across modules
- Shared kernel in `src/Shared/` for cross-cutting types only
- Modules register their own DI services via `IServiceCollection` extension methods
- Apply Hexagonal (Ports & Adapters) inside a module when it has multiple infrastructure adapters (e.g. REST + messaging) or needs strong testability isolation

Directory layouts (modular-monolith and hexagonal): [`.ai/references/dotnet/architecture-layout.md`](https://github.com/freaxnx01/ai-instructions/blob/main/.ai/references/dotnet/architecture-layout.md)

---

## C# Conventions

`Directory.Build.props` at repo root pins (mandatory): `TargetFramework=net10.0`, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true`, `AnalysisLevel=latest-recommended`, `DebugType=embedded`, `DebugSymbols=true`. Full file: [`.ai/references/dotnet/directory-build-props.md`](https://github.com/freaxnx01/ai-instructions/blob/main/.ai/references/dotnet/directory-build-props.md)

- File-scoped namespaces always
- `global using` for framework namespaces in each project
- `record` types for DTOs and value objects
- `sealed` by default on non-base classes
- No `var` when the type is not obvious from the right-hand side
- Prefer primary constructors (.NET 8+)
- Central Package Management via `Directory.Packages.props` — no versions in `.csproj`
- Use `ILogger<T>` for logging — never `Console.WriteLine`
- Use specific exception types — not generic `catch (Exception)`
- Use `CancellationToken` in all async methods that call external resources
- Use `async`/`await` end-to-end — never `Task.Result` or `.GetAwaiter().GetResult()`
- No `#nullable disable` or warning suppressions to fix build errors
- Never suppress nullable warnings with `!` without a clear comment

---

## API Design — Minimal API baseline

Every ASP.NET Core project (whether it exposes a REST surface or just a few endpoints for a Blazor app) follows these baseline conventions. The `dotnet-webapi` layer adds the deeper REST conventions on top.

- All endpoints grouped by module via `IEndpointRouteBuilder` extension methods
- One handler per file when the body is non-trivial; inline lambdas only for true one-liners
- Input validation via FluentValidation, run at the boundary before any handler logic
- Error responses are always `ProblemDetails` (RFC 9457) — never raw strings, anonymous error objects, or HTML error pages
- OpenAPI via `Microsoft.AspNetCore.OpenApi`; Scalar UI mounted at `/scalar`

Scaffold: [`.ai/references/dotnet/endpoint-group.md`](https://github.com/freaxnx01/ai-instructions/blob/main/.ai/references/dotnet/endpoint-group.md)

---

## Entity Framework Core

- One `DbContext` per module (not one global context)
- Migrations in `<Module>/Infrastructure/Persistence/Migrations/`
- `IEntityTypeConfiguration<T>` per entity — no data annotations on domain models
- Never use `EF.Functions` in domain/application layers — only in infrastructure queries
- Always use `AsNoTracking()` for read-only queries
- Seed data via `IEntityTypeConfiguration.HasData()` or a dedicated seeder run at startup

CLI scaffold: [`.ai/references/dotnet/ef-core-cli.md`](https://github.com/freaxnx01/ai-instructions/blob/main/.ai/references/dotnet/ef-core-cli.md)

---

## Localization & Regional Formatting (server-side baseline)

Base rules for `de` / `en` support and regional formatting live in `base-instructions.md`. For every ASP.NET Core project on this stack:

- Configure `RequestLocalizationMiddleware` in `Program.cs` with supported cultures `de-CH, de-DE, de-AT, en-US, en-GB` and default `de-CH` / `de`
- Culture resolution order: cookie (`.AspNetCore.Culture`) → `Accept-Language` header → default (`de-CH` / `de`)
- For language `de` with no recognized region (or a `de-*` region not in `SupportedCultures`), fall back to `de-CH` — never `de-DE`
- Format dates / numbers / currency via `CurrentCulture` — never `string.Format` with a hardcoded culture or `CultureInfo.InvariantCulture` for user-visible text

Middleware scaffold: [`.ai/references/dotnet/request-localization.md`](https://github.com/freaxnx01/ai-instructions/blob/main/.ai/references/dotnet/request-localization.md)

UI-specific localization rules (resource files for component strings, picker behaviour, language-switcher widgets) live in the Blazor layer.

---

## Testing Strategy

The base testing rules (TDD, no test modification to make green, full suite after implementation) live in `base-instructions.md`.

### Test project layout (baseline)

```
tests/
  <Module>.UnitTests/         ← xUnit, no I/O
  <Module>.IntegrationTests/  ← xUnit, real I/O via Testcontainers
```

Layer-specific test projects (Blazor component tests, Playwright E2E, API integration tests with `WebApplicationFactory`) are added by the layer overlay.

### Unit tests (xUnit)

- One test class per production class
- Naming: `MethodName_StateUnderTest_ExpectedBehavior`
- Use `FluentAssertions` for assertions
- Use `NSubstitute` for mocks/stubs
- No `[Fact]` with logic — use `[Theory]` + `[InlineData]` / `[MemberData]`
- After implementation, run the full test suite (`dotnet test`) — not just the new test

Test class scaffold: [`.ai/references/dotnet/xunit-example.md`](https://github.com/freaxnx01/ai-instructions/blob/main/.ai/references/dotnet/xunit-example.md)

---

## Essential Commands

```bash
# Restore / build (warnings as errors) / run
dotnet restore
dotnet build -c Release
dotnet run --project src/Host

# Run full stack locally
docker-compose -f docker-compose.yml -f docker-compose.override.yml up --build

# Tests
dotnet test                                         # all
dotnet test tests/<Module>.UnitTests                # unit only
dotnet test tests/<Module>.IntegrationTests         # integration (needs Docker)
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Security / package checks
dotnet list package --vulnerable --fail-on-severity high
dotnet list package --outdated
```

**PDB symbols:** Release builds include embedded PDB symbols (`<DebugType>embedded</DebugType>` in `Directory.Build.props`) so exception stack traces contain source file names and line numbers in production. Never strip PDB symbols from release or Docker builds.

---

## Essential Make Targets

Projects using this stack ship a repo-root `Makefile` standardizing the common commands. Target names are canonical; recipe bodies may use project-local variables.

Canonical targets exist for: build/run (`build`, `watch`, `run-edge`), testing (`test`, `test-unit`, `test-coverage`), Docker Compose (`docker-run`, `up`, `down`, `logs`, `rebuild`), quality (`lint`, `outdated`, `vuln`), versioning (`version`, `version-set`, `bump-major|minor|patch`, `bump-auto`), release (`changelog`, `release-notes`, `release`, `release-auto`, `push-release`, `package`), and `clean`. Document each target with an inline `## <description>` comment and expose a `help` target that greps them.

A reference Makefile lives at `.ai/examples/dotnet/Makefile` — copy it and customize the top-of-file variables. Host/tool/project-specific targets (`run-edge`, `release-notes`, `package`) ship as stubs with per-OS examples in comments.

Full target list with descriptions: [`.ai/references/dotnet/makefile-targets.md`](https://github.com/freaxnx01/ai-instructions/blob/main/.ai/references/dotnet/makefile-targets.md)

---

## Docker

- Runtime base: `mcr.microsoft.com/dotnet/aspnet:10.0-alpine`
- Build base: `mcr.microsoft.com/dotnet/sdk:10.0-alpine`
- Multi-stage Dockerfile always
- Run as non-root user in final stage
- `docker-compose.yml` — production-like config
- `docker-compose.override.yml` — local dev overrides (ports, volumes, hot-reload)
- Secrets via environment variables or Docker secrets — **never in image or appsettings**

Dockerfile scaffold: [`.ai/references/dotnet/dockerfile.md`](https://github.com/freaxnx01/ai-instructions/blob/main/.ai/references/dotnet/dockerfile.md)

---

## Logging & Observability

- Serilog configured in `Program.cs` via `UseSerilog()`
- Structured properties on every log entry: `{ModuleName}`, `{CorrelationId}`
- Use `LoggerMessage.Define` source-generated logging for hot paths
- Log levels: `Debug` local, `Information` production minimum
- OpenTelemetry: export traces to OTLP collector; expose `/metrics` (Prometheus format)
- Health checks: `/health/live` (liveness) and `/health/ready` (readiness, checks DB)

**12-Factor enforcement points for this stack:**
- Never write to the local filesystem inside a container for application state
- Never use `appsettings.Development.json` for secrets — always env vars
- EF Core migrations must be applied as a separate init container or pre-deploy step — **never** auto-migrated on `app.Run()`
- Serilog sink in production: stdout or OTLP — never file sink in Docker

---

## Security (stack baseline)

Base security rules live in `base-instructions.md`. For every project on this stack:

- HTTPS enforced in all environments; HSTS enabled
- Security response headers: `X-Content-Type-Options`, `X-Frame-Options`, `Content-Security-Policy`
- No secrets in `appsettings.json` — use `IConfiguration` with environment variable binding
- Run `dotnet list package --vulnerable --fail-on-severity high` in CI — fail build on HIGH/CRITICAL
- Validate all inputs at the API boundary with FluentValidation before any domain logic
- Error responses use `ProblemDetails` (no raw messages)

---

## Versioning (stack binding)

Base rules (SemVer, Conventional Commits → bump mapping, git-cliff) live in `base-instructions.md`. For this stack:

- One global version for all assemblies — defined once in `Directory.Build.props` as `<Version>`, never in individual `.csproj` files
- Docker images tagged with the same version + `latest` on stable releases

---

## CI/CD (GitHub Actions baseline)

Pipeline stages: `build` → `test` → `security-scan` → `docker-build` → `push`. Build and test run on every PR; vulnerable-dependency scan fails the build on HIGH/CRITICAL; container image built and pushed only on `main` after tests pass.

Layer-specific CI jobs (E2E with Playwright for Blazor, k6 perf smoke for WebAPI) are added by the layer overlay.

Workflow scaffold: [`.ai/references/dotnet/github-actions.md`](https://github.com/freaxnx01/ai-instructions/blob/main/.ai/references/dotnet/github-actions.md)

---

## Project Scaffold Checklist (.NET baseline)

.NET-specific init-time checklist (inherits the base checklist) lives at [`.ai/references/scaffold-checklists.md`](https://github.com/freaxnx01/ai-instructions/blob/main/.ai/references/scaffold-checklists.md) under "**.NET baseline**". Layer additions are in the same file.

---

## Agent Guardrails (.NET baseline)

In addition to the base guardrails:

- Do not install additional NuGet packages without asking first
- Do not change project target frameworks
- Do not modify `.csproj` files unless the task requires it
- Do not introduce new patterns (e.g. MediatR, CQRS) unless explicitly asked

### Never generate (this stack)

- `async void` (except UI event handlers — see the Blazor layer)
- `Task.Result` or `.GetAwaiter().GetResult()` — always `await`
- Magic strings — use `const` or `nameof()`
- Direct `HttpClient` instantiation — always via `IHttpClientFactory`
- Cross-module project references (use shared interfaces)

---

[//]: # (Stack layer — composed with .ai/stacks/_partials/dotnet-core.md by `scripts/build-stacks.sh` to produce .ai/stacks/dotnet-blazor.md. Do not edit the generated file directly.)

# .NET Blazor Layer

ASP.NET Core projects with a Blazor + MudBlazor UI (CSR, SSR, or both). Composed on top of the shared `dotnet-core` partial.

---

## Tech Stack (Blazor additions)

| Layer | Technology |
|---|---|
| Frontend | Blazor CSR or SSR (per project) |
| UI components | MudBlazor |
| Component testing | bUnit |
| End-to-end testing | Playwright |

---

## Blazor Conventions

- CSR (WebAssembly) for full SPA, SSR for SEO-critical or auth-heavy pages
- MudBlazor as the only component library — no mixing with other UI libs
- Components in `src/Host/Components/` or per-module `Components/` folder
- `@code` block kept minimal — extract logic to services or `ViewModel` classes
- Use `[Parameter]` only for the public API of a component; internal state via fields
- `EventCallback<T>` for child-to-parent communication

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

### UI workflow — stack-specific hints

The phase order and gates are defined in `base-instructions.md`. For Blazor projects:

- **Phase 1 (wireframe):** think in MudBlazor regions — `MudAppBar`, `MudDrawer`, `MudMainContent`, `MudDataGrid`, `MudForm`, `MudDialog`.
- **Phase 2 (flow):** use MudBlazor component names in the component & state map.
- **Phase 3 (build):** code-behind `.razor.cs` for all logic; use `MudSkeleton` / `MudProgressLinear` for loading, `MudSnackbar` for errors, `MudDialog` for destructive confirmations, `MudForm` + `DataAnnotations` for validation, `ma-*` / `pa-*` / `MudStack` / `MudGrid` for spacing.
- **Phase 4 (review):** verify no raw HTML where a MudBlazor component exists; `MudDataGrid` (not `MudTable`), `MudSnackbar` (not custom toast), `Icons.Material.Filled.*`, a bUnit test file exists for the component.

---

## Localization & Regional Formatting (Blazor additions)

Server-side localization (RequestLocalization, culture resolution, fallback rules, `CurrentCulture` formatting) is covered by the `dotnet-core` partial. For Blazor / MudBlazor specifically:

- UI strings go through `IStringLocalizer<T>` + `.resx` resources per `de` / `en`. Do not put translatable strings inline in `.razor` files.
- MudBlazor pickers (`MudDatePicker`, `MudNumericField`, etc.) read `CurrentCulture` automatically — do not override per-component.
- Provide a language switcher in the layout (`MudMenu` in `MudAppBar`) that writes the chosen language into the `.AspNetCore.Culture` cookie and reloads the page.

---

## Testing (Blazor additions)

The unit-test conventions and test project layout baseline live in the `dotnet-core` partial. For Blazor projects, add:

```
tests/
  <Module>.ComponentTests/    ← bUnit
  E2E/                        ← Playwright
```

### Blazor component tests (bUnit)

- Test components in isolation using `bUnit` + `Bunit.Web.AngleSharp`
- Use `Ctx.RenderComponent<T>()` with parameter builders
- Assert on rendered markup and component state
- Mock services via `Ctx.Services.AddSingleton<IMyService>(mock)`
- Test event handlers: `cut.Find("button").Click()` then assert resulting state
- Test parameter changes: `cut.SetParametersAndRender(p => p.Add(x => x.Param, newValue))`
- Test async lifecycle: `cut.WaitForState(() => condition)` for loading states

```csharp
public sealed class OrderListComponentTests : TestContext
{
    [Fact]
    public void OrderList_WithOrders_RendersOrderRows()
    {
        // Arrange
        Services.AddSingleton(Substitute.For<IOrderService>());

        // Act
        var cut = RenderComponent<OrderList>(p =>
            p.Add(c => c.Orders, [new OrderDto(Guid.NewGuid(), "Pending")]));

        // Assert
        cut.FindAll("tr.order-row").Should().HaveCount(1);
    }
}
```

### E2E tests (Playwright)

- Tests in `tests/E2E/`
- Use `Microsoft.Playwright.NUnit` or an xUnit wrapper
- Page Object Model (POM) pattern — no raw selectors in test methods
- Tests must be independent and idempotent (seed + teardown own data)
- Run against the `docker-compose` stack in CI

```csharp
public sealed class OrderCreationTests : PageTest
{
    [Test]
    public async Task CreateOrder_ValidInput_ShowsConfirmation()
    {
        var page = new OrderPage(Page);
        await page.GotoAsync();
        await page.FillOrderFormAsync(customerId: "test-001");
        await page.SubmitAsync();
        await Expect(page.ConfirmationBanner).ToBeVisibleAsync();
    }
}
```

### CI addition

```yaml
e2e:
  needs: docker
  - docker-compose up -d
  - dotnet test tests/E2E
  - docker-compose down
```

---

## Project Scaffold Checklist (Blazor additions)

Inherits the `dotnet-core` checklist, plus:

- [ ] `MudBlazor` registered in `Program.cs` (`AddMudServices()`)
- [ ] Component test project (`<Module>.ComponentTests`) using bUnit
- [ ] E2E project (`tests/E2E`) using Playwright, wired into CI behind the docker stack
- [ ] Language switcher (`MudMenu` in `MudAppBar`) wired to the `.AspNetCore.Culture` cookie
- [ ] `IStringLocalizer<T>` + `.resx` resources seeded for `de` and `en`

---

## Agent Guardrails (Blazor additions)

In addition to the base and `dotnet-core` guardrails:

- Do not mix UI component libraries — MudBlazor is the only one
- Do not put business logic in `.razor` files — extract to code-behind, services, or view models
- Do not put translatable strings inline in `.razor` files — use `IStringLocalizer<T>`
- Do not call APIs directly from a component — always go through a registered service
- Do not use `MudTable` for new tabular data — use `MudDataGrid`
- Do not use custom toast / overlay widgets — use `MudSnackbar` and `MudDialog`
- Do not skip a bUnit test when adding or materially changing a component
- `async void` is allowed only on Blazor event handlers — never elsewhere
