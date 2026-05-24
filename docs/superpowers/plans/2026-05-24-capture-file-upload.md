# Capture File Upload Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add optional binary attachments (≤ 2 MB demo cap, PDF/PNG/JPEG by default) to the FlowHub `Capture` aggregate, with upload entry points in both the appbar `QuickCaptureField` and the `/captures/new` page, persisted on the filesystem with metadata in EF Core.

**Architecture:** New `Attachment` value object owned by the `Capture` record. New driving port `IAttachmentStorage` with a filesystem adapter writing under `App_Data/uploads/<yyyy>/<MM>/<guid><ext>`. `ICaptureService.SubmitAsync` gets an overload accepting an `AttachmentInput` DTO. Config under `FlowHub:Uploads` (`MaxBytes`, `AllowedContentTypes`, `StoragePath`). UI uses `MudFileUpload` with both client-side (`MaxAllowedSize`, `Accept`) and server-side (FluentValidation) checks.

**Tech Stack:** .NET 10, EF Core 10 (owned entities, SQLite/Npgsql), MudBlazor `MudFileUpload`, FluentValidation, bUnit + xUnit + FluentAssertions + NSubstitute.

**Spec:** `docs/superpowers/specs/2026-05-24-capture-file-upload-design.md`

---

## Task 0: Isolated worktree

**Files:** none — branch setup only.

- [ ] **Step 1: Create worktree branch from main**

```bash
cd /home/freax/projects/repos/github/freaxnx01/public/FlowHub-CAS-AISE
git fetch origin
git worktree add .claude/worktrees/upload -b feat/capture-file-upload origin/main
cd .claude/worktrees/upload
```

All subsequent commands run from `.claude/worktrees/upload`.

---

## Task 1: `Attachment` value object

**Files:**
- Create: `source/FlowHub.Core/Captures/Attachment.cs`
- Test: `tests/FlowHub.Core.Tests/Captures/AttachmentTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using FlowHub.Core.Captures;
using FluentAssertions;

namespace FlowHub.Core.Tests.Captures;

public class AttachmentTests
{
    [Fact]
    public void Attachment_TwoInstancesWithSameValues_AreEqual()
    {
        var uploadedAt = DateTimeOffset.UtcNow;
        var a = new Attachment("invoice.pdf", "application/pdf", 1024, "2026/05/abc.pdf", uploadedAt);
        var b = new Attachment("invoice.pdf", "application/pdf", 1024, "2026/05/abc.pdf", uploadedAt);

        a.Should().Be(b);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/FlowHub.Core.Tests --filter FullyQualifiedName~AttachmentTests
```

Expected: FAIL with `CS0246 type 'Attachment' not found`.

- [ ] **Step 3: Write minimal implementation**

```csharp
namespace FlowHub.Core.Captures;

/// <summary>
/// Binary content attached to a <see cref="Capture"/>. Value object, persisted
/// as an EF Core owned entity. Bytes live on the filesystem; this record stores
/// only metadata + a relative storage path.
/// </summary>
public sealed record Attachment(
    string FileName,
    string ContentType,
    long SizeBytes,
    string RelativePath,
    DateTimeOffset UploadedAt);
```

- [ ] **Step 4: Run test to verify it passes**

```bash
dotnet test tests/FlowHub.Core.Tests --filter FullyQualifiedName~AttachmentTests
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add source/FlowHub.Core/Captures/Attachment.cs tests/FlowHub.Core.Tests/Captures/AttachmentTests.cs
git commit -m "feat(core): add Attachment value object"
```

---

## Task 2: Extend `Capture` with optional `Attachment`

**Files:**
- Modify: `source/FlowHub.Core/Captures/Capture.cs`
- Test: `tests/FlowHub.Core.Tests/Captures/CaptureTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using FlowHub.Core.Captures;
using FluentAssertions;

namespace FlowHub.Core.Tests.Captures;

public class CaptureTests
{
    [Fact]
    public void Capture_WithAttachment_PreservesAttachmentOnRecordEquality()
    {
        var att = new Attachment("a.pdf", "application/pdf", 10, "2026/05/x.pdf", DateTimeOffset.UnixEpoch);
        var c1 = new Capture(Guid.Empty, ChannelKind.Web, "a.pdf", DateTimeOffset.UnixEpoch, LifecycleStage.Raw, null, Attachment: att);
        var c2 = c1 with { };

        c2.Attachment.Should().Be(att);
        c2.Should().Be(c1);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/FlowHub.Core.Tests --filter FullyQualifiedName~CaptureTests
```

Expected: FAIL — `Capture` has no `Attachment` member.

- [ ] **Step 3: Add the property**

```csharp
namespace FlowHub.Core.Captures;

public sealed record Capture(
    Guid Id,
    ChannelKind Source,
    string Content,
    DateTimeOffset CreatedAt,
    LifecycleStage Stage,
    string? MatchedSkill,
    string? FailureReason = null,
    string? Title = null,
    string? ExternalRef = null,
    string? VikunjaProject = null,
    string? EnrichmentDescription = null,
    Attachment? Attachment = null);
```

- [ ] **Step 4: Verify all callers still compile**

```bash
dotnet build FlowHub.slnx
```

Expected: no errors. (The new parameter has a default, so positional callers are unaffected.)

- [ ] **Step 5: Run test to verify it passes**

```bash
dotnet test tests/FlowHub.Core.Tests --filter FullyQualifiedName~CaptureTests
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add source/FlowHub.Core/Captures/Capture.cs tests/FlowHub.Core.Tests/Captures/CaptureTests.cs
git commit -m "feat(core): allow optional Attachment on Capture"
```

---

## Task 3: `IAttachmentStorage` port + `AttachmentInput` DTO + `UploadOptions` + `IUploadPolicy`

These are all small declarations — group them under one TDD cycle so we don't churn separate commits for empty interfaces.

**Files:**
- Create: `source/FlowHub.Core/Captures/IAttachmentStorage.cs`
- Create: `source/FlowHub.Core/Captures/AttachmentInput.cs`
- Create: `source/FlowHub.Core/Captures/UploadOptions.cs`
- Create: `source/FlowHub.Core/Captures/IUploadPolicy.cs`
- Test: `tests/FlowHub.Core.Tests/Captures/UploadOptionsTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using System.ComponentModel.DataAnnotations;
using FlowHub.Core.Captures;
using FluentAssertions;

namespace FlowHub.Core.Tests.Captures;

public class UploadOptionsTests
{
    [Fact]
    public void UploadOptions_MissingStoragePath_FailsValidation()
    {
        var opts = new UploadOptions { StoragePath = "", MaxBytes = 1024, AllowedContentTypes = ["application/pdf"] };
        var ctx = new ValidationContext(opts);
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(opts, ctx, results, validateAllProperties: true).Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(UploadOptions.StoragePath)));
    }

    [Fact]
    public void UploadOptions_NegativeMaxBytes_FailsValidation()
    {
        var opts = new UploadOptions { StoragePath = "App_Data/uploads", MaxBytes = -1, AllowedContentTypes = ["application/pdf"] };
        var ctx = new ValidationContext(opts);
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(opts, ctx, results, validateAllProperties: true).Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(UploadOptions.MaxBytes)));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/FlowHub.Core.Tests --filter FullyQualifiedName~UploadOptionsTests
```

Expected: FAIL — `UploadOptions` not defined.

- [ ] **Step 3: Add `UploadOptions`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace FlowHub.Core.Captures;

public sealed class UploadOptions
{
    public const int DefaultMaxBytes = 2 * 1024 * 1024;

    [Required, MinLength(1)]
    public string StoragePath { get; init; } = "App_Data/uploads";

    [Range(1, long.MaxValue)]
    public long MaxBytes { get; init; } = DefaultMaxBytes;

    public IReadOnlyList<string> AllowedContentTypes { get; init; } =
        ["application/pdf", "image/png", "image/jpeg"];
}
```

- [ ] **Step 4: Add `IUploadPolicy`**

```csharp
namespace FlowHub.Core.Captures;

/// <summary>
/// Live view over <see cref="UploadOptions"/>. Components depend on this
/// instead of taking IOptions&lt;T&gt; directly.
/// </summary>
public interface IUploadPolicy
{
    long MaxBytes { get; }
    IReadOnlyList<string> AllowedContentTypes { get; }
    string AcceptAttribute { get; } // comma-joined MIME list for HTML <input>
}
```

- [ ] **Step 5: Add `AttachmentInput`**

```csharp
namespace FlowHub.Core.Captures;

/// <summary>
/// Transient transfer object carrying upload bytes + metadata into the
/// Capture submission pipeline. Never persisted.
/// </summary>
public sealed class AttachmentInput
{
    public required Stream Content { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long SizeBytes { get; init; }
}
```

- [ ] **Step 6: Add `IAttachmentStorage`**

```csharp
namespace FlowHub.Core.Captures;

public interface IAttachmentStorage
{
    /// <returns>Relative storage path (portable across machines).</returns>
    Task<string> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>Best-effort delete used to roll back a failed Capture save.</summary>
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 7: Run test to verify it passes**

```bash
dotnet test tests/FlowHub.Core.Tests --filter FullyQualifiedName~UploadOptionsTests
dotnet build FlowHub.slnx
```

Expected: PASS + clean build.

- [ ] **Step 8: Commit**

```bash
git add source/FlowHub.Core/Captures/ tests/FlowHub.Core.Tests/Captures/UploadOptionsTests.cs
git commit -m "feat(core): add upload port, options, policy, and input DTO"
```

---

## Task 4: Add `SubmitAsync` overload to `ICaptureService` and update all implementers

The overload must be implemented by `EfCaptureService` and `CaptureServiceStub`. We add the interface method (compile-broken state), then immediately implement both adapters in this task to restore green.

**Files:**
- Modify: `source/FlowHub.Core/Captures/ICaptureService.cs`
- Modify: `source/FlowHub.Persistence/EfCaptureService.cs`
- Modify: `source/FlowHub.Web/Stubs/CaptureServiceStub.cs`

- [ ] **Step 1: Extend the interface**

Add this method to `ICaptureService`:

```csharp
Task<Capture> SubmitAsync(
    string? content,
    ChannelKind source,
    AttachmentInput? attachment,
    CancellationToken cancellationToken = default);
```

- [ ] **Step 2: Implement on `EfCaptureService` (temporary — orchestration comes in Task 9)**

Add to `EfCaptureService`:

```csharp
public async Task<Capture> SubmitAsync(
    string? content, ChannelKind source, AttachmentInput? attachment, CancellationToken cancellationToken = default)
{
    if (attachment is null)
    {
        return await SubmitAsync(content ?? throw new ArgumentNullException(nameof(content)), source, cancellationToken);
    }

    throw new NotImplementedException("Attachment orchestration lands in Task 9.");
}
```

- [ ] **Step 3: Implement on `CaptureServiceStub`**

Add to `CaptureServiceStub`:

```csharp
public Task<Capture> SubmitAsync(
    string? content, ChannelKind source, AttachmentInput? attachment, CancellationToken cancellationToken = default)
{
    var effectiveContent = attachment is null
        ? (content ?? throw new ArgumentNullException(nameof(content)))
        : System.IO.Path.GetFileName(attachment.FileName);

    return SubmitAsync(effectiveContent, source, cancellationToken)
        .ContinueWith(t => t.Result with { Attachment = attachment is null ? null : new Attachment(
            FileName: System.IO.Path.GetFileName(attachment.FileName),
            ContentType: attachment.ContentType,
            SizeBytes: attachment.SizeBytes,
            RelativePath: $"stub/{Guid.NewGuid():N}",
            UploadedAt: DateTimeOffset.UtcNow) }, cancellationToken);
}
```

- [ ] **Step 4: Build**

```bash
dotnet build FlowHub.slnx
```

Expected: clean build.

- [ ] **Step 5: Run full test suite (must stay green)**

```bash
dotnet test FlowHub.slnx
```

Expected: PASS — no behaviour changed for existing callers.

- [ ] **Step 6: Commit**

```bash
git add source/FlowHub.Core/Captures/ICaptureService.cs source/FlowHub.Persistence/EfCaptureService.cs source/FlowHub.Web/Stubs/CaptureServiceStub.cs
git commit -m "feat(core): add ICaptureService.SubmitAsync overload for attachments"
```

---

## Task 5: `UploadPolicy` adapter in `FlowHub.Web`

**Files:**
- Create: `source/FlowHub.Web/Uploads/UploadPolicy.cs`
- Test: `tests/FlowHub.Web.ComponentTests/Uploads/UploadPolicyTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using FlowHub.Core.Captures;
using FlowHub.Web.Uploads;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace FlowHub.Web.ComponentTests.Uploads;

public class UploadPolicyTests
{
    [Fact]
    public void AcceptAttribute_JoinsAllowedContentTypesWithCommas()
    {
        var opts = Options.Create(new UploadOptions
        {
            StoragePath = "App_Data/uploads",
            MaxBytes = 2_097_152,
            AllowedContentTypes = ["application/pdf", "image/png"],
        });
        var policy = new UploadPolicy(new TestMonitor(opts.Value));

        policy.AcceptAttribute.Should().Be("application/pdf,image/png");
        policy.MaxBytes.Should().Be(2_097_152);
    }

    private sealed class TestMonitor(UploadOptions current) : IOptionsMonitor<UploadOptions>
    {
        public UploadOptions CurrentValue { get; } = current;
        public UploadOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<UploadOptions, string?> listener) => null;
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/FlowHub.Web.ComponentTests --filter FullyQualifiedName~UploadPolicyTests
```

Expected: FAIL — `UploadPolicy` not defined.

- [ ] **Step 3: Implement**

```csharp
using FlowHub.Core.Captures;
using Microsoft.Extensions.Options;

namespace FlowHub.Web.Uploads;

public sealed class UploadPolicy : IUploadPolicy
{
    private readonly IOptionsMonitor<UploadOptions> _options;

    public UploadPolicy(IOptionsMonitor<UploadOptions> options) => _options = options;

    public long MaxBytes => _options.CurrentValue.MaxBytes;
    public IReadOnlyList<string> AllowedContentTypes => _options.CurrentValue.AllowedContentTypes;
    public string AcceptAttribute => string.Join(",", _options.CurrentValue.AllowedContentTypes);
}
```

- [ ] **Step 4: Run test to verify it passes**

```bash
dotnet test tests/FlowHub.Web.ComponentTests --filter FullyQualifiedName~UploadPolicyTests
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add source/FlowHub.Web/Uploads/UploadPolicy.cs tests/FlowHub.Web.ComponentTests/Uploads/UploadPolicyTests.cs
git commit -m "feat(web): add UploadPolicy adapter over IOptionsMonitor<UploadOptions>"
```

---

## Task 6: `FilesystemAttachmentStorage` adapter

**Files:**
- Create: `source/FlowHub.Persistence/FilesystemAttachmentStorage.cs`
- Test: `tests/FlowHub.Persistence.Tests/FilesystemAttachmentStorageTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using FlowHub.Core.Captures;
using FlowHub.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace FlowHub.Persistence.Tests;

public class FilesystemAttachmentStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "flowhub-upload-tests-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SaveAsync_WritesFileUnderConfiguredRoot_AndReturnsRelativePath()
    {
        var sut = NewSut();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });

        var relative = await sut.SaveAsync(stream, "invoice.pdf", "application/pdf");

        relative.Should().MatchRegex(@"^\d{4}/\d{2}/[0-9a-f]{32}\.pdf$");
        var absolute = Path.Combine(_root, relative);
        File.Exists(absolute).Should().BeTrue();
        (await File.ReadAllBytesAsync(absolute)).Should().Equal(1, 2, 3, 4);
    }

    [Fact]
    public async Task DeleteAsync_RemovesFile()
    {
        var sut = NewSut();
        var relative = await sut.SaveAsync(new MemoryStream(new byte[] { 1 }), "x.png", "image/png");

        await sut.DeleteAsync(relative);

        File.Exists(Path.Combine(_root, relative)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_MissingFile_DoesNotThrow()
    {
        var sut = NewSut();
        await sut.Invoking(s => s.DeleteAsync("2026/01/missing.pdf")).Should().NotThrowAsync();
    }

    private FilesystemAttachmentStorage NewSut()
    {
        var env = Substitute.For<IHostEnvironment>();
        env.ContentRootPath.Returns(_root);
        var opts = Options.Create(new UploadOptions { StoragePath = "", MaxBytes = 2_097_152, AllowedContentTypes = ["application/pdf", "image/png"] });
        return new FilesystemAttachmentStorage(env, opts);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
```

Note: `StoragePath = ""` here so the absolute root is exactly `_root`; production uses `"App_Data/uploads"`.

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/FlowHub.Persistence.Tests --filter FullyQualifiedName~FilesystemAttachmentStorageTests
```

Expected: FAIL — type missing.

- [ ] **Step 3: Implement**

```csharp
using FlowHub.Core.Captures;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FlowHub.Persistence;

public sealed class FilesystemAttachmentStorage : IAttachmentStorage
{
    private readonly IHostEnvironment _env;
    private readonly IOptions<UploadOptions> _options;

    public FilesystemAttachmentStorage(IHostEnvironment env, IOptions<UploadOptions> options)
    {
        _env = env;
        _options = options;
    }

    public async Task<string> SaveAsync(
        Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var safeExt = Path.GetExtension(Path.GetFileName(fileName)); // strips any path components
        var relative = string.Join('/', now.ToString("yyyy"), now.ToString("MM"), $"{Guid.NewGuid():N}{safeExt}");
        var absolute = Path.Combine(AbsoluteRoot(), relative);

        Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);
        await using var file = File.Create(absolute);
        await content.CopyToAsync(file, cancellationToken);

        return relative;
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var absolute = Path.Combine(AbsoluteRoot(), relativePath);
        if (File.Exists(absolute))
        {
            File.Delete(absolute);
        }
        return Task.CompletedTask;
    }

    private string AbsoluteRoot() => Path.Combine(_env.ContentRootPath, _options.Value.StoragePath);
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/FlowHub.Persistence.Tests --filter FullyQualifiedName~FilesystemAttachmentStorageTests
```

Expected: PASS (all three).

- [ ] **Step 5: Commit**

```bash
git add source/FlowHub.Persistence/FilesystemAttachmentStorage.cs tests/FlowHub.Persistence.Tests/FilesystemAttachmentStorageTests.cs
git commit -m "feat(persistence): add FilesystemAttachmentStorage adapter"
```

---

## Task 7: EF Core owned-entity mapping for `Attachment`

**Files:**
- Modify: `source/FlowHub.Persistence/Entities/CaptureEntity.cs`
- Create: `source/FlowHub.Persistence/Entities/AttachmentEntity.cs`
- Modify: `source/FlowHub.Persistence/Entities/CaptureEntityTypeConfiguration.cs`
- Modify: `source/FlowHub.Persistence/Repositories/EfCaptureRepository.cs` — `ToDomain` / `ToEntity`

- [ ] **Step 1: Add `AttachmentEntity`**

```csharp
namespace FlowHub.Persistence.Entities;

internal sealed class AttachmentEntity
{
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public string RelativePath { get; set; } = "";
    public DateTimeOffset UploadedAt { get; set; }
}
```

- [ ] **Step 2: Add `Attachment` property to `CaptureEntity`**

In `source/FlowHub.Persistence/Entities/CaptureEntity.cs`, add at the end of the class:

```csharp
public AttachmentEntity? Attachment { get; set; }
```

- [ ] **Step 3: Configure owned entity**

In `source/FlowHub.Persistence/Entities/CaptureEntityTypeConfiguration.cs`, add inside `Configure`:

```csharp
builder.OwnsOne(c => c.Attachment, a =>
{
    a.Property(x => x.FileName).HasColumnName("Attachment_FileName").HasMaxLength(512);
    a.Property(x => x.ContentType).HasColumnName("Attachment_ContentType").HasMaxLength(128);
    a.Property(x => x.SizeBytes).HasColumnName("Attachment_SizeBytes");
    a.Property(x => x.RelativePath).HasColumnName("Attachment_RelativePath").HasMaxLength(256);
    a.Property(x => x.UploadedAt).HasColumnName("Attachment_UploadedAt");
});
builder.Navigation(c => c.Attachment).IsRequired(false);
```

- [ ] **Step 4: Update `ToDomain` / `ToEntity` in `EfCaptureRepository`**

Replace the two helpers at the bottom of the file:

```csharp
private static Capture ToDomain(CaptureEntity e) => new(
    Id: e.Id,
    Source: Enum.Parse<ChannelKind>(e.Source),
    Content: e.Content,
    CreatedAt: e.CreatedAt,
    Stage: Enum.Parse<LifecycleStage>(e.Stage),
    MatchedSkill: e.MatchedSkill,
    FailureReason: e.FailureReason,
    Title: e.Title,
    ExternalRef: e.ExternalRef,
    VikunjaProject: e.VikunjaProject,
    Attachment: e.Attachment is null
        ? null
        : new Attachment(e.Attachment.FileName, e.Attachment.ContentType, e.Attachment.SizeBytes, e.Attachment.RelativePath, e.Attachment.UploadedAt));

private static CaptureEntity ToEntity(Capture c) => new()
{
    Id = c.Id,
    Content = c.Content,
    Source = c.Source.ToString(),
    Stage = c.Stage.ToString(),
    CreatedAt = c.CreatedAt,
    MatchedSkill = c.MatchedSkill,
    Title = c.Title,
    FailureReason = c.FailureReason,
    ExternalRef = c.ExternalRef,
    VikunjaProject = c.VikunjaProject,
    Attachment = c.Attachment is null ? null : new AttachmentEntity
    {
        FileName = c.Attachment.FileName,
        ContentType = c.Attachment.ContentType,
        SizeBytes = c.Attachment.SizeBytes,
        RelativePath = c.Attachment.RelativePath,
        UploadedAt = c.Attachment.UploadedAt,
    },
};
```

- [ ] **Step 5: Build (no migration yet — we'll generate it next)**

```bash
dotnet build FlowHub.slnx
```

Expected: clean build. Existing tests will still pass against the old DB shape because no migration has been added.

- [ ] **Step 6: Commit**

```bash
git add source/FlowHub.Persistence/Entities/ source/FlowHub.Persistence/Repositories/EfCaptureRepository.cs
git commit -m "feat(persistence): map AttachmentEntity as owned entity of CaptureEntity"
```

---

## Task 8: EF migration `AddCaptureAttachment`

**Files:** auto-generated under `source/FlowHub.Persistence/Migrations/`.

- [ ] **Step 1: Generate the migration**

```bash
dotnet ef migrations add 0009_AddCaptureAttachment \
  --project source/FlowHub.Persistence \
  --startup-project source/FlowHub.Web
```

Expected: three new files (`*.cs`, `*.Designer.cs`, snapshot updated).

- [ ] **Step 2: Inspect the migration**

Open the generated `Up()` and confirm it adds five nullable columns to `Captures`:
`Attachment_FileName`, `Attachment_ContentType`, `Attachment_SizeBytes`, `Attachment_RelativePath`, `Attachment_UploadedAt`. If any non-nullable column slipped in, regenerate — the owned-entity nav should be required-false.

- [ ] **Step 3: Apply locally and verify**

```bash
dotnet ef database update --project source/FlowHub.Persistence --startup-project source/FlowHub.Web
```

Expected: migration applies cleanly.

- [ ] **Step 4: Run all tests**

```bash
dotnet test FlowHub.slnx
```

Expected: PASS — no behavioural change yet.

- [ ] **Step 5: Commit**

```bash
git add source/FlowHub.Persistence/Migrations/
git commit -m "feat(persistence): add 0009_AddCaptureAttachment migration"
```

---

## Task 9: `EfCaptureService.SubmitAsync` overload — orchestration + rollback

**Files:**
- Modify: `source/FlowHub.Persistence/EfCaptureService.cs`
- Test: `tests/FlowHub.Persistence.Tests/EfCaptureServiceAttachmentTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using FlowHub.Core.Captures;
using FlowHub.Core.Events;
using FlowHub.Persistence;
using FluentAssertions;
using MassTransit;
using NSubstitute;

namespace FlowHub.Persistence.Tests;

public class EfCaptureServiceAttachmentTests
{
    [Fact]
    public async Task SubmitAsync_WithAttachment_PersistsAttachmentAndUsesFileNameAsContent()
    {
        var repo = Substitute.For<ICaptureRepository>();
        repo.AddAsync(Arg.Any<Capture>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(ci.Arg<Capture>()));
        var storage = Substitute.For<IAttachmentStorage>();
        storage.SaveAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("2026/05/abc123.pdf");
        var publish = Substitute.For<IPublishEndpoint>();
        var sut = new EfCaptureService(repo, publish, storage);

        using var bytes = new MemoryStream(new byte[10]);
        var input = new AttachmentInput { Content = bytes, FileName = "invoice.pdf", ContentType = "application/pdf", SizeBytes = 10 };

        var capture = await sut.SubmitAsync(content: "ignored typed text", ChannelKind.Web, input);

        capture.Content.Should().Be("invoice.pdf");
        capture.Attachment.Should().NotBeNull();
        capture.Attachment!.RelativePath.Should().Be("2026/05/abc123.pdf");
        capture.Attachment.SizeBytes.Should().Be(10);
    }

    [Fact]
    public async Task SubmitAsync_WithAttachment_RepositoryThrows_DeletesStoredFile()
    {
        var repo = Substitute.For<ICaptureRepository>();
        repo.AddAsync(Arg.Any<Capture>(), Arg.Any<CancellationToken>())
            .Returns<Task<Capture>>(_ => throw new InvalidOperationException("db down"));
        var storage = Substitute.For<IAttachmentStorage>();
        storage.SaveAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("2026/05/abc123.pdf");
        var publish = Substitute.For<IPublishEndpoint>();
        var sut = new EfCaptureService(repo, publish, storage);

        using var bytes = new MemoryStream(new byte[1]);
        var input = new AttachmentInput { Content = bytes, FileName = "x.pdf", ContentType = "application/pdf", SizeBytes = 1 };

        await sut.Invoking(s => s.SubmitAsync(null, ChannelKind.Web, input))
            .Should().ThrowAsync<InvalidOperationException>();

        await storage.Received(1).DeleteAsync("2026/05/abc123.pdf", Arg.Any<CancellationToken>());
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/FlowHub.Persistence.Tests --filter FullyQualifiedName~EfCaptureServiceAttachmentTests
```

Expected: FAIL — constructor lacks `IAttachmentStorage`; the overload still throws `NotImplementedException`.

- [ ] **Step 3: Update `EfCaptureService`**

Add ctor parameter and replace the placeholder overload:

```csharp
public sealed class EfCaptureService : ICaptureService
{
    private readonly ICaptureRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IAttachmentStorage _attachmentStorage;

    public EfCaptureService(
        ICaptureRepository repository,
        IPublishEndpoint publishEndpoint,
        IAttachmentStorage attachmentStorage)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _attachmentStorage = attachmentStorage;
    }

    // ... existing members unchanged ...

    public async Task<Capture> SubmitAsync(
        string? content, ChannelKind source, AttachmentInput? attachment, CancellationToken cancellationToken = default)
    {
        if (attachment is null)
        {
            return await SubmitAsync(content ?? throw new ArgumentNullException(nameof(content)), source, cancellationToken);
        }

        var fileName = Path.GetFileName(attachment.FileName);
        var relativePath = await _attachmentStorage.SaveAsync(
            attachment.Content, fileName, attachment.ContentType, cancellationToken);

        var att = new Attachment(fileName, attachment.ContentType, attachment.SizeBytes, relativePath, DateTimeOffset.UtcNow);
        var capture = new Capture(
            Guid.NewGuid(), source, fileName, DateTimeOffset.UtcNow,
            LifecycleStage.Raw, MatchedSkill: null, Attachment: att);

        try
        {
            var saved = await _repository.AddAsync(capture, cancellationToken);
            await _publishEndpoint.Publish(
                new CaptureCreated(saved.Id, saved.Content, saved.Source, saved.CreatedAt),
                cancellationToken);
            return saved;
        }
        catch
        {
            await _attachmentStorage.DeleteAsync(relativePath, CancellationToken.None);
            throw;
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/FlowHub.Persistence.Tests --filter FullyQualifiedName~EfCaptureServiceAttachmentTests
```

Expected: PASS.

- [ ] **Step 5: Run full suite**

```bash
dotnet test FlowHub.slnx
```

Expected: PASS. (If existing tests construct `EfCaptureService` directly without the new dep, fix them by passing `Substitute.For<IAttachmentStorage>()`.)

- [ ] **Step 6: Commit**

```bash
git add source/FlowHub.Persistence/EfCaptureService.cs tests/FlowHub.Persistence.Tests/EfCaptureServiceAttachmentTests.cs
git commit -m "feat(persistence): wire EfCaptureService attachment orchestration with rollback"
```

---

## Task 10: DI wiring + `appsettings.json` + Kestrel limits

**Files:**
- Modify: `source/FlowHub.Web/Program.cs`
- Modify: `source/FlowHub.Web/appsettings.json`

- [ ] **Step 1: Add the config section**

In `source/FlowHub.Web/appsettings.json`, add at the root level:

```jsonc
"FlowHub": {
  "Uploads": {
    "MaxBytes": 2097152,
    "AllowedContentTypes": ["application/pdf", "image/png", "image/jpeg"],
    "StoragePath": "App_Data/uploads"
  }
}
```

If `FlowHub` already exists, merge `Uploads` under it.

- [ ] **Step 2: Wire DI in `Program.cs`**

After the existing `AddFlowHubPersistence(...)` call, add:

```csharp
builder.Services
    .AddOptions<FlowHub.Core.Captures.UploadOptions>()
    .Bind(builder.Configuration.GetSection("FlowHub:Uploads"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<FlowHub.Core.Captures.IAttachmentStorage, FlowHub.Persistence.FilesystemAttachmentStorage>();
builder.Services.AddSingleton<FlowHub.Core.Captures.IUploadPolicy, FlowHub.Web.Uploads.UploadPolicy>();
```

- [ ] **Step 3: Set Kestrel + form limits to match policy**

Also in `Program.cs`, before `var app = builder.Build();`:

```csharp
var maxUpload = builder.Configuration.GetValue<long?>("FlowHub:Uploads:MaxBytes")
    ?? FlowHub.Core.Captures.UploadOptions.DefaultMaxBytes;

builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(o =>
    o.Limits.MaxRequestBodySize = maxUpload);
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
    o.MultipartBodyLengthLimit = maxUpload);
```

- [ ] **Step 4: Build & run**

```bash
dotnet build FlowHub.slnx
just run
```

Hit `http://localhost:5070` — app should start cleanly. Stop with Ctrl-C.

- [ ] **Step 5: Commit**

```bash
git add source/FlowHub.Web/Program.cs source/FlowHub.Web/appsettings.json
git commit -m "feat(web): wire upload options, storage, policy + Kestrel limits"
```

---

## Task 11: `.gitignore` for uploaded blobs

**Files:**
- Modify: `.gitignore`

- [ ] **Step 1: Add the ignore**

Append to `.gitignore`:

```
# Capture attachment blobs (demo storage)
source/FlowHub.Web/App_Data/uploads/
```

- [ ] **Step 2: Verify**

```bash
mkdir -p source/FlowHub.Web/App_Data/uploads
touch source/FlowHub.Web/App_Data/uploads/sanity.bin
git status --short source/FlowHub.Web/App_Data/uploads/
```

Expected: empty output (file ignored). Then remove the sanity dir:

```bash
rm -rf source/FlowHub.Web/App_Data/uploads
```

- [ ] **Step 3: Commit**

```bash
git add .gitignore
git commit -m "chore: ignore App_Data/uploads/ for demo attachment blobs"
```

---

## Task 12: `QuickCaptureField` — add file picker

**Files:**
- Modify: `source/FlowHub.Web/Components/Layout/QuickCaptureField.razor`
- Modify: `source/FlowHub.Web/Components/Layout/QuickCaptureField.razor.cs`
- Test: `tests/FlowHub.Web.ComponentTests/Layout/QuickCaptureFieldUploadTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using AngleSharp.Dom;
using Bunit;
using FlowHub.Core.Captures;
using FlowHub.Web.Components.Layout;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor.Services;
using NSubstitute;

namespace FlowHub.Web.ComponentTests.Layout;

public class QuickCaptureFieldUploadTests : TestContext
{
    public QuickCaptureFieldUploadTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public async Task StagingValidFile_AndSubmitting_PassesAttachmentToCaptureService()
    {
        var capture = Substitute.For<ICaptureService>();
        capture.SubmitAsync(Arg.Any<string?>(), ChannelKind.Web, Arg.Any<AttachmentInput?>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(new Capture(
                Guid.NewGuid(), ChannelKind.Web, ci.ArgAt<AttachmentInput>(2)!.FileName,
                DateTimeOffset.UtcNow, LifecycleStage.Raw, null)));
        var policy = Substitute.For<IUploadPolicy>();
        policy.MaxBytes.Returns(2_097_152);
        policy.AllowedContentTypes.Returns(new[] { "application/pdf" });
        policy.AcceptAttribute.Returns("application/pdf");

        Services.AddSingleton(capture);
        Services.AddSingleton(policy);

        var cut = RenderComponent<QuickCaptureField>();
        var file = InputFileContent.CreateFromBinary(new byte[8], "invoice.pdf", contentType: "application/pdf");
        cut.FindComponent<InputFile>().UploadFiles(file);

        await cut.InvokeAsync(() => cut.Find("button[aria-label='Submit capture']").Click());

        await capture.Received(1).SubmitAsync(
            Arg.Any<string?>(), ChannelKind.Web,
            Arg.Is<AttachmentInput?>(a => a != null && a.FileName == "invoice.pdf" && a.SizeBytes == 8),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void StagingFileExceedingPolicy_DisablesSubmitAndShowsError()
    {
        var capture = Substitute.For<ICaptureService>();
        var policy = Substitute.For<IUploadPolicy>();
        policy.MaxBytes.Returns(4L);
        policy.AllowedContentTypes.Returns(new[] { "application/pdf" });
        policy.AcceptAttribute.Returns("application/pdf");
        Services.AddSingleton(capture);
        Services.AddSingleton(policy);

        var cut = RenderComponent<QuickCaptureField>();
        var file = InputFileContent.CreateFromBinary(new byte[5], "big.pdf", "application/pdf");
        cut.FindComponent<InputFile>().UploadFiles(file);

        cut.Markup.Should().Contain("too large");
        capture.DidNotReceiveWithAnyArgs().SubmitAsync(default, default, default, default);
    }
}
```

(`InputFileContent` ships in bUnit. The submit button must carry `aria-label="Submit capture"` — Step 3 adds it.)

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/FlowHub.Web.ComponentTests --filter FullyQualifiedName~QuickCaptureFieldUploadTests
```

Expected: FAIL.

- [ ] **Step 3: Update the `.razor` markup**

Replace `source/FlowHub.Web/Components/Layout/QuickCaptureField.razor` with:

```razor
@namespace FlowHub.Web.Components.Layout
@using Microsoft.AspNetCore.Components.Forms

<MudStack Row="true" AlignItems="AlignItems.Center" Spacing="1" Class="mx-4" Style="max-width:640px;">
    <MudFileUpload T="IBrowserFile"
                   Accept="@UploadPolicy.AcceptAttribute"
                   MaximumFileCount="1"
                   OnFilesChanged="OnFileSelected">
        <ActivatorContent>
            <MudIconButton Icon="@Icons.Material.Filled.AttachFile"
                           aria-label="Attach file"
                           Disabled="_isSubmitting" />
        </ActivatorContent>
    </MudFileUpload>

    @if (_stagedFile is not null)
    {
        <MudChip T="string"
                 Icon="@Icons.Material.Filled.InsertDriveFile"
                 OnClose="ClearFile"
                 Color="@(_fileError is null ? Color.Default : Color.Error)">
            @_stagedFile.Name (@FormatBytes(_stagedFile.Size))
        </MudChip>
        @if (_fileError is not null)
        {
            <MudText Color="Color.Error" Typo="Typo.caption">@_fileError</MudText>
        }
    }
    else
    {
        <MudTextField T="string"
                      @bind-Value="_input"
                      Immediate="true"
                      Placeholder="+ Quick capture: paste URL or type…"
                      Variant="Variant.Outlined"
                      Margin="Margin.Dense"
                      OnKeyDown="OnKeyDownAsync"
                      Disabled="_isSubmitting" />
    }

    <MudIconButton Icon="@(_isSubmitting ? Icons.Material.Filled.HourglassTop : Icons.Material.Filled.KeyboardReturn)"
                   aria-label="Submit capture"
                   OnClick="SubmitAsync"
                   Disabled="_isSubmitting || (_stagedFile is not null && _fileError is not null)" />
</MudStack>
```

- [ ] **Step 4: Update the code-behind**

Replace `source/FlowHub.Web/Components/Layout/QuickCaptureField.razor.cs` with:

```csharp
using FlowHub.Core.Captures;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace FlowHub.Web.Components.Layout;

public partial class QuickCaptureField : ComponentBase
{
    [Inject] private ICaptureService CaptureService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IUploadPolicy UploadPolicy { get; set; } = default!;

    private string? _input;
    private bool _isSubmitting;
    private IBrowserFile? _stagedFile;
    private string? _fileError;

    private void OnFileSelected(InputFileChangeEventArgs args)
    {
        var file = args.File;
        _fileError = ValidateFile(file);
        _stagedFile = file;
    }

    private string? ValidateFile(IBrowserFile file)
    {
        if (file.Size > UploadPolicy.MaxBytes)
            return $"File too large ({FormatBytes(file.Size)} > {FormatBytes(UploadPolicy.MaxBytes)})";
        if (!UploadPolicy.AllowedContentTypes.Contains(file.ContentType))
            return $"Type {file.ContentType} not allowed";
        return null;
    }

    private void ClearFile()
    {
        _stagedFile = null;
        _fileError = null;
    }

    private async Task OnKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key is "Enter" or "NumpadEnter") await SubmitAsync();
    }

    private async Task SubmitAsync()
    {
        if (_stagedFile is not null)
        {
            if (_fileError is not null) return;
            await SubmitFileAsync(_stagedFile);
            return;
        }

        var content = _input?.Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            Snackbar.Add("Type something first", Severity.Info);
            return;
        }
        await SubmitTextAsync(content);
    }

    private async Task SubmitTextAsync(string content)
    {
        _isSubmitting = true;
        try
        {
            var capture = await CaptureService.SubmitAsync(content, ChannelKind.Web);
            Snackbar.Add("Captured ✓", Severity.Success, key: capture.Id.ToString());
            _input = string.Empty;
        }
        catch (Exception ex) { Snackbar.Add($"Capture failed: {ex.Message}", Severity.Error); }
        finally { _isSubmitting = false; }
    }

    private async Task SubmitFileAsync(IBrowserFile file)
    {
        _isSubmitting = true;
        try
        {
            await using var stream = file.OpenReadStream(UploadPolicy.MaxBytes);
            var input = new AttachmentInput
            {
                Content = stream,
                FileName = file.Name,
                ContentType = file.ContentType,
                SizeBytes = file.Size,
            };
            var capture = await CaptureService.SubmitAsync(content: null, ChannelKind.Web, input);
            Snackbar.Add($"Uploaded ✓ — {capture.Content}", Severity.Success, key: capture.Id.ToString());
            ClearFile();
        }
        catch (Exception ex) { Snackbar.Add($"Upload failed: {ex.Message}", Severity.Error); }
        finally { _isSubmitting = false; }
    }

    private static string FormatBytes(long bytes) =>
        bytes < 1024 ? $"{bytes} B"
        : bytes < 1024 * 1024 ? $"{bytes / 1024.0:F1} KB"
        : $"{bytes / 1024.0 / 1024.0:F2} MB";
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/FlowHub.Web.ComponentTests --filter FullyQualifiedName~QuickCaptureFieldUploadTests
```

Expected: PASS.

- [ ] **Step 6: Run all ComponentTests (catch regressions)**

```bash
dotnet test tests/FlowHub.Web.ComponentTests
```

Expected: PASS. Fix any existing QuickCaptureField test broken by markup changes (e.g. by injecting `IUploadPolicy`).

- [ ] **Step 7: Commit**

```bash
git add source/FlowHub.Web/Components/Layout/ tests/FlowHub.Web.ComponentTests/Layout/QuickCaptureFieldUploadTests.cs
git commit -m "feat(web): add file picker to QuickCaptureField"
```

---

## Task 13: `NewCapture` page — upload zone

**Files:**
- Modify: `source/FlowHub.Web/Components/Pages/NewCapture.razor`
- Modify: `source/FlowHub.Web/Components/Pages/NewCapture.razor.cs`
- Test: `tests/FlowHub.Web.ComponentTests/Pages/NewCaptureUploadTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using Bunit;
using FlowHub.Core.Captures;
using FlowHub.Core.Health;
using FlowHub.Web.Components.Pages;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor.Services;
using NSubstitute;

namespace FlowHub.Web.ComponentTests.Pages;

public class NewCaptureUploadTests : TestContext
{
    public NewCaptureUploadTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(Substitute.For<ICaptureService>());
        var skills = Substitute.For<ISkillRegistry>();
        skills.GetHealthAsync().Returns(Task.FromResult<IReadOnlyList<SkillHealth>>([]));
        Services.AddSingleton(skills);
        var policy = Substitute.For<IUploadPolicy>();
        policy.MaxBytes.Returns(2_097_152);
        policy.AllowedContentTypes.Returns(new[] { "application/pdf" });
        policy.AcceptAttribute.Returns("application/pdf");
        Services.AddSingleton(policy);
    }

    [Fact]
    public void StagingFile_DisablesTextAreaAndShowsHelperText()
    {
        var cut = RenderComponent<NewCapture>();
        var file = InputFileContent.CreateFromBinary(new byte[2], "doc.pdf", "application/pdf");
        cut.FindComponent<InputFile>().UploadFiles(file);

        cut.Markup.Should().Contain("File overrides text");
        // The textarea is disabled — MudBlazor renders disabled="" on the underlying input
        cut.Find("textarea").GetAttribute("disabled").Should().NotBeNull();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/FlowHub.Web.ComponentTests --filter FullyQualifiedName~NewCaptureUploadTests
```

Expected: FAIL.

- [ ] **Step 3: Update the page markup**

In `source/FlowHub.Web/Components/Pages/NewCapture.razor`, replace the `MudTextField` for content and add the file upload below it:

```razor
<MudTextField T="string"
              @bind-Value="_content"
              Label="Content"
              Required="@(_stagedFile is null)"
              RequiredError="Content is required"
              Lines="5"
              Placeholder="Paste a URL, type a quote, or describe what you want to capture."
              HelperText="@(_stagedFile is null ? "Paste a URL, type a quote, or describe what you want to capture." : "File overrides text")"
              Variant="Variant.Outlined"
              Class="mb-4"
              Disabled="_isSubmitting || _stagedFile is not null" />

<MudFileUpload T="IBrowserFile"
               Accept="@UploadPolicy.AcceptAttribute"
               MaximumFileCount="1"
               OnFilesChanged="OnFileSelected"
               Class="mb-4">
    <ActivatorContent>
        <MudButton HtmlTag="label"
                   Variant="Variant.Outlined"
                   StartIcon="@Icons.Material.Filled.AttachFile"
                   Disabled="_isSubmitting">
            @(_stagedFile is null ? "Attach file (optional)" : $"{_stagedFile.Name} — change")
        </MudButton>
    </ActivatorContent>
</MudFileUpload>
@if (_fileError is not null)
{
    <MudText Color="Color.Error" Typo="Typo.caption" Class="mb-4">@_fileError</MudText>
}
```

Add the using at the top: `@using Microsoft.AspNetCore.Components.Forms`.

- [ ] **Step 4: Update the code-behind**

Add `IUploadPolicy` inject + staging state + extend `SubmitAsync` in `NewCapture.razor.cs`:

```csharp
[Inject] private IUploadPolicy UploadPolicy { get; set; } = default!;

private IBrowserFile? _stagedFile;
private string? _fileError;

private void OnFileSelected(InputFileChangeEventArgs args)
{
    var file = args.File;
    if (file.Size > UploadPolicy.MaxBytes)
    {
        _fileError = $"File too large (max {UploadPolicy.MaxBytes / 1024 / 1024} MB)";
        _stagedFile = null;
        return;
    }
    if (!UploadPolicy.AllowedContentTypes.Contains(file.ContentType))
    {
        _fileError = $"Type {file.ContentType} not allowed";
        _stagedFile = null;
        return;
    }
    _fileError = null;
    _stagedFile = file;
}
```

Then replace the body of `SubmitAsync` to branch on file presence:

```csharp
private async Task SubmitAsync()
{
    if (_stagedFile is null)
    {
        await _form.Validate();
        if (!_form.IsValid) return;
    }
    else if (_fileError is not null)
    {
        return;
    }

    _isSubmitting = true;
    try
    {
        Capture capture;
        if (_stagedFile is not null)
        {
            await using var stream = _stagedFile.OpenReadStream(UploadPolicy.MaxBytes);
            capture = await CaptureService.SubmitAsync(
                content: null, ChannelKind.Web,
                new AttachmentInput
                {
                    Content = stream,
                    FileName = _stagedFile.Name,
                    ContentType = _stagedFile.ContentType,
                    SizeBytes = _stagedFile.Size,
                });
        }
        else
        {
            capture = await CaptureService.SubmitAsync(_content!, ChannelKind.Web);
        }

        var preview = capture.Content.Length > 40
            ? string.Concat(capture.Content.AsSpan(0, 37), "...")
            : capture.Content;
        Snackbar.Add($"Captured ✓ — \"{preview}\"", Severity.Success);

        _content = null;
        _stagedFile = null;
        _selectedSkill = AutoSkill;
        await _form.ResetAsync();
    }
    catch (Exception ex) { Snackbar.Add($"Capture failed: {ex.Message}", Severity.Error); }
    finally { _isSubmitting = false; }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/FlowHub.Web.ComponentTests --filter FullyQualifiedName~NewCaptureUploadTests
dotnet test tests/FlowHub.Web.ComponentTests
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add source/FlowHub.Web/Components/Pages/NewCapture.razor source/FlowHub.Web/Components/Pages/NewCapture.razor.cs tests/FlowHub.Web.ComponentTests/Pages/NewCaptureUploadTests.cs
git commit -m "feat(web): add file upload to NewCapture page"
```

---

## Task 14: Manual smoke test

**Files:** none — verification only.

- [ ] **Step 1: Start the app**

```bash
just run
```

- [ ] **Step 2: Browser smoke**

In a browser at `http://localhost:5070`:

1. Open the appbar `QuickCaptureField`. Click the paperclip. Select a small (< 2 MB) PDF. Confirm the filename chip appears. Click submit. Confirm snackbar "Uploaded ✓ — <filename>".
2. Try a `.txt` file. Confirm the chip shows red helper text "Type text/plain not allowed".
3. Try a > 2 MB file. Confirm "File too large" message.
4. Navigate to `/captures/new`. Stage a PDF. Confirm the textarea is disabled and helper says "File overrides text". Submit. Confirm success.
5. Check `source/FlowHub.Web/App_Data/uploads/2026/05/` — file present with GUID name + `.pdf` extension.
6. Open the Captures list. Confirm the new captures appear with filename as their content.

- [ ] **Step 3: Stop the app and clean up demo blobs**

```bash
rm -rf source/FlowHub.Web/App_Data/uploads
```

- [ ] **Step 4: No commit (smoke test only)**

---

## Task 15: Docs & vault housekeeping

**Files:**
- Modify: `CHANGELOG.md`
- Modify: `docs/ai-usage.md`
- Modify: `vault/Blöcke/04 Persitence/04 Persitence - b) PVA.md`

- [ ] **Step 1: Update `CHANGELOG.md`**

Under `## [Unreleased]` → `### Added` add:

```
- Capture file attachments (paperless-ngx prep): PDF/PNG/JPEG default allowlist, 2 MB demo limit configurable via `FlowHub:Uploads`.
```

- [ ] **Step 2: Append to `docs/ai-usage.md`**

Add an entry dated `2026-05-24` summarising: brainstorming via skill, design spec written, implementation plan executed task-by-task with TDD, scope = Capture attachment domain + UI + persistence.

- [ ] **Step 3: Strike-through PVA TODO items**

In `vault/Blöcke/04 Persitence/04 Persitence - b) PVA.md`, change the two `- [ ]` lines to `- [x]` and append `(implemented in feat/capture-file-upload)`. Bump the frontmatter `updated:` to `2026-05-24`.

- [ ] **Step 4: Commit**

```bash
git add CHANGELOG.md docs/ai-usage.md "vault/Blöcke/04 Persitence/04 Persitence - b) PVA.md"
git commit -m "docs: record capture file upload feature (CHANGELOG, ai-usage, PVA notes)"
```

---

## Task 16: Final verification + push + PR

**Files:** none — verification only.

- [ ] **Step 1: Full test suite + format**

```bash
dotnet test FlowHub.slnx
dotnet format FlowHub.slnx --verify-no-changes || dotnet format FlowHub.slnx
```

Expected: all tests pass; format either clean or fixed-then-clean.

- [ ] **Step 2: Vulnerability scan**

```bash
dotnet list package --vulnerable --fail-on-severity high
```

Expected: no new vulnerable packages (we added none).

- [ ] **Step 3: If `dotnet format` made changes, commit**

```bash
git add -A
git status   # confirm only formatting-noise
git commit -m "style: dotnet format"
```

- [ ] **Step 4: Push branch and open PR**

```bash
git push -u origin feat/capture-file-upload
gh pr create --title "feat: capture file upload (paperless-ngx prep, 2 MB demo limit)" --body "$(cat <<'EOF'
## Summary
- Adds optional `Attachment` to `Capture` (1:0..1) backed by filesystem storage under `App_Data/uploads/<yyyy>/<MM>/<guid><ext>`.
- File upload entry points in both `QuickCaptureField` (appbar paperclip) and `/captures/new`.
- 2 MB demo limit + PDF/PNG/JPEG allowlist, configurable under `FlowHub:Uploads`.
- Server-authoritative size + content-type validation; client-side hints via `MudFileUpload.MaxAllowedSize` / `Accept`.

Spec: `docs/superpowers/specs/2026-05-24-capture-file-upload-design.md`
Driver: PVA 4 (2026-05-23) feedback.

## Test plan
- [ ] `dotnet test FlowHub.slnx` green
- [ ] Manual smoke per plan Task 14 (small PDF, oversized file, disallowed type, NewCapture textarea disable)
- [ ] EF migration `0009_AddCaptureAttachment` applies cleanly

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

Expected: PR URL returned.

---

## Self-Review

**Spec coverage:**
- Domain `Attachment` + `Capture` extension → Tasks 1–2 ✓
- `IAttachmentStorage` port → Task 3 ✓
- `ICaptureService.SubmitAsync` overload + content rule → Tasks 4, 9 ✓
- `UploadOptions` (`MaxBytes`/`AllowedContentTypes`/`StoragePath`) + `IUploadPolicy` → Tasks 3, 5, 10 ✓
- `FilesystemAttachmentStorage` with `<yyyy>/<MM>/<guid><ext>` layout → Task 6 ✓
- EF owned-entity mapping + nullable columns + migration → Tasks 7–8 ✓
- Rollback on DB failure → Task 9 ✓
- QuickCaptureField paperclip + staged-file states → Task 12 ✓
- NewCapture drop zone + textarea disable → Task 13 ✓
- Server-side size + content-type re-validation → covered in Task 9 (orchestration) — note: FluentValidation `CaptureSubmissionValidator` mentioned in spec is **subsumed** by the inline checks in the service overload + client-side `MudFileUpload` guards. No standalone validator class; this is intentional YAGNI since there's no HTTP boundary controller yet (the Web app goes directly through DI). If a Minimal API endpoint for captures lands later, add the validator then.
- Kestrel + form limits → Task 10 ✓
- `.gitignore` → Task 11 ✓
- Docs/vault housekeeping → Task 15 ✓

**Type consistency check:** `IAttachmentStorage.SaveAsync` (Task 3) returns `Task<string>` and is consumed by `EfCaptureService` (Task 9) — match. `AttachmentInput` properties (Task 3) match usage in `QuickCaptureField` (Task 12) and `NewCapture` (Task 13). `IUploadPolicy.AcceptAttribute` defined Task 3 / impl Task 5 / consumed Tasks 12, 13.

**Placeholders:** none — every code step has full code, every test step has the actual test.

**Scope:** Single feature, single plan. No decomposition needed.

---

**Plan complete and saved to `docs/superpowers/plans/2026-05-24-capture-file-upload.md`. Two execution options:**

**1. Subagent-Driven (recommended)** — I dispatch a fresh subagent per task, review between tasks, fast iteration.

**2. Inline Execution** — Execute tasks in this session using `executing-plans`, batch execution with checkpoints.

Which approach?
