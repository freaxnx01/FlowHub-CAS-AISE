# Slice A REST API Implementation Plan (Block 3 Slice A)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire a Captures-only REST API (`POST/GET /api/v1/captures`, `GET /captures/{id}`, `POST /captures/{id}/retry`) into FlowHub.Web with FluentValidation, ProblemDetails (RFC 9457), OpenAPI/Scalar, and integration tests via `WebApplicationFactory`.

**Architecture:** New library `source/FlowHub.Api/` co-hosted in the FlowHub.Web process (per ADR 0002 D2). `AddFlowHubApi()` + `MapFlowHubApi()` extensions compose into `Program.cs`. Pagination via base64-URL-encoded JSON cursor. The Submit and Retry endpoints publish `CaptureCreated` on the existing MassTransit bus from Slice B.

**Tech Stack:** .NET 10 Minimal API, FluentValidation 11.x, `Microsoft.AspNetCore.OpenApi`, Scalar.AspNetCore, `Microsoft.AspNetCore.Mvc.Testing` for integration tests.

**Source spec:** `docs/superpowers/specs/2026-05-02-slice-a-rest-api-design.md` (D1–D11).

---

## File Structure

**Create — production**

- `source/FlowHub.Api/FlowHub.Api.csproj`
- `source/FlowHub.Api/ServiceCollectionExtensions.cs`
- `source/FlowHub.Api/Endpoints/CaptureEndpoints.cs`
- `source/FlowHub.Api/Requests/CreateCaptureRequest.cs`
- `source/FlowHub.Api/Validation/CreateCaptureRequestValidator.cs`
- `source/FlowHub.Api/Pagination/CursorBinder.cs`
- `source/FlowHub.Api/ProblemDetails/FlowHubProblemDetailsWriter.cs`
- `source/FlowHub.Core/Captures/CaptureFilter.cs`
- `source/FlowHub.Core/Captures/CapturePage.cs`
- `source/FlowHub.Core/Captures/CaptureCursor.cs`

**Create — tests**

- `tests/FlowHub.Api.IntegrationTests/FlowHub.Api.IntegrationTests.csproj`
- `tests/FlowHub.Api.IntegrationTests/IntegrationTestFactory.cs`
- `tests/FlowHub.Api.IntegrationTests/Captures/SubmitCaptureTests.cs`
- `tests/FlowHub.Api.IntegrationTests/Captures/ListCapturesTests.cs`
- `tests/FlowHub.Api.IntegrationTests/Captures/GetCaptureByIdTests.cs`
- `tests/FlowHub.Api.IntegrationTests/Captures/RetryCaptureTests.cs`
- `tests/FlowHub.Api.IntegrationTests/Captures/ProblemDetailsFormatTests.cs`
- `tests/FlowHub.Api.IntegrationTests/Usings.cs`
- (extends) `tests/FlowHub.Web.ComponentTests/Captures/CaptureCursorTests.cs`
- (extends) `tests/FlowHub.Web.ComponentTests/Stubs/CaptureServiceStubTests.cs` (new tests for ListAsync + ResetForRetryAsync)

**Create — docs**

- `docs/problems/validation.md`
- `docs/problems/capture-not-found.md`
- `docs/problems/capture-not-retryable.md`

**Modify**

- `Directory.Packages.props` — add FluentValidation, OpenAPI, Scalar, Mvc.Testing
- `FlowHub.slnx` — register `FlowHub.Api` and `FlowHub.Api.IntegrationTests`
- `source/FlowHub.Web/FlowHub.Web.csproj` — add `<ProjectReference>` to FlowHub.Api
- `source/FlowHub.Web/Program.cs` — call `AddFlowHubApi()`, `AddOpenApi()`, `MapFlowHubApi()`, `MapScalarApiReference()`
- `source/FlowHub.Core/Captures/ChannelKind.cs` — add `Api` enum value
- `source/FlowHub.Core/Captures/ICaptureService.cs` — add `ListAsync` and `ResetForRetryAsync`
- `source/FlowHub.Web/Stubs/CaptureServiceStub.cs` — implement both new methods

---

## Task 1: Add packages

**Files:**
- Modify: `Directory.Packages.props`

- [ ] **Step 1.1: Discover current versions**

Run each in turn (don't add to a project — just read the latest stable version printed):

```bash
dotnet package search FluentValidation --exact-match 2>&1 | tail -3
dotnet package search FluentValidation.DependencyInjectionExtensions --exact-match 2>&1 | tail -3
dotnet package search Microsoft.AspNetCore.OpenApi --exact-match 2>&1 | tail -3
dotnet package search Scalar.AspNetCore --exact-match 2>&1 | tail -3
dotnet package search Microsoft.AspNetCore.Mvc.Testing --exact-match 2>&1 | tail -3
```

Replace `<FV>`, `<FVDI>`, `<OPENAPI>`, `<SCALAR>`, `<MVCTEST>` below with the resolved versions. If your sandbox has no network, fall back to: FluentValidation `11.10.0`, FluentValidation.DependencyInjectionExtensions `11.10.0`, Microsoft.AspNetCore.OpenApi `10.0.7`, Scalar.AspNetCore `2.0.0`, Microsoft.AspNetCore.Mvc.Testing `10.0.7`.

- [ ] **Step 1.2: Add central package versions**

In `Directory.Packages.props`, add a new `<ItemGroup>` after the existing `Messaging` group:

```xml
  <ItemGroup Label="Api">
    <PackageVersion Include="FluentValidation" Version="<FV>" />
    <PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="<FVDI>" />
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="<OPENAPI>" />
    <PackageVersion Include="Scalar.AspNetCore" Version="<SCALAR>" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="<MVCTEST>" />
  </ItemGroup>
```

- [ ] **Step 1.3: Restore + build**

Run: `make restore && make build`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`. Adding central versions doesn't add references; the build should be unchanged.

- [ ] **Step 1.4: Commit**

```bash
git add Directory.Packages.props
git commit -m "build(deps): add FluentValidation, OpenAPI, Scalar, Mvc.Testing for Slice A"
```

---

## Task 2: Scaffold `FlowHub.Api` project + DI/route stubs

**Files:**
- Create: `source/FlowHub.Api/FlowHub.Api.csproj`
- Create: `source/FlowHub.Api/ServiceCollectionExtensions.cs`
- Create: `source/FlowHub.Api/Endpoints/CaptureEndpoints.cs`
- Modify: `FlowHub.slnx`
- Modify: `source/FlowHub.Web/FlowHub.Web.csproj`
- Modify: `source/FlowHub.Web/Program.cs`

- [ ] **Step 2.1: Create `FlowHub.Api.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RootNamespace>FlowHub.Api</RootNamespace>
    <AssemblyName>FlowHub.Api</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="FluentValidation" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
    <PackageReference Include="Scalar.AspNetCore" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\FlowHub.Core\FlowHub.Core.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2.2: Add stub `ServiceCollectionExtensions.cs`**

```csharp
// source/FlowHub.Api/ServiceCollectionExtensions.cs
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FlowHub.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFlowHubApi(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddProblemDetails();
        services.AddOpenApi();
        return services;
    }
}
```

- [ ] **Step 2.3: Add stub `CaptureEndpoints.cs`**

```csharp
// source/FlowHub.Api/Endpoints/CaptureEndpoints.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace FlowHub.Api.Endpoints;

public static class CaptureEndpoints
{
    public static IEndpointRouteBuilder MapFlowHubApi(this IEndpointRouteBuilder app)
    {
        var captures = app.MapGroup("/api/v1/captures")
            .RequireAuthorization()
            .WithTags("Captures");

        // Endpoints land in subsequent tasks (T6, T7, T8, T9).

        return app;
    }
}
```

- [ ] **Step 2.4: Register the project in `FlowHub.slnx`**

In `FlowHub.slnx`, add the line for FlowHub.Api inside the `/source/` folder block:

```xml
    <Project Path="source/FlowHub.Api/FlowHub.Api.csproj" />
```

(Place it alphabetically — between `FlowHub.Core` and `FlowHub.Skills`.)

- [ ] **Step 2.5: Add ProjectReference from `FlowHub.Web`**

In `source/FlowHub.Web/FlowHub.Web.csproj`, add inside the existing `ProjectReference` ItemGroup:

```xml
    <ProjectReference Include="..\FlowHub.Api\FlowHub.Api.csproj" />
```

- [ ] **Step 2.6: Wire into `Program.cs`**

In `source/FlowHub.Web/Program.cs`:

Add to the `using` block at the top:

```csharp
using FlowHub.Api;
using FlowHub.Api.Endpoints;
using Scalar.AspNetCore;
```

Add immediately AFTER the existing `builder.Services.AddMassTransit(...)` block (just before `var app = builder.Build();`):

```csharp
// Block 3 Slice A — REST API surface for non-UI consumers.
builder.Services.AddFlowHubApi();
```

Add immediately AFTER `app.MapRazorComponents<App>()...AddInteractiveServerRenderMode();` (just before `app.Run();`):

```csharp
app.MapFlowHubApi();
app.MapOpenApi("/openapi/v1.json");
app.MapScalarApiReference();
```

- [ ] **Step 2.7: Make `Program` discoverable for `WebApplicationFactory`**

`WebApplicationFactory<TEntryPoint>` needs the entry-point type to be public. The Minimal API top-level program creates an internal `Program` class by default. Add this stub at the very end of `source/FlowHub.Web/Program.cs`:

```csharp

// Expose Program for WebApplicationFactory<Program> in integration tests.
public partial class Program { }
```

- [ ] **Step 2.8: Restore + build**

Run: `make restore && make build`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 2.9: Smoke-test the empty API**

Start the server in the background:

```bash
make run
```

In another shell:

```bash
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5070/api/v1/captures
# Expected: 404 (no method registered yet — endpoint group exists but no concrete route)
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5070/scalar
# Expected: 200
curl -s http://localhost:5070/openapi/v1.json | head -c 80
# Expected: JSON beginning with {"openapi":"3.0.x"...
```

Stop `make run` (Ctrl+C in the first shell).

- [ ] **Step 2.10: Run existing test suite**

Run: `dotnet test FlowHub.slnx`
Expected: 47 passed (no regressions).

- [ ] **Step 2.11: Commit**

```bash
git add FlowHub.slnx source/FlowHub.Api source/FlowHub.Web/FlowHub.Web.csproj source/FlowHub.Web/Program.cs
git commit -m "feat(api): scaffold FlowHub.Api project + AddFlowHubApi / MapFlowHubApi extensions"
```

---

## Task 3: Scaffold integration test project

**Files:**
- Create: `tests/FlowHub.Api.IntegrationTests/FlowHub.Api.IntegrationTests.csproj`
- Create: `tests/FlowHub.Api.IntegrationTests/Usings.cs`
- Create: `tests/FlowHub.Api.IntegrationTests/IntegrationTestFactory.cs`
- Create: `tests/FlowHub.Api.IntegrationTests/Captures/SmokeTests.cs`
- Modify: `FlowHub.slnx`

- [ ] **Step 3.1: Create the csproj**

```xml
<!-- tests/FlowHub.Api.IntegrationTests/FlowHub.Api.IntegrationTests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RootNamespace>FlowHub.Api.IntegrationTests</RootNamespace>
    <AssemblyName>FlowHub.Api.IntegrationTests</AssemblyName>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <NoWarn>$(NoWarn);CA1707</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\source\FlowHub.Api\FlowHub.Api.csproj" />
    <ProjectReference Include="..\..\source\FlowHub.Core\FlowHub.Core.csproj" />
    <ProjectReference Include="..\..\source\FlowHub.Web\FlowHub.Web.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3.2: Add `Usings.cs`**

```csharp
// tests/FlowHub.Api.IntegrationTests/Usings.cs
global using FluentAssertions;
global using Xunit;
```

- [ ] **Step 3.3: Add `IntegrationTestFactory.cs`**

```csharp
// tests/FlowHub.Api.IntegrationTests/IntegrationTestFactory.cs
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace FlowHub.Api.IntegrationTests;

/// <summary>
/// Boots FlowHub.Web in-process with the Development environment so the
/// DevAuthHandler bypass is active (no real OIDC token required).
/// </summary>
public sealed class IntegrationTestFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        return base.CreateHost(builder);
    }
}
```

- [ ] **Step 3.4: Add a smoke test**

```csharp
// tests/FlowHub.Api.IntegrationTests/Captures/SmokeTests.cs
namespace FlowHub.Api.IntegrationTests.Captures;

public sealed class SmokeTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public SmokeTests(IntegrationTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Scalar_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/scalar");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task OpenApiDocument_IsServed()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/openapi/v1.json");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"openapi\"");
    }
}
```

- [ ] **Step 3.5: Register in `FlowHub.slnx`**

In the `/tests/` folder block, add:

```xml
    <Project Path="tests/FlowHub.Api.IntegrationTests/FlowHub.Api.IntegrationTests.csproj" />
```

- [ ] **Step 3.6: Build + run targeted tests**

```bash
make build
dotnet test tests/FlowHub.Api.IntegrationTests --nologo --verbosity minimal
```
Expected: 2 passed.

- [ ] **Step 3.7: Run full suite**

Run: `dotnet test FlowHub.slnx`
Expected: 49 passed (47 baseline + 2 new).

- [ ] **Step 3.8: Commit**

```bash
git add FlowHub.slnx tests/FlowHub.Api.IntegrationTests
git commit -m "test(api): scaffold FlowHub.Api.IntegrationTests with WebApplicationFactory"
```

---

## Task 4: Add `ChannelKind.Api` enum value

**Files:**
- Modify: `source/FlowHub.Core/Captures/ChannelKind.cs`

- [ ] **Step 4.1: Extend the enum**

Replace the file content:

```csharp
namespace FlowHub.Core.Captures;

/// <summary>
/// Inbound source channel for a <see cref="Capture"/>.
/// See Glossary entry "Channel" in the CAS Obsidian vault.
/// </summary>
public enum ChannelKind
{
    Telegram,
    Web,
    Api,
}
```

- [ ] **Step 4.2: Build + run full suite**

Run: `make build && dotnet test FlowHub.slnx`
Expected: 49 passed (no regressions; the new enum value isn't yet referenced by any test).

- [ ] **Step 4.3: Commit**

```bash
git add source/FlowHub.Core/Captures/ChannelKind.cs
git commit -m "feat(core): add ChannelKind.Api for REST-submitted captures"
```

---

## Task 5: TDD `CaptureCursor`

**Files:**
- Create: `source/FlowHub.Core/Captures/CaptureCursor.cs`
- Create: `tests/FlowHub.Web.ComponentTests/Captures/CaptureCursorTests.cs`

- [ ] **Step 5.1: Write failing tests**

```csharp
// tests/FlowHub.Web.ComponentTests/Captures/CaptureCursorTests.cs
using FlowHub.Core.Captures;
using FluentAssertions;

namespace FlowHub.Web.ComponentTests.Captures;

public sealed class CaptureCursorTests
{
    [Fact]
    public void Encode_AndThen_Decode_RoundTripsValues()
    {
        var original = new CaptureCursor(
            CreatedAt: new DateTimeOffset(2026, 5, 2, 10, 0, 0, TimeSpan.FromHours(2)),
            Id: Guid.Parse("11111111-2222-3333-4444-555555555555"));

        var token = original.Encode();
        var decoded = CaptureCursor.Decode(token);

        decoded.Should().Be(original);
    }

    [Fact]
    public void Encode_ReturnsUrlSafeBase64()
    {
        var cursor = new CaptureCursor(DateTimeOffset.UtcNow, Guid.NewGuid());

        var token = cursor.Encode();

        token.Should().NotContain("+");
        token.Should().NotContain("/");
        token.Should().NotContain("=");
    }

    [Fact]
    public void Decode_MalformedToken_Throws()
    {
        Action act = () => CaptureCursor.Decode("not-a-valid-cursor");
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Decode_EmptyString_Throws()
    {
        Action act = () => CaptureCursor.Decode(string.Empty);
        act.Should().Throw<FormatException>();
    }
}
```

- [ ] **Step 5.2: Run; expect compile failure**

```bash
dotnet test tests/FlowHub.Web.ComponentTests --filter FullyQualifiedName~CaptureCursorTests
```
Expected: FAIL — `CaptureCursor` not found.

- [ ] **Step 5.3: Implement `CaptureCursor`**

```csharp
// source/FlowHub.Core/Captures/CaptureCursor.cs
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace FlowHub.Core.Captures;

public sealed record CaptureCursor(DateTimeOffset CreatedAt, Guid Id)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    public string Encode()
    {
        var json = JsonSerializer.Serialize(this, Options);
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(json));
    }

    public static CaptureCursor Decode(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            throw new FormatException("Cursor must be a non-empty Base64Url-encoded value.");
        }

        try
        {
            var bytes = WebEncoders.Base64UrlDecode(token);
            var json = Encoding.UTF8.GetString(bytes);
            var cursor = JsonSerializer.Deserialize<CaptureCursor>(json, Options);
            if (cursor is null)
            {
                throw new FormatException("Cursor decoded to null.");
            }
            return cursor;
        }
        catch (Exception ex) when (ex is not FormatException)
        {
            throw new FormatException("Cursor is not a valid Base64Url-encoded JSON document.", ex);
        }
    }
}
```

Note: `WebEncoders.Base64UrlEncode` lives in `Microsoft.AspNetCore.WebUtilities`, available transitively to `FlowHub.Core` via the `FrameworkReference Microsoft.AspNetCore.App` from FlowHub.Web's project graph — but FlowHub.Core itself doesn't reference ASP.NET Core. Add it as a package or use a hand-rolled URL-safe Base64 helper instead. Hand-rolled fallback (preferred to avoid bloating Core):

```csharp
// (replace the Base64Url-related lines above with these helpers)
public string Encode()
{
    var json = JsonSerializer.Serialize(this, Options);
    var bytes = Encoding.UTF8.GetBytes(json);
    return Convert.ToBase64String(bytes)
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');
}

public static CaptureCursor Decode(string token)
{
    if (string.IsNullOrEmpty(token))
    {
        throw new FormatException("Cursor must be a non-empty Base64Url-encoded value.");
    }

    try
    {
        var padded = token.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        var bytes = Convert.FromBase64String(padded);
        var json = Encoding.UTF8.GetString(bytes);
        var cursor = JsonSerializer.Deserialize<CaptureCursor>(json, Options);
        return cursor ?? throw new FormatException("Cursor decoded to null.");
    }
    catch (Exception ex) when (ex is not FormatException)
    {
        throw new FormatException("Cursor is not a valid Base64Url-encoded JSON document.", ex);
    }
}
```

Use the hand-rolled version. Drop the `Microsoft.AspNetCore.WebUtilities` reference and adjust the `using` block accordingly.

- [ ] **Step 5.4: Run targeted tests**

```bash
dotnet test tests/FlowHub.Web.ComponentTests --filter FullyQualifiedName~CaptureCursorTests
```
Expected: 4 passed.

- [ ] **Step 5.5: Run full suite**

```bash
dotnet test FlowHub.slnx
```
Expected: 53 passed (49 + 4 new).

- [ ] **Step 5.6: Commit**

```bash
git add source/FlowHub.Core/Captures/CaptureCursor.cs tests/FlowHub.Web.ComponentTests/Captures/
git commit -m "feat(core): add CaptureCursor with Base64Url JSON encode/decode"
```

---

## Task 6: TDD `ListAsync` + `CaptureFilter` + `CapturePage`

**Files:**
- Create: `source/FlowHub.Core/Captures/CaptureFilter.cs`
- Create: `source/FlowHub.Core/Captures/CapturePage.cs`
- Modify: `source/FlowHub.Core/Captures/ICaptureService.cs`
- Modify: `source/FlowHub.Web/Stubs/CaptureServiceStub.cs`
- Modify: `tests/FlowHub.Web.ComponentTests/Stubs/CaptureServiceStubTests.cs`

- [ ] **Step 6.1: Add the two record types**

```csharp
// source/FlowHub.Core/Captures/CaptureFilter.cs
namespace FlowHub.Core.Captures;

public sealed record CaptureFilter(
    IReadOnlyList<LifecycleStage>? Stages,
    ChannelKind? Source,
    int Limit,
    CaptureCursor? Cursor);
```

```csharp
// source/FlowHub.Core/Captures/CapturePage.cs
namespace FlowHub.Core.Captures;

public sealed record CapturePage(
    IReadOnlyList<Capture> Items,
    CaptureCursor? Next);
```

- [ ] **Step 6.2: Extend `ICaptureService`**

In `source/FlowHub.Core/Captures/ICaptureService.cs`, add this method just before the closing brace of the interface:

```csharp
    Task<CapturePage> ListAsync(CaptureFilter filter, CancellationToken cancellationToken = default);
```

- [ ] **Step 6.3: Verify build fails**

Run: `make build`
Expected: build fails — `CaptureServiceStub does not implement interface member ICaptureService.ListAsync`. This is intentional; the next step fixes it.

- [ ] **Step 6.4: Write failing tests**

In `tests/FlowHub.Web.ComponentTests/Stubs/CaptureServiceStubTests.cs`, add these tests inside the existing class (after the existing tests):

```csharp
    // ── ListAsync ──

    [Fact]
    public async Task ListAsync_NoFilter_ReturnsAllOrderedByCreatedAtDesc()
    {
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);

        var page = await sut.ListAsync(new CaptureFilter(null, null, Limit: 50, Cursor: null), default);

        page.Items.Should().HaveCountGreaterThanOrEqualTo(12);
        page.Items.Should().BeInDescendingOrder(c => c.CreatedAt);
    }

    [Fact]
    public async Task ListAsync_LimitTwo_ReturnsTwoItemsAndNextCursor()
    {
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);

        var page = await sut.ListAsync(new CaptureFilter(null, null, Limit: 2, Cursor: null), default);

        page.Items.Should().HaveCount(2);
        page.Next.Should().NotBeNull();
        page.Next!.CreatedAt.Should().Be(page.Items[1].CreatedAt);
        page.Next.Id.Should().Be(page.Items[1].Id);
    }

    [Fact]
    public async Task ListAsync_WithCursor_ReturnsItemsStrictlyAfterCursor()
    {
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);
        var firstPage = await sut.ListAsync(new CaptureFilter(null, null, Limit: 2, Cursor: null), default);

        var secondPage = await sut.ListAsync(
            new CaptureFilter(null, null, Limit: 2, Cursor: firstPage.Next), default);

        secondPage.Items.Should().NotBeEmpty();
        secondPage.Items.Select(c => c.Id).Should().NotIntersectWith(firstPage.Items.Select(c => c.Id));
    }

    [Fact]
    public async Task ListAsync_StageOrphan_ReturnsOnlyOrphanCaptures()
    {
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);

        var page = await sut.ListAsync(
            new CaptureFilter(Stages: new[] { LifecycleStage.Orphan }, null, Limit: 50, Cursor: null), default);

        page.Items.Should().NotBeEmpty();
        page.Items.Should().OnlyContain(c => c.Stage == LifecycleStage.Orphan);
    }
```

(Don't forget `using FlowHub.Core.Captures;` is already present.)

- [ ] **Step 6.5: Run; expect compile failure**

```bash
dotnet test tests/FlowHub.Web.ComponentTests --filter FullyQualifiedName~CaptureServiceStubTests
```
Expected: FAIL — `ListAsync` not implemented in `CaptureServiceStub`.

- [ ] **Step 6.6: Implement `ListAsync` in `CaptureServiceStub`**

In `source/FlowHub.Web/Stubs/CaptureServiceStub.cs`, add the `using` if missing:

```csharp
using FlowHub.Core.Captures;  // already present
```

Add the method (place it after `MarkUnhandledAsync`):

```csharp
    public Task<CapturePage> ListAsync(CaptureFilter filter, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Capture> items;
        CaptureCursor? next;

        lock (_lock)
        {
            IEnumerable<Capture> query = _captures
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id);

            if (filter.Stages is { Count: > 0 } stages)
            {
                query = query.Where(c => stages.Contains(c.Stage));
            }

            if (filter.Source is ChannelKind src)
            {
                query = query.Where(c => c.Source == src);
            }

            if (filter.Cursor is CaptureCursor cursor)
            {
                query = query.SkipWhile(c =>
                    c.CreatedAt > cursor.CreatedAt
                    || (c.CreatedAt == cursor.CreatedAt && c.Id.CompareTo(cursor.Id) >= 0));
            }

            var limit = Math.Clamp(filter.Limit, 1, 200);
            var page = query.Take(limit + 1).ToList();

            if (page.Count > limit)
            {
                var last = page[limit - 1];
                next = new CaptureCursor(last.CreatedAt, last.Id);
                items = page.Take(limit).ToList();
            }
            else
            {
                next = null;
                items = page;
            }
        }

        return Task.FromResult(new CapturePage(items, next));
    }
```

- [ ] **Step 6.7: Run targeted tests**

```bash
dotnet test tests/FlowHub.Web.ComponentTests --filter FullyQualifiedName~CaptureServiceStubTests
```
Expected: 14 passed (10 prior + 4 new).

- [ ] **Step 6.8: Run full suite**

Run: `dotnet test FlowHub.slnx`
Expected: 57 passed (53 + 4 new).

- [ ] **Step 6.9: Commit**

```bash
git add source/FlowHub.Core/Captures/CaptureFilter.cs source/FlowHub.Core/Captures/CapturePage.cs source/FlowHub.Core/Captures/ICaptureService.cs source/FlowHub.Web/Stubs/CaptureServiceStub.cs tests/FlowHub.Web.ComponentTests/Stubs/CaptureServiceStubTests.cs
git commit -m "feat(core): add ListAsync + CaptureFilter + CapturePage to ICaptureService"
```

---

## Task 7: TDD `ResetForRetryAsync`

**Files:**
- Modify: `source/FlowHub.Core/Captures/ICaptureService.cs`
- Modify: `source/FlowHub.Web/Stubs/CaptureServiceStub.cs`
- Modify: `tests/FlowHub.Web.ComponentTests/Stubs/CaptureServiceStubTests.cs`

- [ ] **Step 7.1: Add the interface method**

In `source/FlowHub.Core/Captures/ICaptureService.cs`, add just before the closing brace:

```csharp
    Task ResetForRetryAsync(Guid id, CancellationToken cancellationToken = default);
```

- [ ] **Step 7.2: Write failing tests**

Append to `tests/FlowHub.Web.ComponentTests/Stubs/CaptureServiceStubTests.cs`:

```csharp
    [Fact]
    public async Task ResetForRetryAsync_OrphanCapture_ResetsToRawAndClearsFailureReason()
    {
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);
        var capture = await sut.SubmitAsync("hello", ChannelKind.Web, default);
        await sut.MarkOrphanAsync(capture.Id, "no skill matched", default);

        await sut.ResetForRetryAsync(capture.Id, default);

        var updated = await sut.GetByIdAsync(capture.Id, default);
        updated!.Stage.Should().Be(LifecycleStage.Raw);
        updated.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task ResetForRetryAsync_UnknownId_Throws()
    {
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);

        Func<Task> act = () => sut.ResetForRetryAsync(Guid.NewGuid(), default);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
```

- [ ] **Step 7.3: Implement in `CaptureServiceStub`**

After `MarkUnhandledAsync` (and before `ListAsync`), add:

```csharp
    public Task ResetForRetryAsync(Guid id, CancellationToken cancellationToken = default) =>
        ReplaceCapture(id, c => c with { Stage = LifecycleStage.Raw, FailureReason = null });
```

- [ ] **Step 7.4: Run targeted + full**

```bash
dotnet test tests/FlowHub.Web.ComponentTests --filter FullyQualifiedName~CaptureServiceStubTests
```
Expected: 16 passed.

```bash
dotnet test FlowHub.slnx
```
Expected: 59 passed.

- [ ] **Step 7.5: Commit**

```bash
git add source/FlowHub.Core/Captures/ICaptureService.cs source/FlowHub.Web/Stubs/CaptureServiceStub.cs tests/FlowHub.Web.ComponentTests/Stubs/CaptureServiceStubTests.cs
git commit -m "feat(core): add ResetForRetryAsync to ICaptureService"
```

---

## Task 8: TDD `POST /api/v1/captures` (submit)

**Files:**
- Create: `source/FlowHub.Api/Requests/CreateCaptureRequest.cs`
- Create: `source/FlowHub.Api/Validation/CreateCaptureRequestValidator.cs`
- Modify: `source/FlowHub.Api/Endpoints/CaptureEndpoints.cs`
- Modify: `source/FlowHub.Api/ServiceCollectionExtensions.cs`
- Create: `tests/FlowHub.Api.IntegrationTests/Captures/SubmitCaptureTests.cs`

- [ ] **Step 8.1: Add the request DTO**

```csharp
// source/FlowHub.Api/Requests/CreateCaptureRequest.cs
using FlowHub.Core.Captures;

namespace FlowHub.Api.Requests;

public sealed record CreateCaptureRequest(string Content, ChannelKind Source);
```

- [ ] **Step 8.2: Add the validator**

```csharp
// source/FlowHub.Api/Validation/CreateCaptureRequestValidator.cs
using FluentValidation;
using FlowHub.Api.Requests;
using FlowHub.Core.Captures;

namespace FlowHub.Api.Validation;

public sealed class CreateCaptureRequestValidator : AbstractValidator<CreateCaptureRequest>
{
    public CreateCaptureRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content must not be empty.")
            .MaximumLength(8192).WithMessage("Content exceeds the 8192-character limit.");

        RuleFor(x => x.Source)
            .IsInEnum().WithMessage("Source must be a known ChannelKind value.");
    }
}
```

- [ ] **Step 8.3: Write failing integration tests**

```csharp
// tests/FlowHub.Api.IntegrationTests/Captures/SubmitCaptureTests.cs
using System.Net;
using System.Net.Http.Json;
using FlowHub.Core.Captures;

namespace FlowHub.Api.IntegrationTests.Captures;

public sealed class SubmitCaptureTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public SubmitCaptureTests(IntegrationTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Post_ValidContent_Returns201WithLocationAndCapture()
    {
        var client = _factory.CreateClient();
        var body = new { content = "https://example.com/article", source = "Api" };

        var response = await client.PostAsJsonAsync("/api/v1/captures", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().StartWith("/api/v1/captures/");

        var capture = await response.Content.ReadFromJsonAsync<Capture>();
        capture.Should().NotBeNull();
        capture!.Content.Should().Be("https://example.com/article");
        capture.Source.Should().Be(ChannelKind.Api);
        capture.Stage.Should().Be(LifecycleStage.Raw);
    }

    [Fact]
    public async Task Post_EmptyContent_Returns400WithValidationProblem()
    {
        var client = _factory.CreateClient();
        var body = new { content = "", source = "Api" };

        var response = await client.PostAsJsonAsync("/api/v1/captures", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadAsStringAsync();
        problem.Should().Contain("\"errors\"").And.Contain("Content");
    }

    [Fact]
    public async Task Post_ContentOver8192_Returns400()
    {
        var client = _factory.CreateClient();
        var body = new { content = new string('x', 8193), source = "Api" };

        var response = await client.PostAsJsonAsync("/api/v1/captures", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_UnknownSource_Returns400()
    {
        var client = _factory.CreateClient();
        var body = new { content = "ok", source = "DoesNotExist" };

        var response = await client.PostAsJsonAsync("/api/v1/captures", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

- [ ] **Step 8.4: Run; expect failures (404 because no POST handler yet)**

```bash
dotnet test tests/FlowHub.Api.IntegrationTests --filter FullyQualifiedName~SubmitCaptureTests
```
Expected: 4 failed.

- [ ] **Step 8.5: Implement the endpoint handler**

Replace `source/FlowHub.Api/Endpoints/CaptureEndpoints.cs`:

```csharp
using FluentValidation;
using FlowHub.Api.Requests;
using FlowHub.Core.Captures;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FlowHub.Api.Endpoints;

public static class CaptureEndpoints
{
    public static IEndpointRouteBuilder MapFlowHubApi(this IEndpointRouteBuilder app)
    {
        var captures = app.MapGroup("/api/v1/captures")
            .RequireAuthorization()
            .WithTags("Captures");

        captures.MapPost("/", SubmitAsync)
            .WithName("SubmitCapture")
            .Produces<Capture>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        return app;
    }

    private static async Task<Results<Created<Capture>, ValidationProblem>> SubmitAsync(
        CreateCaptureRequest request,
        IValidator<CreateCaptureRequest> validator,
        ICaptureService captureService,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return TypedResults.ValidationProblem(errors);
        }

        var capture = await captureService.SubmitAsync(request.Content, request.Source, ct);
        return TypedResults.Created($"/api/v1/captures/{capture.Id}", capture);
    }
}
```

- [ ] **Step 8.6: Run targeted tests**

```bash
dotnet test tests/FlowHub.Api.IntegrationTests --filter FullyQualifiedName~SubmitCaptureTests
```
Expected: 4 passed.

- [ ] **Step 8.7: Run full suite**

Run: `dotnet test FlowHub.slnx`
Expected: 63 passed.

- [ ] **Step 8.8: Commit**

```bash
git add source/FlowHub.Api/Requests source/FlowHub.Api/Validation source/FlowHub.Api/Endpoints/CaptureEndpoints.cs tests/FlowHub.Api.IntegrationTests/Captures/SubmitCaptureTests.cs
git commit -m "feat(api): add POST /api/v1/captures with FluentValidation"
```

---

## Task 9: TDD `GET /api/v1/captures` (list with cursor pagination)

**Files:**
- Modify: `source/FlowHub.Api/Endpoints/CaptureEndpoints.cs`
- Create: `tests/FlowHub.Api.IntegrationTests/Captures/ListCapturesTests.cs`

- [ ] **Step 9.1: Write failing tests**

```csharp
// tests/FlowHub.Api.IntegrationTests/Captures/ListCapturesTests.cs
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FlowHub.Core.Captures;

namespace FlowHub.Api.IntegrationTests.Captures;

public sealed class ListCapturesTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    public ListCapturesTests(IntegrationTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Get_NoFilter_ReturnsFirstPageWithItems()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/captures?limit=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<ListResponse>(JsonOpts);
        page.Should().NotBeNull();
        page!.Items.Should().HaveCountLessThanOrEqualTo(5);
        page.NextCursor.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Get_WithCursor_ReturnsNextPageNoOverlap()
    {
        var client = _factory.CreateClient();
        var firstResponse = await client.GetAsync("/api/v1/captures?limit=3");
        var firstPage = await firstResponse.Content.ReadFromJsonAsync<ListResponse>(JsonOpts);

        var secondResponse = await client.GetAsync($"/api/v1/captures?limit=3&cursor={Uri.EscapeDataString(firstPage!.NextCursor!)}");

        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondPage = await secondResponse.Content.ReadFromJsonAsync<ListResponse>(JsonOpts);
        secondPage!.Items.Should().NotBeEmpty();
        secondPage.Items.Select(c => c.Id).Should().NotIntersectWith(firstPage.Items.Select(c => c.Id));
    }

    [Fact]
    public async Task Get_StageFilter_ReturnsOnlyMatchingStages()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/captures?stage=Orphan,Unhandled&limit=50");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<ListResponse>(JsonOpts);
        page!.Items.Should().OnlyContain(c =>
            c.Stage == LifecycleStage.Orphan || c.Stage == LifecycleStage.Unhandled);
    }

    [Fact]
    public async Task Get_SourceFilter_ReturnsOnlyMatching()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/captures", new { content = "for-api-source-test", source = "Api" });

        var response = await client.GetAsync("/api/v1/captures?source=Api&limit=50");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<ListResponse>(JsonOpts);
        page!.Items.Should().OnlyContain(c => c.Source == ChannelKind.Api);
    }

    [Fact]
    public async Task Get_MalformedCursor_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/captures?cursor=not-a-real-cursor");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    private sealed record ListResponse(IReadOnlyList<Capture> Items, string? NextCursor);
}
```

- [ ] **Step 9.2: Run; expect 5 failures (route doesn't exist)**

```bash
dotnet test tests/FlowHub.Api.IntegrationTests --filter FullyQualifiedName~ListCapturesTests
```
Expected: 5 failed.

- [ ] **Step 9.3: Implement the endpoint**

Add to `source/FlowHub.Api/Endpoints/CaptureEndpoints.cs` (inside `MapFlowHubApi`, after the POST registration):

```csharp
        captures.MapGet("/", ListAsync)
            .WithName("ListCaptures")
            .Produces<ListCapturesResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
```

Add the response DTO and the handler at the bottom of the file:

```csharp
    public sealed record ListCapturesResponse(IReadOnlyList<Capture> Items, string? NextCursor);

    private static async Task<Results<Ok<ListCapturesResponse>, ProblemHttpResult>> ListAsync(
        ICaptureService captureService,
        HttpContext httpContext,
        string? stage,
        ChannelKind? source,
        int? limit,
        string? cursor,
        CancellationToken ct)
    {
        IReadOnlyList<LifecycleStage>? stages = null;
        if (!string.IsNullOrEmpty(stage))
        {
            try
            {
                stages = stage.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => Enum.Parse<LifecycleStage>(s, ignoreCase: true))
                    .ToArray();
            }
            catch (ArgumentException)
            {
                return TypedResults.Problem(
                    type: "https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/validation.md",
                    title: "Invalid stage filter.",
                    detail: $"'{stage}' contains an unknown LifecycleStage.",
                    statusCode: StatusCodes.Status400BadRequest,
                    instance: httpContext.Request.Path);
            }
        }

        CaptureCursor? captureCursor = null;
        if (!string.IsNullOrEmpty(cursor))
        {
            try
            {
                captureCursor = CaptureCursor.Decode(cursor);
            }
            catch (FormatException)
            {
                return TypedResults.Problem(
                    type: "https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/validation.md",
                    title: "Invalid pagination cursor.",
                    detail: "The cursor is not a valid Base64Url-encoded value.",
                    statusCode: StatusCodes.Status400BadRequest,
                    instance: httpContext.Request.Path);
            }
        }

        var filter = new CaptureFilter(stages, source, limit ?? 50, captureCursor);
        var page = await captureService.ListAsync(filter, ct);

        return TypedResults.Ok(new ListCapturesResponse(page.Items, page.Next?.Encode()));
    }
```

- [ ] **Step 9.4: Run targeted + full**

```bash
dotnet test tests/FlowHub.Api.IntegrationTests --filter FullyQualifiedName~ListCapturesTests
dotnet test FlowHub.slnx
```
Expected: 5 / 68 passed.

- [ ] **Step 9.5: Commit**

```bash
git add source/FlowHub.Api/Endpoints/CaptureEndpoints.cs tests/FlowHub.Api.IntegrationTests/Captures/ListCapturesTests.cs
git commit -m "feat(api): add GET /api/v1/captures with cursor pagination + stage/source filters"
```

---

## Task 10: TDD `GET /api/v1/captures/{id}`

**Files:**
- Modify: `source/FlowHub.Api/Endpoints/CaptureEndpoints.cs`
- Create: `tests/FlowHub.Api.IntegrationTests/Captures/GetCaptureByIdTests.cs`

- [ ] **Step 10.1: Write failing tests**

```csharp
// tests/FlowHub.Api.IntegrationTests/Captures/GetCaptureByIdTests.cs
using System.Net;
using System.Net.Http.Json;
using FlowHub.Core.Captures;

namespace FlowHub.Api.IntegrationTests.Captures;

public sealed class GetCaptureByIdTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public GetCaptureByIdTests(IntegrationTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Get_KnownId_Returns200WithCapture()
    {
        var client = _factory.CreateClient();
        var submit = await client.PostAsJsonAsync("/api/v1/captures",
            new { content = "for-getbyid", source = "Api" });
        var created = await submit.Content.ReadFromJsonAsync<Capture>();

        var response = await client.GetAsync($"/api/v1/captures/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var capture = await response.Content.ReadFromJsonAsync<Capture>();
        capture!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task Get_UnknownId_Returns404WithCaptureNotFoundProblem()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/captures/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("capture-not-found");
    }
}
```

- [ ] **Step 10.2: Run; expect failures**

```bash
dotnet test tests/FlowHub.Api.IntegrationTests --filter FullyQualifiedName~GetCaptureByIdTests
```
Expected: 2 failed (route 404).

- [ ] **Step 10.3: Implement**

Inside `MapFlowHubApi`, add after the GET-list registration:

```csharp
        captures.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetCapture")
            .Produces<Capture>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
```

Add the handler method at the bottom of the class:

```csharp
    private static async Task<Results<Ok<Capture>, ProblemHttpResult>> GetByIdAsync(
        Guid id,
        ICaptureService captureService,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var capture = await captureService.GetByIdAsync(id, ct);
        if (capture is null)
        {
            return TypedResults.Problem(
                type: "https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/capture-not-found.md",
                title: "Capture not found.",
                detail: $"No capture exists with id {id}.",
                statusCode: StatusCodes.Status404NotFound,
                instance: httpContext.Request.Path);
        }
        return TypedResults.Ok(capture);
    }
```

- [ ] **Step 10.4: Run targeted + full**

```bash
dotnet test tests/FlowHub.Api.IntegrationTests --filter FullyQualifiedName~GetCaptureByIdTests
dotnet test FlowHub.slnx
```
Expected: 2 / 70 passed.

- [ ] **Step 10.5: Commit**

```bash
git add source/FlowHub.Api/Endpoints/CaptureEndpoints.cs tests/FlowHub.Api.IntegrationTests/Captures/GetCaptureByIdTests.cs
git commit -m "feat(api): add GET /api/v1/captures/{id} with capture-not-found problem"
```

---

## Task 11: TDD `POST /api/v1/captures/{id}/retry`

**Files:**
- Modify: `source/FlowHub.Api/Endpoints/CaptureEndpoints.cs`
- Create: `tests/FlowHub.Api.IntegrationTests/Captures/RetryCaptureTests.cs`

The retry endpoint must:
1. Look up the capture; 404 if missing.
2. Verify stage ∈ {`Orphan`, `Unhandled`}; 409 with `capture-not-retryable` problem otherwise.
3. Call `ICaptureService.ResetForRetryAsync(id)` to flip stage to `Raw` and clear `FailureReason`.
4. Republish `CaptureCreated` on the bus so the pipeline re-enters from the beginning.
5. Return `202 Accepted` with the updated capture body.

Republishing requires `IPublishEndpoint`/`IBus` from MassTransit. Inject `IBus` (singleton, captive-dependency-safe) into the handler.

- [ ] **Step 11.1: Write failing tests**

```csharp
// tests/FlowHub.Api.IntegrationTests/Captures/RetryCaptureTests.cs
using System.Net;
using System.Net.Http.Json;
using FlowHub.Core.Captures;

namespace FlowHub.Api.IntegrationTests.Captures;

public sealed class RetryCaptureTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public RetryCaptureTests(IntegrationTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Retry_OrphanCapture_Returns202AndResetsStageToRaw()
    {
        var client = _factory.CreateClient();
        // The seeded Bogus stub includes captures at indices 2 and 8 with Stage=Orphan.
        // Pick one Orphan capture from the list endpoint.
        var listResponse = await client.GetAsync("/api/v1/captures?stage=Orphan&limit=1");
        var page = await listResponse.Content.ReadFromJsonAsync<ListPage>();
        page!.Items.Should().NotBeEmpty();
        var orphan = page.Items[0];

        var response = await client.PostAsync($"/api/v1/captures/{orphan.Id}/retry", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var capture = await response.Content.ReadFromJsonAsync<Capture>();
        capture!.Stage.Should().Be(LifecycleStage.Raw);
        capture.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task Retry_CompletedCapture_Returns409WithNotRetryableProblem()
    {
        var client = _factory.CreateClient();
        var listResponse = await client.GetAsync("/api/v1/captures?stage=Completed&limit=1");
        var page = await listResponse.Content.ReadFromJsonAsync<ListPage>();
        page!.Items.Should().NotBeEmpty();
        var completed = page.Items[0];

        var response = await client.PostAsync($"/api/v1/captures/{completed.Id}/retry", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("capture-not-retryable");
    }

    [Fact]
    public async Task Retry_UnknownId_Returns404()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync($"/api/v1/captures/{Guid.NewGuid()}/retry", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record ListPage(IReadOnlyList<Capture> Items, string? NextCursor);
}
```

- [ ] **Step 11.2: Run; expect failures**

```bash
dotnet test tests/FlowHub.Api.IntegrationTests --filter FullyQualifiedName~RetryCaptureTests
```
Expected: 3 failed.

- [ ] **Step 11.3: Implement**

In `source/FlowHub.Api/Endpoints/CaptureEndpoints.cs`:

Add `using MassTransit;` and `using FlowHub.Core.Events;` at the top.

Inside `MapFlowHubApi`, after the GET-by-id registration:

```csharp
        captures.MapPost("/{id:guid}/retry", RetryAsync)
            .WithName("RetryCapture")
            .Produces<Capture>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
```

Handler at the bottom of the class:

```csharp
    private static readonly LifecycleStage[] RetryableStages = [LifecycleStage.Orphan, LifecycleStage.Unhandled];

    private static async Task<Results<Accepted<Capture>, ProblemHttpResult>> RetryAsync(
        Guid id,
        ICaptureService captureService,
        IBus bus,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var capture = await captureService.GetByIdAsync(id, ct);
        if (capture is null)
        {
            return TypedResults.Problem(
                type: "https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/capture-not-found.md",
                title: "Capture not found.",
                detail: $"No capture exists with id {id}.",
                statusCode: StatusCodes.Status404NotFound,
                instance: httpContext.Request.Path);
        }

        if (!RetryableStages.Contains(capture.Stage))
        {
            return TypedResults.Problem(
                type: "https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/capture-not-retryable.md",
                title: "Capture stage is not retryable.",
                detail: $"Captures may only be retried from Orphan or Unhandled. Current stage: {capture.Stage}.",
                statusCode: StatusCodes.Status409Conflict,
                instance: httpContext.Request.Path);
        }

        await captureService.ResetForRetryAsync(id, ct);
        await bus.Publish(new CaptureCreated(capture.Id, capture.Content, capture.Source, capture.CreatedAt), ct);

        var updated = await captureService.GetByIdAsync(id, ct);
        return TypedResults.Accepted($"/api/v1/captures/{id}", updated!);
    }
```

- [ ] **Step 11.4: Run targeted + full**

```bash
dotnet test tests/FlowHub.Api.IntegrationTests --filter FullyQualifiedName~RetryCaptureTests
dotnet test FlowHub.slnx
```
Expected: 3 / 73 passed.

- [ ] **Step 11.5: Commit**

```bash
git add source/FlowHub.Api/Endpoints/CaptureEndpoints.cs tests/FlowHub.Api.IntegrationTests/Captures/RetryCaptureTests.cs
git commit -m "feat(api): add POST /api/v1/captures/{id}/retry with stage validation + republish"
```

---

## Task 12: ProblemDetails wire-format test

**Files:**
- Create: `tests/FlowHub.Api.IntegrationTests/Captures/ProblemDetailsFormatTests.cs`

ASP.NET Core 10's default `AddProblemDetails()` already produces RFC 9457-compliant JSON with `type`, `title`, `status`, `instance`, plus a `traceId` extension when an `Activity` is current. We don't need a custom factory for the basic shape — verify the wire format here.

- [ ] **Step 12.1: Write the format test**

```csharp
// tests/FlowHub.Api.IntegrationTests/Captures/ProblemDetailsFormatTests.cs
using System.Net.Http.Json;
using System.Text.Json;

namespace FlowHub.Api.IntegrationTests.Captures;

public sealed class ProblemDetailsFormatTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public ProblemDetailsFormatTests(IntegrationTestFactory factory) => _factory = factory;

    [Fact]
    public async Task ValidationFailure_HasRfc9457KeysAndTraceId()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/captures", new { content = "", source = "Api" });
        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.TryGetProperty("type", out _).Should().BeTrue("type is required by RFC 9457");
        root.TryGetProperty("title", out _).Should().BeTrue();
        root.TryGetProperty("status", out _).Should().BeTrue();
        root.TryGetProperty("errors", out _).Should().BeTrue("validation failures flatten errors per field");
        // traceId is added by AddProblemDetails when an Activity is current; this should hold under WAF.
        root.TryGetProperty("traceId", out _).Should().BeTrue();
    }
}
```

- [ ] **Step 12.2: Run targeted**

```bash
dotnet test tests/FlowHub.Api.IntegrationTests --filter FullyQualifiedName~ProblemDetailsFormatTests
```
Expected: 1 passed. If `traceId` isn't present, the default `AddProblemDetails` doesn't enrich; in that case, configure it in `AddFlowHubApi`:

```csharp
services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        var activity = System.Diagnostics.Activity.Current;
        if (activity is not null && !ctx.ProblemDetails.Extensions.ContainsKey("traceId"))
        {
            ctx.ProblemDetails.Extensions["traceId"] = activity.Id;
        }
    };
});
```

Re-run the test until it passes.

- [ ] **Step 12.3: Run full suite**

```bash
dotnet test FlowHub.slnx
```
Expected: 74 passed.

- [ ] **Step 12.4: Commit**

```bash
git add source/FlowHub.Api/ServiceCollectionExtensions.cs tests/FlowHub.Api.IntegrationTests/Captures/ProblemDetailsFormatTests.cs
git commit -m "feat(api): ensure traceId in ProblemDetails extensions"
```

---

## Task 13: OpenAPI sidebar polish + Scalar smoke

**Files:**
- Modify: `source/FlowHub.Api/Endpoints/CaptureEndpoints.cs`

The four endpoints already have `WithName(...)`, `WithTags("Captures")` (group-level), and `Produces<T>(...)`. Verify the OpenAPI document and Scalar render correctly.

- [ ] **Step 13.1: Verify the OpenAPI document lists all 4 endpoints**

Start the server:

```bash
make run
```

In another shell:

```bash
curl -s http://localhost:5070/openapi/v1.json | python3 -c "import json,sys; d=json.load(sys.stdin); print('\\n'.join(d['paths'].keys()))"
# Expected: 4 lines:
#   /api/v1/captures
#   /api/v1/captures/{id}
#   /api/v1/captures/{id}/retry
```

(`/api/v1/captures` covers both POST and GET; the `paths` keys collapse on path so 3 keys is correct, with the GET/POST methods both under `/api/v1/captures`.)

- [ ] **Step 13.2: Visual smoke-test of Scalar**

```bash
curl -s http://localhost:5070/scalar | head -c 200
# Expected: HTML content containing "scalar"
```

Stop `make run`.

- [ ] **Step 13.3: Add `Produces<T>` for ProblemDetails on all routes if missing**

Each endpoint already declares `.Produces<...>` for happy-path and `.ProducesProblem(...)` for error paths. Verify by reading the file. No changes needed if all is in place; otherwise add the missing declarations.

- [ ] **Step 13.4: No commit unless changes were made**

If you added `.Produces` declarations:

```bash
git add source/FlowHub.Api/Endpoints/CaptureEndpoints.cs
git commit -m "docs(api): complete Produces / ProducesProblem declarations for OpenAPI"
```

---

## Task 14: Author the 3 problem docs

**Files:**
- Create: `docs/problems/validation.md`
- Create: `docs/problems/capture-not-found.md`
- Create: `docs/problems/capture-not-retryable.md`

These are the human-readable docs that ProblemDetails `type` URIs resolve to. They live in `main` so the URLs at `https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/<slug>.md` resolve.

- [ ] **Step 14.1: `docs/problems/validation.md`**

```markdown
# `validation` problem

Type URI: `https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/validation.md`

Returned when one or more request fields fail validation. Status code: `400 Bad Request`.

## Response shape

```json
{
  "type": "https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/validation.md",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "instance": "/api/v1/captures",
  "errors": {
    "Content": ["Content must not be empty."]
  },
  "traceId": "00-..."
}
```

## How to fix

The `errors` object lists violating field names with messages. Resubmit the request after correcting the listed fields.

## Where it surfaces

- `POST /api/v1/captures` — invalid `content` or `source`
- `GET /api/v1/captures` — malformed `cursor` or unknown `stage` value
```

- [ ] **Step 14.2: `docs/problems/capture-not-found.md`**

```markdown
# `capture-not-found` problem

Type URI: `https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/capture-not-found.md`

Returned when a capture id is referenced but no capture exists with that id. Status code: `404 Not Found`.

## Response shape

```json
{
  "type": "https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/capture-not-found.md",
  "title": "Capture not found.",
  "status": 404,
  "detail": "No capture exists with id <guid>.",
  "instance": "/api/v1/captures/<guid>",
  "traceId": "00-..."
}
```

## How to fix

Verify the capture id is correct. Captures in FlowHub are append-only — once created they persist, but ids are case-sensitive GUIDs and must be exact.

## Where it surfaces

- `GET /api/v1/captures/{id}`
- `POST /api/v1/captures/{id}/retry`
```

- [ ] **Step 14.3: `docs/problems/capture-not-retryable.md`**

```markdown
# `capture-not-retryable` problem

Type URI: `https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/capture-not-retryable.md`

Returned when `POST /api/v1/captures/{id}/retry` is called against a capture whose lifecycle stage is not retryable. Status code: `409 Conflict`.

Only captures in `Orphan` or `Unhandled` are retryable. Captures in `Raw`, `Classified`, `Routed`, or `Completed` cannot be retried — `Raw`/`Classified`/`Routed` are in-flight; `Completed` is the success terminal state.

## Response shape

```json
{
  "type": "https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/capture-not-retryable.md",
  "title": "Capture stage is not retryable.",
  "status": 409,
  "detail": "Captures may only be retried from Orphan or Unhandled. Current stage: Completed.",
  "instance": "/api/v1/captures/<guid>/retry",
  "traceId": "00-..."
}
```

## How to fix

If the capture is in flight (`Raw`/`Classified`/`Routed`), wait — the pipeline will resolve it to a terminal state on its own. If it's `Completed`, no retry is needed. If you need to recover from a permanent failure, that capture is already `Orphan` or `Unhandled` and is retryable from there.
```

- [ ] **Step 14.4: Commit**

```bash
git add docs/problems
git commit -m "docs(api): add 3 ProblemDetails type docs (validation, capture-not-found, capture-not-retryable)"
```

---

## Task 15: Tick off Block-3 vault checklist + final test pass

**Files:**
- Modify: `vault/Blöcke/03 Service/03 Service - c) Nachbereitung.md`

- [ ] **Step 15.1: Run full suite once more**

```bash
make build && dotnet test FlowHub.slnx
```
Expected: build clean, **74 passed** (49 baseline before this slice + 4 cursor + 4 ListAsync + 2 ResetForRetry + 4 submit + 5 list + 2 getbyid + 3 retry + 1 problem-format = 25 new tests).

- [ ] **Step 15.2: Smoke-run the full app**

```bash
make run &
sleep 8
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5070/                          # Expected: 200
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5070/scalar                    # Expected: 200
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5070/api/v1/captures           # Expected: 200 (DevAuth bypass)
pkill -f FlowHub.Web
```

- [ ] **Step 15.3: Tick off Block-3 Nachbereitung items**

In `vault/Blöcke/03 Service/03 Service - c) Nachbereitung.md`, change the following items to `- [x]`:

- "Projekt scaffolden + in `FlowHub.slnx` registrieren" (REST-API section)
- "Endpoints: `GET/POST /api/captures`, …" — annotate inline that retry/list/get-by-id all landed
- "FluentValidation am Boundary, ProblemDetails (RFC 9457) für Fehler"
- "OpenAPI + Scalar UI (`/scalar`)"
- "Stub-Daten-Quelle (Bogus/In-Memory aus Block 2 wiederverwenden)"
- "Component-Tests für API-Endpoints (`Microsoft.AspNetCore.Mvc.Testing`)"
- "OpenAPI-Versionierungs- und Konsistenzstrategie festlegen (Scalar als UI, Refit/Kiota als Client-Generator)" — partial: Scalar is wired; Refit/Kiota is Slice D
- Update `updated:` frontmatter to `2026-05-XX` (today's date).

For Bewertungskriterien items, additional ticks where evidence has landed:

- "Abnahmekriterien definiert (5)" — partial via integration tests asserting expected response shapes per endpoint
- "Test-Strategie + Technologien spezifiziert (5)" — `docs/spec/testing-strategy.md` already exists; this slice adds `Microsoft.AspNetCore.Mvc.Testing` to the list (note in the file)

- [ ] **Step 15.4: Update `docs/ai-usage.md`**

Append a new section under `## Block 3 Slice A — REST API` mirroring the Slice B structure (tools, generated vs handwritten share, what worked / didn't, prompts of note).

- [ ] **Step 15.5: Commit**

```bash
git add vault/Blöcke docs/ai-usage.md
git commit -m "docs(vault/blöcke): tick off Slice-A items in Block 3 Nachbereitung"
```

- [ ] **Step 15.6: Push (ASK USER FIRST)**

Do not push without explicit user confirmation. Once confirmed:

```bash
git push origin <branch>
```

---

## Self-review

**Spec coverage check (D1–D11 + endpoint requirements):**

- D1 `ChannelKind.Api` → Task 4
- D2 `source/FlowHub.Api/` library co-hosted → Task 2 (csproj + Program.cs wiring)
- D3 retry returns 202 → Task 11 (`TypedResults.Accepted`)
- D4 day-one filters: stage, source, limit, cursor → Task 9 (handler binds all four)
- D5 ProblemDetails type URIs at github.com/.../docs/problems/<slug>.md → Tasks 9–11 (handlers) + Task 14 (docs)
- D6 no DELETE → no task adds DELETE; spec out-of-scope
- D7 Captures-only scope → Tasks 8–11 only cover Captures; Skills/Integrations endpoints absent by design
- D8 new test project via WebApplicationFactory → Task 3
- D9 cursor = base64-URL JSON → Task 5
- D10 ListAsync + ResetForRetryAsync in Core → Tasks 6, 7
- D11 Bruno deferred → no task creates Bruno collections

All 4 endpoints implemented (Tasks 8, 9, 10, 11) with the test cases enumerated in the spec (4 + 5 + 2 + 3 + 1 = 15 cases — note: spec lists 14 + 1 format test = 15 total).

**Placeholder scan:** no `TBD`, `TODO`, vague directives, or "implement later" references in any task. Step 1.1 references `<FV>` etc. as version placeholders that the implementer resolves at runtime — explicitly explained.

**Type consistency:**
- `CaptureFilter(Stages, Source, Limit, Cursor)` — same property names in handler bindings (Task 9), unit tests (Task 6), and stub implementation (Task 6 Step 6.6). ✓
- `CapturePage(Items, Next)` — wire-format response uses `nextCursor` (camelCased `Next` after `JsonNamingPolicy.CamelCase`); Task 9 handler maps `Next` to a `string?` via `Encode()`. ✓
- `ListCapturesResponse(Items, NextCursor)` — wire-side type with `NextCursor` as a string; tests deserialize into a private `ListResponse(Items, NextCursor)` shape. ✓
- `RetryableStages` array (Task 11) — uses `LifecycleStage.Orphan`, `LifecycleStage.Unhandled`. ✓
- `CaptureCursor.Encode()` returns `string` (URL-safe base64); `Decode(string)` returns `CaptureCursor` and throws `FormatException` on malformed input. ✓
