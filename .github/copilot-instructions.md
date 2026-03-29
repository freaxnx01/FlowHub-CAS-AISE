[//]: # (Source of truth: .ai/base-instructions.md — update conventions there first, then reflect changes here)

# GitHub Copilot Instructions

This project uses .NET 10 / C# with ASP.NET Core Minimal API, Blazor (MudBlazor), Entity Framework Core, and a Modular Monolith architecture. Follow all conventions below when generating or completing code.

---

## Language & Style

- C# 13, .NET 10 target framework
- File-scoped namespaces always: `namespace MyApp.Modules.Orders.Domain;`
- Primary constructors for classes with injected dependencies
- `sealed` on all concrete classes unless designed for inheritance
- `record` for DTOs, commands, queries, value objects
- `global using` for framework namespaces at the project level
- Nullable reference types enabled — never suppress with `!` unless genuinely safe
- No `var` when the type is not obvious from the right-hand side
- Prefer expression-bodied members for single-line getters/small methods

---

## Architecture Rules

- **Modular Monolith**: one folder per module under `src/Modules/<Name>/`
- Layers within a module: `Domain`, `Application`, `Infrastructure`
- Modules must not reference each other's projects directly — use shared interfaces in `src/Shared/`
- Apply **Hexagonal (Ports & Adapters)** within a module when it has multiple infrastructure adapters or requires strong isolation
- Driving ports (inbound) in `Application/Ports/Driving/`
- Driven ports (outbound) in `Application/Ports/Driven/`

---

## Minimal API Endpoints

Always register endpoints via extension method on `IEndpointRouteBuilder`:

```csharp
public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/orders")
                       .WithTags("Orders")
                       .WithOpenApi();

        group.MapPost("/", CreateOrderAsync).WithName("CreateOrder");
        group.MapGet("/{id:guid}", GetOrderByIdAsync).WithName("GetOrderById");

        return app;
    }
}
```

- Route prefix: `/api/v1/<module>`
- Errors: always `Results.Problem(...)` (RFC 9457 ProblemDetails) — never plain strings
- Validate input with FluentValidation before handler logic

---

## Testing — TDD, Tests First

**Critical rule: never modify a test to make it green. Fix the implementation.**

- Framework: xUnit
- Assertions: FluentAssertions
- Mocking: NSubstitute
- Naming: `MethodName_StateUnderTest_ExpectedBehavior`
- One test class per production class
- `[Theory]` + `[InlineData]` / `[MemberData]` for parameterised tests — no logic in `[Fact]`

```csharp
public sealed class CreateOrderHandlerTests
{
    private readonly IOrderRepository _repository = Substitute.For<IOrderRepository>();
    private readonly CreateOrderHandler _sut;

    public CreateOrderHandlerTests() => _sut = new CreateOrderHandler(_repository);

    [Fact]
    public async Task Handle_ValidCommand_CreatesAndPersistsOrder()
    {
        var command = new CreateOrderCommand(Guid.NewGuid(), []);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }
}
```

---

## Blazor Components (MudBlazor)

- MudBlazor is the only UI component library — do not introduce others
- Keep `@code` blocks minimal — logic goes in services or ViewModel classes
- Use `[Parameter]` only for the public API of the component
- `EventCallback<T>` for child-to-parent communication
- Component tests use bUnit:

```csharp
public sealed class OrderListTests : TestContext
{
    [Fact]
    public void OrderList_WithOrders_RendersRows()
    {
        Services.AddSingleton(Substitute.For<IOrderService>());
        var cut = RenderComponent<OrderList>(p =>
            p.Add(c => c.Orders, [new OrderDto(Guid.NewGuid(), "Pending")]));

        cut.FindAll("tr.order-row").Should().HaveCount(1);
    }
}
```

---

## Entity Framework Core

- One `DbContext` per module
- Entity configurations via `IEntityTypeConfiguration<T>` — no data annotations on domain models
- `AsNoTracking()` on all read-only queries
- Migrations: `src/Modules/<Module>/Infrastructure/Persistence/Migrations/`

---

## Logging

- Serilog with structured properties
- Always include `{ModuleName}` and `{CorrelationId}` on log entries
- Use `LoggerMessage.Define` source-generated logging for hot paths

---

## Versioning & Changelog

- Version follows [SemVer 2.0.0](https://semver.org/) — defined once in `Directory.Build.props` as `<Version>`
- `feat` commit → MINOR bump · `fix`/`perf` → PATCH · `BREAKING CHANGE:` footer → MAJOR
- `CHANGELOG.md` uses [Keep a Changelog](https://keepachangelog.com) format — always update `[Unreleased]` section
- Never bump version in multiple places — `Directory.Build.props` is the single source of truth

---

## 12-Factor Rules

- **Config:** Environment-specific values via env vars only — never in `appsettings.json`
- **Logs:** Serilog to stdout only — never file sinks inside Docker containers
- **Stateless:** No local file system state, no in-memory session state
- **Migrations:** Never call `MigrateAsync()` inside `app.Run()` — migrations are a separate pre-deploy step

---

## Commit Messages

Follow Conventional Commits:

```
feat(orders): add order cancellation endpoint
fix(auth): handle expired token refresh correctly
test(catalog): add unit tests for price calculation
```

Types: `feat`, `fix`, `test`, `refactor`, `chore`, `docs`, `ci`, `perf`

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

## What NOT to Generate

- No `using` statements for namespaces covered by `global using`
- No `async void` (except Blazor event handlers where unavoidable)
- No `Task.Result` or `.GetAwaiter().GetResult()` — always `await`
- No magic strings — use `const` or `nameof()`
- No direct `HttpClient` instantiation — always via `IHttpClientFactory`
- No secrets, connection strings, or credentials in source files

---

## UI Development Workflow (Mandatory Phase Order)

**Never skip phases. Never write component code before wireframe approval.**

| Phase | Command | Gate |
|---|---|---|
| 1 — Brainstorm | `/ui-brainstorm` | ASCII wireframe approved |
| 2 — Flow | `/ui-flow` | Mermaid diagrams approved |
| 3 — Build | `/ui-build` | Shell → logic → interactions → polish |
| 4 — Review | `/ui-review` | Checklist passes |

### Phase 1 — ASCII Wireframe (`/ui-brainstorm`)

Before writing any UI code, create an ASCII wireframe showing:
- Overall layout (AppBar, Drawer, main content area)
- Key MudBlazor regions (DataGrid, Form, Dialog, etc.)
- Primary actions (buttons, FABs)
- Empty state and loading state placeholders

Use box-drawing characters for clarity:
```
┌─────────────────────────────────────┐
│ AppBar                              │
├──────────┬──────────────────────────┤
│ Drawer   │ Main Content             │
│          │                          │
└──────────┴──────────────────────────┘
```

Save approved wireframes to `docs/design/<feature-name>/wireframe.md`.

### Phase 2 — Mermaid Flow Diagrams (`/ui-flow`)

After wireframe approval, map the logic with Mermaid diagrams:

**Diagram 1 — User Journey** (`flowchart TD`):
- All entry points, user decisions, branching paths
- Error states (validation errors, API failures, 403/404)
- Empty states, success states, exit points
- Confirmation dialogs for destructive actions

**Diagram 2 — Component & State Map**:
- Component hierarchy (parent → children)
- State ownership and data flow direction
- Service injection points and API call triggers

Save approved diagrams to `docs/design/<feature-name>/flow.md`.

### What to Check Before Writing UI Code

- [ ] Does a similar component already exist in `/src/Shared/`?
- [ ] Has the ASCII wireframe been approved?
- [ ] Has the Mermaid flow been approved?
- [ ] Are you building the shell first (no business logic yet)?
- [ ] Does the component need a bUnit test?
