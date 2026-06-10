# Demo: Three Live Services (Wallabag + paperless-ngx) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the public demo route captures to three live, self-resetting downstream services — Vikunja (existing), Wallabag (URL → read-later), and a minimal paperless-ngx (uploaded PDF/image → document archive).

**Architecture:** Two code workstreams plus demo infra. (A) A deterministic *has-attachment ⇒ Paperless* routing rule in the classification consumer, a new `PaperlessSkillIntegration`, and an attachment-read method on storage. (B) The existing Wallabag skill gains a self-refreshing OAuth2 token provider (static tokens expire hourly and can't survive the 24/7 demo). (C) Compose services + one-shot bootstrap sidecars + extension of the existing 15-min reset loop, all mirroring the established Vikunja demo pattern (`/bootstrap` shared-volume env files).

**Tech Stack:** .NET 10, MassTransit (in-proc consumers), `IHttpClientFactory`, MudBlazor (unchanged), xUnit + FluentAssertions + NSubstitute + RichardSzalay.MockHttp (unit) / WireMock (contract), Docker Compose, Traefik, paperless-ngx, Wallabag, Redis.

**Spec:** `docs/superpowers/specs/2026-06-10-demo-three-live-services-design.md`

---

## File Structure

**Phase 1 — shared plumbing (attachment routing):**
- Modify `source/FlowHub.Core/Captures/IAttachmentStorage.cs` — add `OpenReadAsync`.
- Modify `source/FlowHub.Persistence/FilesystemAttachmentStorage.cs` — implement `OpenReadAsync`.
- Modify `source/FlowHub.Core/Events/CaptureCreated.cs` — add `bool HasAttachment`.
- Modify `source/FlowHub.Persistence/EfCaptureService.cs` — set `HasAttachment` on publish (two call sites).
- Modify `source/FlowHub.Web/Pipeline/CaptureEnrichmentConsumer.cs` — short-circuit to `Paperless` when `HasAttachment`.

**Phase 2 — Paperless skill:**
- Create `source/FlowHub.Skills/Paperless/PaperlessOptions.cs`
- Create `source/FlowHub.Skills/Paperless/PaperlessSkillIntegration.cs`
- Modify `source/FlowHub.Skills/SkillsServiceCollectionExtensions.cs` — add `AddPaperless`.

**Phase 3 — Wallabag token provider:**
- Modify `source/FlowHub.Skills/Wallabag/WallabagOptions.cs` — OAuth client/user fields.
- Create `source/FlowHub.Skills/Wallabag/WallabagTokenProvider.cs`
- Modify `source/FlowHub.Skills/Wallabag/WallabagSkillIntegration.cs` — use token provider.
- Modify `source/FlowHub.Skills/SkillsServiceCollectionExtensions.cs` — `AddWallabag` rewrite.
- Modify existing Wallabag tests under `tests/FlowHub.Skills.Tests/Wallabag/` and `tests/FlowHub.Skills.ContractTests/Wallabag/`.

**Phase 4 — demo infra:**
- Create `demo/paperless/bootstrap.sh`, `demo/paperless/Dockerfile`
- Create `demo/wallabag/bootstrap.sh`, `demo/wallabag/Dockerfile`
- Modify `demo/docker-compose.yml` — paperless + redis + wallabag services, bootstrap sidecars, web env, depends_on.
- Modify `demo/docker-compose.vps.yml` — Traefik overrides for paperless + wallabag.
- Modify `demo/reset/reset.sh` — clear Wallabag entries + Paperless documents.
- Modify `demo/.env.example` — new demo vars.

---

## Phase 1 — Shared plumbing: has-attachment ⇒ Paperless

### Task 1: Add `OpenReadAsync` to attachment storage

**Files:**
- Modify: `source/FlowHub.Core/Captures/IAttachmentStorage.cs`
- Modify: `source/FlowHub.Persistence/FilesystemAttachmentStorage.cs`
- Test: `tests/FlowHub.Persistence.Tests/FilesystemAttachmentStorageTests.cs` (create if absent)

- [ ] **Step 1: Write the failing test**

```csharp
using FlowHub.Core.Captures;
using FlowHub.Persistence;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace FlowHub.Persistence.Tests;

public sealed class FilesystemAttachmentStorageTests
{
    private static FilesystemAttachmentStorage Build(string root)
    {
        var env = Substitute.For<IHostEnvironment>();
        env.ContentRootPath.Returns(root);
        var opts = Options.Create(new UploadOptions { StoragePath = "uploads" });
        return new FilesystemAttachmentStorage(env, opts);
    }

    [Fact]
    public async Task OpenReadAsync_ReturnsBytesPreviouslySaved()
    {
        var root = Path.Combine(Path.GetTempPath(), $"fh-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var sut = Build(root);
            var bytes = new byte[] { 1, 2, 3, 4 };
            using var input = new MemoryStream(bytes);
            var relative = await sut.SaveAsync(input, "x.pdf", "application/pdf");

            await using var read = await sut.OpenReadAsync(relative);
            using var buffer = new MemoryStream();
            await read.CopyToAsync(buffer);

            buffer.ToArray().Should().Equal(bytes);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FlowHub.Persistence.Tests --filter OpenReadAsync_ReturnsBytesPreviouslySaved`
Expected: FAIL — `IAttachmentStorage` has no `OpenReadAsync`.

- [ ] **Step 3: Add the interface method**

In `source/FlowHub.Core/Captures/IAttachmentStorage.cs`, add inside the interface:

```csharp
    /// <summary>Opens the stored bytes for reading. Caller disposes the stream.</summary>
    Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);
```

- [ ] **Step 4: Implement it**

In `source/FlowHub.Persistence/FilesystemAttachmentStorage.cs`, add:

```csharp
    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var absolute = Path.Combine(AbsoluteRoot(), relativePath);
        Stream stream = File.OpenRead(absolute);
        return Task.FromResult(stream);
    }
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test tests/FlowHub.Persistence.Tests --filter OpenReadAsync_ReturnsBytesPreviouslySaved`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add source/FlowHub.Core/Captures/IAttachmentStorage.cs source/FlowHub.Persistence/FilesystemAttachmentStorage.cs tests/FlowHub.Persistence.Tests/FilesystemAttachmentStorageTests.cs
git commit -m "feat(persistence): add OpenReadAsync to attachment storage"
```

---

### Task 2: Carry `HasAttachment` on `CaptureCreated`

**Files:**
- Modify: `source/FlowHub.Core/Events/CaptureCreated.cs`
- Modify: `source/FlowHub.Persistence/EfCaptureService.cs:42-43` and `:68-69`

- [ ] **Step 1: Add the event field**

In `source/FlowHub.Core/Events/CaptureCreated.cs`:

```csharp
public sealed record CaptureCreated(
    Guid CaptureId,
    string Content,
    ChannelKind Source,
    DateTimeOffset CreatedAt,
    bool HasAttachment = false);
```

- [ ] **Step 2: Set it at both publish sites**

In `source/FlowHub.Persistence/EfCaptureService.cs`, the text `SubmitAsync` publish stays default (`HasAttachment` omitted = false). In the attachment branch, change the publish to:

```csharp
            await _publishEndpoint.Publish(
                new CaptureCreated(saved.Id, saved.Content, saved.Source, saved.CreatedAt, HasAttachment: true),
                cancellationToken);
```

- [ ] **Step 3: Build to verify compilation**

Run: `dotnet build source/FlowHub.Persistence`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add source/FlowHub.Core/Events/CaptureCreated.cs source/FlowHub.Persistence/EfCaptureService.cs
git commit -m "feat(core): flag attachment captures on CaptureCreated"
```

---

### Task 3: Route attachment captures to Paperless (skip the LLM)

**Files:**
- Modify: `source/FlowHub.Web/Pipeline/CaptureEnrichmentConsumer.cs`
- Test: `tests/FlowHub.Web.ComponentTests/Pipeline/CaptureEnrichmentConsumerTests.cs`

- [ ] **Step 1: Write the failing test**

Add to `CaptureEnrichmentConsumerTests`. The classifier is a stub that throws — proving it is never called for attachments:

```csharp
    [Fact]
    public async Task Consume_AttachmentCapture_RoutesToPaperless_WithoutCallingClassifier()
    {
        var classifier = Substitute.For<IClassifier>();
        classifier.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<ClassificationResult>>(_ => throw new InvalidOperationException("classifier must not be called for attachments"));

        await using var provider = PipelineTestBase.Build(
            configure: s => s.AddSingleton(classifier),
            configureBus: x => x.AddConsumer<CaptureEnrichmentConsumer>());

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var captureService = provider.GetRequiredService<ICaptureService>();
        using var bytes = new MemoryStream(new byte[] { 1, 2, 3 });
        var capture = await captureService.SubmitAsync(
            content: null, ChannelKind.Web,
            new AttachmentInput { Content = bytes, FileName = "scan.pdf", ContentType = "application/pdf", SizeBytes = 3 },
            default);

        (await harness.Published.Any<CaptureClassified>(
            x => x.Context.Message.CaptureId == capture.Id
                && x.Context.Message.MatchedSkill == "Paperless"))
            .Should().BeTrue();

        var stored = await captureService.GetByIdAsync(capture.Id, default);
        stored!.MatchedSkill.Should().Be("Paperless");
    }
```

> Note: `PipelineTestBase` must provide a real `IAttachmentStorage` (filesystem under a temp content root) so `SubmitAsync(attachment)` works. If it does not already, register `FilesystemAttachmentStorage` + a temp `UploadOptions`/`IHostEnvironment` in `PipelineTestBase.Build`. Inspect `tests/FlowHub.Web.ComponentTests/Pipeline/PipelineTestBase.cs` first; add the registration only if absent.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter Consume_AttachmentCapture_RoutesToPaperless_WithoutCallingClassifier`
Expected: FAIL — classifier throws (today the consumer always classifies).

- [ ] **Step 3: Add the short-circuit**

In `source/FlowHub.Web/Pipeline/CaptureEnrichmentConsumer.cs`, at the top of `Consume`, before `_classifier.ClassifyAsync`:

```csharp
        var msg = context.Message;
        var ct = context.CancellationToken;

        if (msg.HasAttachment)
        {
            await _captureService.MarkClassifiedAsync(msg.CaptureId, "Paperless", title: null, ct: ct);
            await context.Publish(new CaptureClassified(
                msg.CaptureId,
                Tags: ["document"],
                MatchedSkill: "Paperless",
                ClassifiedAt: DateTimeOffset.UtcNow), ct);
            return;
        }

        var result = await _classifier.ClassifyAsync(msg.Content, ct);
```

> The existing `MarkClassifiedAsync` signature is `(id, matchedSkill, title, vikunjaProject, enrichmentDescription, ct)` — pass `title: null` and rely on defaults for the rest; the named `ct:` argument targets the trailing `CancellationToken`.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter Consume_AttachmentCapture_RoutesToPaperless_WithoutCallingClassifier`
Expected: PASS

- [ ] **Step 5: Run the existing consumer tests to confirm no regression**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter CaptureEnrichmentConsumerTests`
Expected: PASS (all)

- [ ] **Step 6: Commit**

```bash
git add source/FlowHub.Web/Pipeline/CaptureEnrichmentConsumer.cs tests/FlowHub.Web.ComponentTests/Pipeline/CaptureEnrichmentConsumerTests.cs
git commit -m "feat(pipeline): route attachment captures to Paperless without LLM"
```

---

## Phase 2 — Paperless skill

### Task 4: `PaperlessOptions`

**Files:**
- Create: `source/FlowHub.Skills/Paperless/PaperlessOptions.cs`

- [ ] **Step 1: Write the options class**

```csharp
namespace FlowHub.Skills.Paperless;

/// <summary>
/// Bound from configuration section <c>Skills:Paperless</c>.
/// </summary>
public sealed class PaperlessOptions
{
    public const string SectionName = "Skills:Paperless";

    public string? BaseUrl { get; set; }
    public string? ApiToken { get; set; }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build source/FlowHub.Skills`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add source/FlowHub.Skills/Paperless/PaperlessOptions.cs
git commit -m "feat(skills): add PaperlessOptions"
```

---

### Task 5: `PaperlessSkillIntegration`

**Files:**
- Create: `source/FlowHub.Skills/Paperless/PaperlessSkillIntegration.cs`
- Test: `tests/FlowHub.Skills.Tests/Paperless/PaperlessSkillIntegrationTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using System.Net;
using FlowHub.Core.Captures;
using FlowHub.Skills.Paperless;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using RichardSzalay.MockHttp;

namespace FlowHub.Skills.Tests.Paperless;

public sealed class PaperlessSkillIntegrationTests
{
    private static (PaperlessSkillIntegration sut, MockHttpMessageHandler mock, IAttachmentStorage storage) Build()
    {
        var options = new PaperlessOptions { BaseUrl = "https://paperless.example.com", ApiToken = "tok" };
        var mock = new MockHttpMessageHandler();
        var http = mock.ToHttpClient();
        http.BaseAddress = new Uri(options.BaseUrl!);
        var storage = Substitute.For<IAttachmentStorage>();
        var sut = new PaperlessSkillIntegration(http, Options.Create(options), storage, NullLogger<PaperlessSkillIntegration>.Instance);
        return (sut, mock, storage);
    }

    private static Capture DocCapture(Attachment? attachment) => new(
        Id: Guid.NewGuid(),
        Source: ChannelKind.Web,
        Content: "scan.pdf",
        CreatedAt: DateTimeOffset.UtcNow,
        Stage: LifecycleStage.Classified,
        MatchedSkill: "Paperless",
        Attachment: attachment);

    [Fact]
    public void Name_IsPaperless()
    {
        var (sut, _, _) = Build();
        sut.Name.Should().Be("Paperless");
    }

    [Fact]
    public async Task HandleAsync_PostsMultipartWithTokenHeader_ReturnsExternalRef()
    {
        var (sut, mock, storage) = Build();
        storage.OpenReadAsync("2026/06/abc.pdf", Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<Stream>(new MemoryStream(new byte[] { 1, 2, 3 })));

        mock.Expect(HttpMethod.Post, "https://paperless.example.com/api/documents/post_document/")
            .WithHeaders("Authorization", "Token tok")
            .Respond("application/json", "\"d9b8...uuid\"");

        var capture = DocCapture(new Attachment("scan.pdf", "application/pdf", 3, "2026/06/abc.pdf", DateTimeOffset.UtcNow));

        var result = await sut.HandleAsync(capture, default);

        result.Success.Should().BeTrue();
        result.ExternalRef.Should().Be("d9b8...uuid");
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task HandleAsync_NoAttachment_Throws()
    {
        var (sut, _, _) = Build();
        var act = () => sut.HandleAsync(DocCapture(attachment: null), default);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task HandleAsync_ServerReturns503_ThrowsHttpRequestException()
    {
        var (sut, mock, storage) = Build();
        storage.OpenReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<Stream>(new MemoryStream(new byte[] { 1 })));
        mock.Expect(HttpMethod.Post, "*/api/documents/post_document/").Respond(HttpStatusCode.ServiceUnavailable);

        var capture = DocCapture(new Attachment("scan.pdf", "application/pdf", 1, "p.pdf", DateTimeOffset.UtcNow));
        var act = () => sut.HandleAsync(capture, default);
        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
```

> paperless-ngx's `POST /api/documents/post_document/` returns the consume **task UUID** as a JSON string (quoted). The skill returns that string as `ExternalRef`. The async OCR consume step is fire-and-forget for the demo — a 200 with a task id is success.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/FlowHub.Skills.Tests --filter PaperlessSkillIntegrationTests`
Expected: FAIL — type does not exist.

- [ ] **Step 3: Implement the skill**

```csharp
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FlowHub.Core.Captures;
using FlowHub.Core.Skills;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlowHub.Skills.Paperless;

public sealed class PaperlessSkillIntegration : ISkillIntegration
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly PaperlessOptions _options;
    private readonly IAttachmentStorage _storage;
    private readonly ILogger<PaperlessSkillIntegration> _log;

    public PaperlessSkillIntegration(
        HttpClient http,
        IOptions<PaperlessOptions> options,
        IAttachmentStorage storage,
        ILogger<PaperlessSkillIntegration> log)
    {
        _http = http;
        _options = options.Value;
        _storage = storage;
        _log = log;
    }

    public string Name => "Paperless";

    public async Task<SkillResult> HandleAsync(Capture capture, CancellationToken cancellationToken)
    {
        if (capture.Attachment is null)
        {
            throw new InvalidOperationException($"Capture {capture.Id} routed to Paperless has no attachment.");
        }

        var att = capture.Attachment;
        await using var bytes = await _storage.OpenReadAsync(att.RelativePath, cancellationToken);

        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(att.ContentType);
        content.Add(fileContent, "document", att.FileName);
        content.Add(new StringContent(capture.Title ?? att.FileName), "title");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/documents/post_document/")
        {
            Content = content,
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Token", _options.ApiToken);

        using var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        // post_document returns the consume task UUID as a quoted JSON string.
        var taskId = await response.Content.ReadFromJsonAsync<string>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Paperless response body was empty.");

        return new SkillResult(Success: true, ExternalRef: taskId);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/FlowHub.Skills.Tests --filter PaperlessSkillIntegrationTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add source/FlowHub.Skills/Paperless/PaperlessSkillIntegration.cs tests/FlowHub.Skills.Tests/Paperless/PaperlessSkillIntegrationTests.cs
git commit -m "feat(skills): add Paperless document-upload skill"
```

---

### Task 6: Register Paperless in DI

**Files:**
- Modify: `source/FlowHub.Skills/SkillsServiceCollectionExtensions.cs`
- Test: `tests/FlowHub.Skills.Tests/SkillsServiceCollectionExtensionsTests.cs`

- [ ] **Step 1: Write the failing test**

Add to `SkillsServiceCollectionExtensionsTests` (mirror the existing Wallabag/Vikunja registration tests already in that file — read it first to match the harness helpers):

```csharp
    [Fact]
    public void AddFlowHubSkills_WithPaperlessConfigured_RegistersPaperlessIntegration()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Skills:Paperless:BaseUrl"] = "https://paperless.example.com",
            ["Skills:Paperless:ApiToken"] = "tok",
        }).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAttachmentStorage>(NSubstitute.Substitute.For<IAttachmentStorage>());
        services.AddFlowHubSkills(config);

        using var provider = services.BuildServiceProvider();
        provider.GetServices<ISkillIntegration>()
            .Should().Contain(s => s.Name == "Paperless");
    }

    [Fact]
    public void AddFlowHubSkills_WithoutPaperlessConfig_DoesNotRegisterPaperless()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFlowHubSkills(config);

        using var provider = services.BuildServiceProvider();
        provider.GetServices<ISkillIntegration>()
            .Should().NotContain(s => s.Name == "Paperless");
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FlowHub.Skills.Tests --filter Paperless`
Expected: FAIL — Paperless never registered.

- [ ] **Step 3: Add the registration**

In `source/FlowHub.Skills/SkillsServiceCollectionExtensions.cs`, add `using FlowHub.Skills.Paperless;`, call `AddPaperless(services, configuration);` inside `AddFlowHubSkills`, and add:

```csharp
    private static void AddPaperless(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(PaperlessOptions.SectionName);
        var options = section.Get<PaperlessOptions>() ?? new PaperlessOptions();

        if (string.IsNullOrWhiteSpace(options.BaseUrl) || string.IsNullOrWhiteSpace(options.ApiToken))
        {
            services.AddSingleton(new SkillsRegistrationOutcome("Paperless", Registered: false,
                Reason: string.IsNullOrWhiteSpace(options.BaseUrl) ? "missing-base-url" : "missing-api-token"));
            return;
        }

        services.Configure<PaperlessOptions>(section);
        services.AddHttpClient<PaperlessSkillIntegration>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl!);
            client.Timeout = TimeSpan.FromSeconds(30); // OCR upload accepts larger bodies
        });
        services.AddSingleton<ISkillIntegration>(sp => sp.GetRequiredService<PaperlessSkillIntegration>());
        services.AddSingleton(new SkillsRegistrationOutcome("Paperless", Registered: true, Reason: "configured"));
    }
```

> `PaperlessSkillIntegration` depends on `IAttachmentStorage`, which is registered by the Web/Persistence host (not by the Skills module). The unit test above registers a substitute to satisfy the graph; in production it is already present.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/FlowHub.Skills.Tests --filter Paperless`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add source/FlowHub.Skills/SkillsServiceCollectionExtensions.cs tests/FlowHub.Skills.Tests/SkillsServiceCollectionExtensionsTests.cs
git commit -m "feat(skills): register Paperless integration when configured"
```

---

## Phase 3 — Wallabag self-refreshing token

### Task 7: Replace static token with OAuth fields on `WallabagOptions`

**Files:**
- Modify: `source/FlowHub.Skills/Wallabag/WallabagOptions.cs`

- [ ] **Step 1: Rewrite the options**

```csharp
namespace FlowHub.Skills.Wallabag;

/// <summary>
/// Bound from configuration section <c>Skills:Wallabag</c>.
/// Wallabag's API is OAuth2 (password grant); access tokens expire (~1h), so the
/// skill obtains and refreshes its own token from these credentials.
/// </summary>
public sealed class WallabagOptions
{
    public const string SectionName = "Skills:Wallabag";

    public string? BaseUrl { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}
```

- [ ] **Step 2: Build (expect failures in dependents — that's fine, next tasks fix them)**

Run: `dotnet build source/FlowHub.Skills`
Expected: FAIL — `ApiToken` references in `WallabagSkillIntegration` / registration no longer compile. Proceed to Task 8.

- [ ] **Step 3: Commit (WIP-safe — committed together with Task 8/9 if you prefer; otherwise stage now)**

Defer commit until Task 9 builds green. (No commit this step.)

---

### Task 8: `WallabagTokenProvider`

**Files:**
- Create: `source/FlowHub.Skills/Wallabag/WallabagTokenProvider.cs`
- Test: `tests/FlowHub.Skills.Tests/Wallabag/WallabagTokenProviderTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using FlowHub.Skills.Wallabag;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;

namespace FlowHub.Skills.Tests.Wallabag;

public sealed class WallabagTokenProviderTests
{
    private static (WallabagTokenProvider sut, MockHttpMessageHandler mock) Build(TimeProvider time)
    {
        var options = new WallabagOptions
        {
            BaseUrl = "https://wallabag.example.com",
            ClientId = "cid", ClientSecret = "secret", Username = "u", Password = "p",
        };
        var mock = new MockHttpMessageHandler();
        var http = mock.ToHttpClient();
        http.BaseAddress = new Uri(options.BaseUrl!);
        return (new WallabagTokenProvider(http, Options.Create(options), time, NullLogger<WallabagTokenProvider>.Instance), mock);
    }

    private static void ExpectTokenGrant(MockHttpMessageHandler mock, string token, int expiresIn) =>
        mock.Expect(HttpMethod.Post, "*/oauth/v2/token")
            .Respond("application/json", $$"""{"access_token":"{{token}}","expires_in":{{expiresIn}},"token_type":"bearer","refresh_token":"r"}""");

    [Fact]
    public async Task GetTokenAsync_FirstCall_FetchesToken()
    {
        var time = new FakeTimeProvider();
        var (sut, mock) = Build(time);
        ExpectTokenGrant(mock, "tok-1", 3600);

        var token = await sut.GetTokenAsync(default);

        token.Should().Be("tok-1");
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetTokenAsync_SecondCallBeforeExpiry_ReusesCachedToken()
    {
        var time = new FakeTimeProvider();
        var (sut, mock) = Build(time);
        ExpectTokenGrant(mock, "tok-1", 3600); // exactly one grant expected

        var first = await sut.GetTokenAsync(default);
        time.Advance(TimeSpan.FromMinutes(30));
        var second = await sut.GetTokenAsync(default);

        first.Should().Be("tok-1");
        second.Should().Be("tok-1");
        mock.VerifyNoOutstandingExpectation(); // no second grant
    }

    [Fact]
    public async Task GetTokenAsync_AfterExpiry_RefetchesToken()
    {
        var time = new FakeTimeProvider();
        var (sut, mock) = Build(time);
        ExpectTokenGrant(mock, "tok-1", 3600);
        ExpectTokenGrant(mock, "tok-2", 3600);

        var first = await sut.GetTokenAsync(default);
        time.Advance(TimeSpan.FromMinutes(59)); // inside the 60s safety margin of a 3600s token
        var second = await sut.GetTokenAsync(default);

        first.Should().Be("tok-1");
        second.Should().Be("tok-2");
        mock.VerifyNoOutstandingExpectation();
    }
}
```

> `FakeTimeProvider` lives in `Microsoft.Extensions.TimeProvider.Testing` — already used by the Vikunja catalog tests. Confirm the package is referenced by `FlowHub.Skills.Tests` (the Vikunja tests there use it); if not, the Vikunja catalog tests are in a different project — in that case use a minimal hand-rolled `TimeProvider` stub.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/FlowHub.Skills.Tests --filter WallabagTokenProviderTests`
Expected: FAIL — type does not exist.

- [ ] **Step 3: Implement the provider**

```csharp
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlowHub.Skills.Wallabag;

/// <summary>
/// Obtains and caches a Wallabag OAuth2 access token via the password grant,
/// refreshing shortly before expiry. Single-flight via a semaphore.
/// </summary>
public sealed class WallabagTokenProvider : IDisposable
{
    private static readonly TimeSpan SafetyMargin = TimeSpan.FromSeconds(60);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly WallabagOptions _options;
    private readonly TimeProvider _time;
    private readonly ILogger<WallabagTokenProvider> _log;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private string? _token;
    private DateTimeOffset _expiresAt;

    public WallabagTokenProvider(
        HttpClient http,
        IOptions<WallabagOptions> options,
        TimeProvider time,
        ILogger<WallabagTokenProvider> log)
    {
        _http = http;
        _options = options.Value;
        _time = time;
        _log = log;
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow();
        if (_token is not null && now < _expiresAt - SafetyMargin)
        {
            return _token;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            now = _time.GetUtcNow();
            if (_token is not null && now < _expiresAt - SafetyMargin)
            {
                return _token;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "/oauth/v2/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "password",
                    ["client_id"] = _options.ClientId ?? "",
                    ["client_secret"] = _options.ClientSecret ?? "",
                    ["username"] = _options.Username ?? "",
                    ["password"] = _options.Password ?? "",
                }),
            };

            using var response = await _http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var grant = await response.Content.ReadFromJsonAsync<TokenGrant>(JsonOptions, cancellationToken)
                ?? throw new InvalidOperationException("Wallabag token response was empty.");
            if (string.IsNullOrEmpty(grant.AccessToken))
            {
                throw new InvalidOperationException("Wallabag token response had no access_token.");
            }

            _token = grant.AccessToken;
            _expiresAt = now + TimeSpan.FromSeconds(grant.ExpiresIn);
            return _token;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();

    private sealed record TokenGrant(string? AccessToken, int ExpiresIn);
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/FlowHub.Skills.Tests --filter WallabagTokenProviderTests`
Expected: PASS

- [ ] **Step 5: Commit (with Task 7's option change, now consistent)**

```bash
git add source/FlowHub.Skills/Wallabag/WallabagOptions.cs source/FlowHub.Skills/Wallabag/WallabagTokenProvider.cs tests/FlowHub.Skills.Tests/Wallabag/WallabagTokenProviderTests.cs
git commit -m "feat(skills): add Wallabag OAuth token provider"
```

---

### Task 9: Use the token provider in `WallabagSkillIntegration` + re-register

**Files:**
- Modify: `source/FlowHub.Skills/Wallabag/WallabagSkillIntegration.cs`
- Modify: `source/FlowHub.Skills/SkillsServiceCollectionExtensions.cs` (`AddWallabag`)
- Modify: `tests/FlowHub.Skills.Tests/Wallabag/WallabagSkillIntegrationTests.cs`
- Modify: `tests/FlowHub.Skills.ContractTests/Wallabag/WallabagContractTests.cs`

- [ ] **Step 1: Update the skill to fetch the bearer token per call**

Constructor takes `WallabagTokenProvider` instead of `IOptions<WallabagOptions>`. In `HandleAsync`, before building the request:

```csharp
        var token = await _tokenProvider.GetTokenAsync(cancellationToken);
        // ...
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
```

Full constructor:

```csharp
    private readonly HttpClient _http;
    private readonly WallabagTokenProvider _tokenProvider;
    private readonly ILogger<WallabagSkillIntegration> _log;

    public WallabagSkillIntegration(
        HttpClient http,
        WallabagTokenProvider tokenProvider,
        ILogger<WallabagSkillIntegration> log)
    {
        _http = http;
        _tokenProvider = tokenProvider;
        _log = log;
    }
```

- [ ] **Step 2: Update the existing unit tests to the new constructor**

In `tests/FlowHub.Skills.Tests/Wallabag/WallabagSkillIntegrationTests.cs`, the `Build` helper must construct a real `WallabagTokenProvider` whose `/oauth/v2/token` is stubbed on the same mock handler. Replace the helper:

```csharp
    private static (WallabagSkillIntegration sut, MockHttpMessageHandler mock) Build()
    {
        var options = new WallabagOptions
        {
            BaseUrl = "https://wallabag.example.com",
            ClientId = "cid", ClientSecret = "secret", Username = "u", Password = "p",
        };
        var mock = new MockHttpMessageHandler();
        var http = mock.ToHttpClient();
        http.BaseAddress = new Uri(options.BaseUrl!);
        // Token grant is needed by every call; respond generously.
        mock.When(HttpMethod.Post, "*/oauth/v2/token")
            .Respond("application/json", """{"access_token":"test-token","expires_in":3600,"token_type":"bearer","refresh_token":"r"}""");
        var tokenProvider = new WallabagTokenProvider(http, Options.Create(options), TimeProvider.System, NullLogger<WallabagTokenProvider>.Instance);
        return (new WallabagSkillIntegration(http, tokenProvider, NullLogger<WallabagSkillIntegration>.Instance), mock);
    }
```

> The existing assertions that check `Bearer test-token` still hold because the stubbed grant returns `test-token`. Tests using `mock.Expect(...)` for the entries POST keep working; switch the token stub to `mock.When(...)` (unordered, reusable) so it doesn't interfere with `VerifyNoOutstandingExpectation()`.

- [ ] **Step 3: Update the contract test similarly**

In `tests/FlowHub.Skills.ContractTests/Wallabag/WallabagContractTests.cs`, register a WireMock stub for `POST /oauth/v2/token` returning an access token, and build the SUT with a `WallabagTokenProvider` (BaseUrl = `_wire.BaseUrl`, dummy client/user creds, `TimeProvider.System`). Keep the existing entry-post assertion but expect `Authorization: Bearer <granted-token>`.

- [ ] **Step 4: Rewrite `AddWallabag` registration**

In `source/FlowHub.Skills/SkillsServiceCollectionExtensions.cs`:

```csharp
    private static void AddWallabag(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(WallabagOptions.SectionName);
        var options = section.Get<WallabagOptions>() ?? new WallabagOptions();

        string? reason = null;
        if (string.IsNullOrWhiteSpace(options.BaseUrl)) { reason = "missing-base-url"; }
        else if (string.IsNullOrWhiteSpace(options.ClientId)) { reason = "missing-client-id"; }
        else if (string.IsNullOrWhiteSpace(options.Username)) { reason = "missing-username"; }

        if (reason is not null)
        {
            services.AddSingleton(new SkillsRegistrationOutcome("Wallabag", Registered: false, Reason: reason));
            return;
        }

        services.Configure<WallabagOptions>(section);
        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpClient<WallabagTokenProvider>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl!);
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddHttpClient<WallabagSkillIntegration>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl!);
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddSingleton<ISkillIntegration>(sp => sp.GetRequiredService<WallabagSkillIntegration>());
        services.AddSingleton(new SkillsRegistrationOutcome("Wallabag", Registered: true, Reason: "configured"));
    }
```

> Add `using Microsoft.Extensions.DependencyInjection.Extensions;` if not present (it is — Vikunja uses `TryAddSingleton`). `WallabagTokenProvider` is registered as a typed `HttpClient`, so it resolves as a transient-per-client; the in-memory token cache then lives per resolution. Since `WallabagSkillIntegration` is a singleton holding one `WallabagTokenProvider`, register the provider so the singleton captures a single instance — resolve it via the singleton graph (typed client registration returns the same instance to the singleton consumer for the lifetime of that consumer). Verify with the registration test below.

- [ ] **Step 5: Update the registration test**

In `SkillsServiceCollectionExtensionsTests`, update the Wallabag "configured" test inputs to the new keys:

```csharp
            ["Skills:Wallabag:BaseUrl"] = "https://wallabag.example.com",
            ["Skills:Wallabag:ClientId"] = "cid",
            ["Skills:Wallabag:ClientSecret"] = "secret",
            ["Skills:Wallabag:Username"] = "u",
            ["Skills:Wallabag:Password"] = "p",
```

- [ ] **Step 6: Build + run all Skills tests**

Run: `dotnet build source/FlowHub.Skills && dotnet test tests/FlowHub.Skills.Tests`
Expected: Build succeeded; all PASS.

- [ ] **Step 7: Run contract tests**

Run: `dotnet test tests/FlowHub.Skills.ContractTests --filter Wallabag`
Expected: PASS

- [ ] **Step 8: Commit**

```bash
git add source/FlowHub.Skills/Wallabag/WallabagSkillIntegration.cs source/FlowHub.Skills/SkillsServiceCollectionExtensions.cs tests/FlowHub.Skills.Tests tests/FlowHub.Skills.ContractTests/Wallabag
git commit -m "refactor(skills): drive Wallabag with self-refreshing OAuth token"
```

---

### Task 10: Full solution build + test gate

- [ ] **Step 1: Build the whole solution (warnings are errors)**

Run: `dotnet build FlowHub.slnx`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 2: Run the full suite**

Run: `dotnet test FlowHub.slnx`
Expected: all green. Fix any fallout before proceeding to infra.

- [ ] **Step 3: Commit (only if fixes were needed)**

```bash
git commit -am "test: reconcile suite after three-service routing changes"
```

---

## Phase 4 — Demo infrastructure

> Infra tasks are not unit-testable; each ends with a concrete verification command. The bootstrap scripts mirror `demo/vikunja/bootstrap.sh` (wait-for-API → provision → write `/bootstrap/<svc>.env`).

### Task 11: paperless-ngx bootstrap sidecar

**Files:**
- Create: `demo/paperless/Dockerfile`
- Create: `demo/paperless/bootstrap.sh`

- [ ] **Step 1: Write `demo/paperless/Dockerfile`**

```dockerfile
# demo/paperless/Dockerfile — one-shot provisioner for the demo paperless-ngx.
FROM alpine:3.20
RUN apk add --no-cache bash curl jq ca-certificates
COPY bootstrap.sh /bootstrap.sh
RUN chmod +x /bootstrap.sh
ENTRYPOINT ["/bootstrap.sh"]
```

- [ ] **Step 2: Write `demo/paperless/bootstrap.sh`**

```bash
#!/usr/bin/env bash
# demo/paperless/bootstrap.sh — provision the demo paperless-ngx instance.
# Waits for the API, obtains an API token for the auto-created admin, and writes
# /bootstrap/paperless.env (sourced by flowhub.web to activate the Paperless skill
# and by the reset sidecar to clear documents each cycle).
set -euo pipefail
ts() { date -u +%Y-%m-%dT%H:%M:%SZ; }
log() { echo "[$(ts)] paperless-bootstrap: $*"; }

API="${PAPERLESS_API_URL:-http://paperless:8000}"
USER="${PAPERLESS_ADMIN_USER:-flowhub}"
PASS="${PAPERLESS_ADMIN_PASSWORD:-flowhub-demo}"
OUT="${BOOTSTRAP_OUT:-/bootstrap/paperless.env}"

log "waiting for ${API}/api/ ..."
ready=""
for _ in $(seq 1 120); do
  if curl -fsS "${API}/api/" >/dev/null 2>&1; then ready=1; break; fi
  sleep 3
done
[ -n "${ready}" ] || { log "ERROR: paperless did not become ready"; exit 1; }
log "paperless is up"

# Obtain an API token for the admin (auto-created via PAPERLESS_ADMIN_* on first boot).
TOKEN=$(curl -fsS -X POST "${API}/api/token/" \
  -H 'Content-Type: application/json' \
  -d "{\"username\":\"${USER}\",\"password\":\"${PASS}\"}" | jq -r '.token // empty')
[ -n "${TOKEN}" ] || { log "ERROR: could not obtain API token"; exit 1; }
log "obtained API token"

mkdir -p "$(dirname "${OUT}")"
cat > "${OUT}" <<EOF
# Generated by demo/paperless/bootstrap.sh — do not edit by hand.
Skills__Paperless__BaseUrl=${API}
Skills__Paperless__ApiToken=${TOKEN}
PAPERLESS_API_URL=${API}
PAPERLESS_TOKEN=${TOKEN}
EOF
log "wrote ${OUT}"
log "done"
```

- [ ] **Step 3: Lint the script**

Run: `bash -n demo/paperless/bootstrap.sh`
Expected: no output (syntax OK).

- [ ] **Step 4: Commit**

```bash
git add demo/paperless/
git commit -m "feat(demo): add paperless-ngx bootstrap sidecar"
```

---

### Task 12: Wallabag bootstrap sidecar

**Files:**
- Create: `demo/wallabag/Dockerfile`
- Create: `demo/wallabag/bootstrap.sh`

- [ ] **Step 1: Write `demo/wallabag/Dockerfile`**

```dockerfile
# demo/wallabag/Dockerfile — one-shot provisioner for the demo Wallabag.
FROM alpine:3.20
RUN apk add --no-cache bash curl jq ca-certificates
COPY bootstrap.sh /bootstrap.sh
RUN chmod +x /bootstrap.sh
ENTRYPOINT ["/bootstrap.sh"]
```

- [ ] **Step 2: Write `demo/wallabag/bootstrap.sh`**

```bash
#!/usr/bin/env bash
# demo/wallabag/bootstrap.sh — provision the demo Wallabag instance.
# Waits for the API, creates an OAuth API client via the Wallabag console
# (executed in the wallabag container), and writes /bootstrap/wallabag.env with
# the client credentials + demo user login. The FlowHub Wallabag skill performs
# the password grant itself (tokens expire hourly), so only credentials are stored.
set -euo pipefail
ts() { date -u +%Y-%m-%dT%H:%M:%SZ; }
log() { echo "[$(ts)] wallabag-bootstrap: $*"; }

API="${WALLABAG_API_URL:-http://wallabag:80}"
USER="${WALLABAG_DEMO_USER:-flowhub}"
PASS="${WALLABAG_DEMO_PASSWORD:-flowhub-demo}"
CLIENT_ID="${WALLABAG_CLIENT_ID:?WALLABAG_CLIENT_ID required}"
CLIENT_SECRET="${WALLABAG_CLIENT_SECRET:?WALLABAG_CLIENT_SECRET required}"
OUT="${BOOTSTRAP_OUT:-/bootstrap/wallabag.env}"

log "waiting for ${API}/api/info ..."
ready=""
for _ in $(seq 1 120); do
  if curl -fsS "${API}/api/info" >/dev/null 2>&1; then ready=1; break; fi
  sleep 3
done
[ -n "${ready}" ] || { log "ERROR: wallabag did not become ready"; exit 1; }
log "wallabag is up"

# Verify the credentials yield a token (fail fast if the client/user is wrong).
code=$(curl -sS -o /tmp/tok.json -w '%{http_code}' -X POST "${API}/oauth/v2/token" \
  -d "grant_type=password&client_id=${CLIENT_ID}&client_secret=${CLIENT_SECRET}&username=${USER}&password=${PASS}" || true)
if [ "${code}" != "200" ]; then
  log "ERROR: token grant failed (HTTP ${code}) — check WALLABAG_CLIENT_* + demo user"; cat /tmp/tok.json; exit 1
fi
log "verified password grant"

mkdir -p "$(dirname "${OUT}")"
cat > "${OUT}" <<EOF
# Generated by demo/wallabag/bootstrap.sh — do not edit by hand.
Skills__Wallabag__BaseUrl=${API}
Skills__Wallabag__ClientId=${CLIENT_ID}
Skills__Wallabag__ClientSecret=${CLIENT_SECRET}
Skills__Wallabag__Username=${USER}
Skills__Wallabag__Password=${PASS}
WALLABAG_API_URL=${API}
WALLABAG_CLIENT_ID=${CLIENT_ID}
WALLABAG_CLIENT_SECRET=${CLIENT_SECRET}
WALLABAG_USER=${USER}
WALLABAG_PASSWORD=${PASS}
EOF
log "wrote ${OUT}"
log "done"
```

> **Client creation:** Wallabag's API client (`WALLABAG_CLIENT_ID`/`SECRET`) is created once via `php bin/console wallabag:client:create` inside the wallabag container. For the demo we pin them as compose env vars and let the Wallabag image's `FOSUserBundle` + `wallabag:install` provision the user; if the chosen image cannot pre-seed a client non-interactively, add a `command:` step on the `wallabag` service that runs the console create and echoes the id/secret into the shared volume. Confirm the exact mechanism against the pinned `wallabag/wallabag` image tag during Task 13.

- [ ] **Step 3: Lint the script**

Run: `bash -n demo/wallabag/bootstrap.sh`
Expected: no output.

- [ ] **Step 4: Commit**

```bash
git add demo/wallabag/
git commit -m "feat(demo): add wallabag bootstrap sidecar"
```

---

### Task 13: Wire services into `demo/docker-compose.yml`

**Files:**
- Modify: `demo/docker-compose.yml`

- [ ] **Step 1: Stop forcing Wallabag empty + add Paperless note in `flowhub.web.environment`**

Remove the two lines:

```yaml
      Skills__Wallabag__BaseUrl: ""
      Skills__Wallabag__ApiToken: ""
```

(Live Wallabag + Paperless config is injected at runtime from `/bootstrap/*.env` via the entrypoint wrapper — same as Vikunja.)

- [ ] **Step 2: Extend the entrypoint to source all three env files**

Replace the `flowhub.web.entrypoint` with:

```yaml
    entrypoint: ["/bin/sh", "-c", "set -a; for f in /bootstrap/vikunja.env /bootstrap/wallabag.env /bootstrap/paperless.env; do [ -f \"$f\" ] && . \"$f\"; done; set +a; exec dotnet FlowHub.Web.dll"]
```

- [ ] **Step 3: Add bootstrap volumes + depends_on to `flowhub.web`**

Under `flowhub.web.volumes` add `- wallabag-bootstrap:/bootstrap-wb:ro` is **not** needed — keep a single shared `vikunja-bootstrap` volume mounted at `/bootstrap` and have all three bootstraps write into it. So all bootstrap sidecars mount the **same** `vikunja-bootstrap` volume at `/bootstrap`. Add to `flowhub.web.depends_on`:

```yaml
      flowhub.wallabag-bootstrap:
        condition: service_completed_successfully
      flowhub.paperless-bootstrap:
        condition: service_completed_successfully
```

> Rename is optional; to avoid churn keep the volume named `vikunja-bootstrap` but treat it as the shared bootstrap volume. (A clarifying comment in the compose file is enough.)

- [ ] **Step 4: Add the `redis`, `paperless`, `wallabag` services + their bootstrap sidecars**

Append services:

```yaml
  redis:
    image: redis:7-alpine
    restart: unless-stopped
    ports: !reset []

  paperless:
    image: ${PAPERLESS_IMAGE:-ghcr.io/paperless-ngx/paperless-ngx:2.13}
    restart: unless-stopped
    depends_on:
      redis:
        condition: service_started
    environment:
      PAPERLESS_REDIS: redis://redis:6379
      PAPERLESS_DBENGINE: sqlite
      PAPERLESS_ADMIN_USER: ${PAPERLESS_ADMIN_USER:-flowhub}
      PAPERLESS_ADMIN_PASSWORD: ${PAPERLESS_ADMIN_PASSWORD:-flowhub-demo}
      PAPERLESS_URL: ${PAPERLESS_PUBLIC_URL:-https://paperless.demo.flowhub.freaxnx01.ch}
      PAPERLESS_TIME_ZONE: Europe/Zurich
      PAPERLESS_OCR_LANGUAGE: eng+deu
    volumes:
      - paperless-data:/usr/src/paperless/data
      - paperless-media:/usr/src/paperless/media
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.flowhub-paperless.rule=Host(`paperless.demo.flowhub.freaxnx01.ch`)"
      - "traefik.http.routers.flowhub-paperless.entrypoints=websecure"
      - "traefik.http.routers.flowhub-paperless.tls=true"
      - "traefik.http.routers.flowhub-paperless.tls.certresolver=letsencrypt"
      - "traefik.http.services.flowhub-paperless.loadbalancer.server.port=8000"
      - "traefik.docker.network=${TRAEFIK_NETWORK:-traefik_public}"
    networks: [default, traefik_public]

  flowhub.paperless-bootstrap:
    build:
      context: demo/paperless
    image: flowhub-paperless-bootstrap:local
    environment:
      PAPERLESS_API_URL: http://paperless:8000
      PAPERLESS_ADMIN_USER: ${PAPERLESS_ADMIN_USER:-flowhub}
      PAPERLESS_ADMIN_PASSWORD: ${PAPERLESS_ADMIN_PASSWORD:-flowhub-demo}
      BOOTSTRAP_OUT: /bootstrap/paperless.env
    volumes:
      - vikunja-bootstrap:/bootstrap
    depends_on:
      - paperless
    restart: "no"

  wallabag:
    image: ${WALLABAG_IMAGE:-wallabag/wallabag:2.6.10}
    restart: unless-stopped
    environment:
      SYMFONY__ENV__DATABASE_DRIVER: pdo_sqlite
      SYMFONY__ENV__DOMAIN_NAME: ${WALLABAG_PUBLIC_URL:-https://wallabag.demo.flowhub.freaxnx01.ch}
      SYMFONY__ENV__FOSUSER_REGISTRATION: "false"
    volumes:
      - wallabag-data:/var/www/wallabag/data
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.flowhub-wallabag.rule=Host(`wallabag.demo.flowhub.freaxnx01.ch`)"
      - "traefik.http.routers.flowhub-wallabag.entrypoints=websecure"
      - "traefik.http.routers.flowhub-wallabag.tls=true"
      - "traefik.http.routers.flowhub-wallabag.tls.certresolver=letsencrypt"
      - "traefik.http.services.flowhub-wallabag.loadbalancer.server.port=80"
      - "traefik.docker.network=${TRAEFIK_NETWORK:-traefik_public}"
    networks: [default, traefik_public]

  flowhub.wallabag-bootstrap:
    build:
      context: demo/wallabag
    image: flowhub-wallabag-bootstrap:local
    environment:
      WALLABAG_API_URL: http://wallabag:80
      WALLABAG_DEMO_USER: ${WALLABAG_DEMO_USER:-flowhub}
      WALLABAG_DEMO_PASSWORD: ${WALLABAG_DEMO_PASSWORD:-flowhub-demo}
      WALLABAG_CLIENT_ID: ${WALLABAG_CLIENT_ID:?set in demo/.env}
      WALLABAG_CLIENT_SECRET: ${WALLABAG_CLIENT_SECRET:?set in demo/.env}
      BOOTSTRAP_OUT: /bootstrap/wallabag.env
    volumes:
      - vikunja-bootstrap:/bootstrap
    depends_on:
      - wallabag
    restart: "no"
```

Add to the bottom `volumes:` block:

```yaml
  paperless-data:
  paperless-media:
  wallabag-data:
```

> **Wallabag client provisioning** (resolve during this task): the default `wallabag/wallabag` image runs `wallabag:install` on first boot creating the default user. The API client must be created via `php bin/console wallabag:client:create <user> --env=prod`. Simplest demo approach: add a `command:` override or an extra one-shot that `docker exec`-style runs the console create and writes the id/secret to the shared volume *before* `wallabag-bootstrap` runs; OR pin known client creds by seeding the DB. Pick the approach that works against the pinned tag and document it inline. If non-interactive client creation proves heavy, fall back to a tiny init that runs `wallabag:client:create` and greps the id/secret into `/bootstrap/wallabag-client.env`, which `flowhub.wallabag-bootstrap` then sources.

- [ ] **Step 5: Validate compose config renders**

Run: `docker compose -f docker-compose.yml -f demo/docker-compose.yml config >/dev/null`
Expected: no error (env-var placeholders may warn; set dummies via a throwaway `.env` if needed).

- [ ] **Step 6: Commit**

```bash
git add demo/docker-compose.yml
git commit -m "feat(demo): run live wallabag + paperless-ngx services"
```

---

### Task 14: VPS Traefik overrides

**Files:**
- Modify: `demo/docker-compose.vps.yml`

- [ ] **Step 1: Append `!override` label blocks for `paperless` and `wallabag`**

Mirror the existing `vikunja` override block (entrypoint `web-secure`, certresolver `default`, network `web`):

```yaml
  paperless:
    labels: !override
      - "traefik.enable=true"
      - "traefik.http.routers.flowhub-paperless.rule=Host(`paperless.demo.flowhub.freaxnx01.ch`)"
      - "traefik.http.routers.flowhub-paperless.entrypoints=web-secure"
      - "traefik.http.routers.flowhub-paperless.tls=true"
      - "traefik.http.routers.flowhub-paperless.tls.certresolver=default"
      - "traefik.http.services.flowhub-paperless.loadbalancer.server.port=8000"
      - "traefik.docker.network=web"

  wallabag:
    labels: !override
      - "traefik.enable=true"
      - "traefik.http.routers.flowhub-wallabag.rule=Host(`wallabag.demo.flowhub.freaxnx01.ch`)"
      - "traefik.http.routers.flowhub-wallabag.entrypoints=web-secure"
      - "traefik.http.routers.flowhub-wallabag.tls=true"
      - "traefik.http.routers.flowhub-wallabag.tls.certresolver=default"
      - "traefik.http.services.flowhub-wallabag.loadbalancer.server.port=80"
      - "traefik.docker.network=web"
```

- [ ] **Step 2: Validate the three-file overlay renders**

Run: `docker compose -f docker-compose.yml -f demo/docker-compose.yml -f demo/docker-compose.vps.yml config >/dev/null`
Expected: no error.

- [ ] **Step 3: Commit**

```bash
git add demo/docker-compose.vps.yml
git commit -m "feat(demo): VPS Traefik routes for wallabag + paperless"
```

---

### Task 15: Extend the reset loop to clear all three services

**Files:**
- Modify: `demo/reset/reset.sh`
- Modify: `demo/docker-compose.yml` (`flowhub.demo-reset.environment` — pass the new env-file paths)

- [ ] **Step 1: Add a Wallabag-clear block to `reset.sh`**

After the existing Vikunja block, before the final "complete" echo:

```bash
# 5. Clear Wallabag entries (best-effort) — mints a fresh token via password grant.
WALLABAG_ENV="${WALLABAG_ENV_FILE:-/bootstrap/wallabag.env}"
if [ -f "${WALLABAG_ENV}" ]; then
  # shellcheck disable=SC1090
  . "${WALLABAG_ENV}"
  if [ -n "${WALLABAG_API_URL:-}" ] && [ -n "${WALLABAG_CLIENT_ID:-}" ]; then
    tok=$(curl -fsS -X POST "${WALLABAG_API_URL}/oauth/v2/token" \
      -d "grant_type=password&client_id=${WALLABAG_CLIENT_ID}&client_secret=${WALLABAG_CLIENT_SECRET}&username=${WALLABAG_USER}&password=${WALLABAG_PASSWORD}" \
      2>/dev/null | jq -r '.access_token // empty' || true)
    if [ -n "${tok}" ]; then
      ids=$(curl -fsS "${WALLABAG_API_URL}/api/entries.json?perPage=1000" -H "Authorization: Bearer ${tok}" 2>/dev/null \
        | jq -r '._embedded.items[].id' 2>/dev/null || true)
      count=0
      for id in ${ids}; do
        curl -fsS -X DELETE "${WALLABAG_API_URL}/api/entries/${id}.json" -H "Authorization: Bearer ${tok}" >/dev/null 2>&1 && count=$((count + 1)) || true
      done
      echo "[$(ts)] demo-reset: cleared ${count} Wallabag entr(ies)"
    else
      echo "[$(ts)] demo-reset: Wallabag token grant failed — skipping"
    fi
  fi
else
  echo "[$(ts)] demo-reset: no ${WALLABAG_ENV} — skipping Wallabag clear"
fi
```

- [ ] **Step 2: Add a Paperless-clear block to `reset.sh`**

```bash
# 6. Clear paperless-ngx documents (best-effort) via bulk_edit delete.
PAPERLESS_ENV="${PAPERLESS_ENV_FILE:-/bootstrap/paperless.env}"
if [ -f "${PAPERLESS_ENV}" ]; then
  # shellcheck disable=SC1090
  . "${PAPERLESS_ENV}"
  if [ -n "${PAPERLESS_API_URL:-}" ] && [ -n "${PAPERLESS_TOKEN:-}" ]; then
    auth="Authorization: Token ${PAPERLESS_TOKEN}"
    ids=$(curl -fsS "${PAPERLESS_API_URL}/api/documents/?page_size=1000" -H "${auth}" 2>/dev/null \
      | jq -c '[.results[].id]' 2>/dev/null || echo '[]')
    if [ "${ids}" != "[]" ] && [ -n "${ids}" ]; then
      curl -fsS -X POST "${PAPERLESS_API_URL}/api/documents/bulk_edit/" -H "${auth}" \
        -H 'Content-Type: application/json' \
        -d "{\"documents\":${ids},\"method\":\"delete\",\"parameters\":{}}" >/dev/null 2>&1 \
        && echo "[$(ts)] demo-reset: deleted paperless documents ${ids}" \
        || echo "[$(ts)] demo-reset: paperless bulk delete failed"
    else
      echo "[$(ts)] demo-reset: no paperless documents to clear"
    fi
  fi
else
  echo "[$(ts)] demo-reset: no ${PAPERLESS_ENV} — skipping Paperless clear"
fi
```

- [ ] **Step 3: Pass env-file paths to the reset container**

In `demo/docker-compose.yml`, `flowhub.demo-reset.environment`, add:

```yaml
      WALLABAG_ENV_FILE: /bootstrap/wallabag.env
      PAPERLESS_ENV_FILE: /bootstrap/paperless.env
```

(The reset container already mounts `vikunja-bootstrap:/bootstrap:ro`.)

- [ ] **Step 4: Lint the script**

Run: `bash -n demo/reset/reset.sh`
Expected: no output.

- [ ] **Step 5: Commit**

```bash
git add demo/reset/reset.sh demo/docker-compose.yml
git commit -m "feat(demo): clear wallabag + paperless on each reset cycle"
```

---

### Task 16: Update demo env example + banner

**Files:**
- Modify: `demo/.env.example`
- Modify: `demo/docker-compose.yml` (`Demo__BannerText`)

- [ ] **Step 1: Add new vars to `demo/.env.example`**

```dotenv
# paperless-ngx (document archive)
PAPERLESS_ADMIN_USER=flowhub
PAPERLESS_ADMIN_PASSWORD=flowhub-demo
PAPERLESS_PUBLIC_URL=https://paperless.demo.flowhub.freaxnx01.ch

# Wallabag (read-later). Client created via `wallabag:client:create` (see demo/wallabag).
WALLABAG_DEMO_USER=flowhub
WALLABAG_DEMO_PASSWORD=flowhub-demo
WALLABAG_CLIENT_ID=
WALLABAG_CLIENT_SECRET=
WALLABAG_PUBLIC_URL=https://wallabag.demo.flowhub.freaxnx01.ch
```

- [ ] **Step 2: Update the banner text**

In `demo/docker-compose.yml`, change `Demo__BannerText` to mention all three routes and the upload path, e.g.:

```yaml
      Demo__BannerText: "Public demo — quick-capture (top): 'todo: buy milk' routes to a live Vikunja board; a URL routes to Wallabag (read-later); upload a PDF/image at /captures/new to route it to paperless-ngx. A note like 'The Matrix is a great movie' shows AI classification. Data resets every 15 min; embeddings disabled."
```

- [ ] **Step 3: Validate compose still renders**

Run: `docker compose -f docker-compose.yml -f demo/docker-compose.yml config >/dev/null`
Expected: no error.

- [ ] **Step 4: Commit**

```bash
git add demo/.env.example demo/docker-compose.yml
git commit -m "docs(demo): document wallabag + paperless env + banner"
```

---

### Task 17: End-to-end smoke on the demo stack (manual verification)

> Run on a host with Docker. This is the integration gate the unit tests cannot cover.

- [ ] **Step 1: Bring up the demo stack**

```bash
cp demo/.env.example .env   # fill WALLABAG_CLIENT_* + Ai__OpenRouter__ApiKey
docker compose -f docker-compose.yml -f demo/docker-compose.yml up --build -d --wait
```
Expected: all services healthy; the three bootstrap sidecars exit 0; `/bootstrap/{vikunja,wallabag,paperless}.env` exist.

- [ ] **Step 2: Verify skill activation in web logs**

Run: `docker compose logs flowhub.web | grep -i "skill"`
Expected: boot log shows Vikunja, Wallabag, **and** Paperless `Registered: true`.

- [ ] **Step 3: Exercise each route**
- [ ] Submit `todo: buy milk` via the quick-capture box → appears as a Vikunja task.
- [ ] Submit a URL (e.g. `https://en.wikipedia.org/wiki/Hexagonal_architecture`) → appears in Wallabag.
- [ ] Upload a small PDF at `/captures/new` → appears as a paperless-ngx document (after OCR consume).

- [ ] **Step 4: Verify reset clears all three**

Run: `docker compose exec flowhub.demo-reset /reset.sh` (forces one cycle)
Expected: log lines confirm Captures truncated, Vikunja tasks cleared, Wallabag entries cleared, Paperless documents deleted.

- [ ] **Step 5: Tear down**

```bash
docker compose -f docker-compose.yml -f demo/docker-compose.yml down -v
```

- [ ] **Step 6: Final commit / push the branch (when ready)**

```bash
git push -u origin worktree-demo-vps
```

---

## Self-Review Notes (author)

- **Spec coverage:** Wallabag-live (Tasks 7-9, 12-14), paperless-live-minimal (Tasks 4-6, 11, 13-14), attachment routing rule (Tasks 1-3), reset-all-three (Task 15), banner/env (Task 16), token lifecycle decision (Tasks 7-9), Tika/Gotenberg omitted (Task 13 paperless service has neither). All spec sections map to tasks.
- **Type consistency:** `OpenReadAsync` (Task 1) consumed in Task 5; `HasAttachment` (Task 2) consumed in Task 3; `WallabagTokenProvider.GetTokenAsync` (Task 8) consumed in Task 9; `PaperlessOptions`/`PaperlessSkillIntegration` names consistent Tasks 4-6.
- **Known soft spot (flagged, not a placeholder):** Wallabag API-client (`client_id`/`secret`) creation against the pinned image is the one infra step needing on-host confirmation — Tasks 12/13 document the `wallabag:client:create` fallback explicitly rather than hand-waving it.
- **Out of scope (per spec):** skill-override dropdown wiring; office/email ingestion; document search.
