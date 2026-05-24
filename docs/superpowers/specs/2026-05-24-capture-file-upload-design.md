# Capture File Upload — Design Spec

**Date:** 2026-05-24
**Status:** Approved (brainstorming) — awaiting implementation plan
**Driver:** PVA 4 feedback (2026-05-23) — see `vault/Blöcke/04 Persitence - b) PVA.md`
**Future consumer:** paperless-ngx skill/integration (not part of this spec)

## Goal

Extend the `Capture` aggregate so that a single optional binary attachment can ride along with each Capture. Provide upload entry points in both the appbar `QuickCaptureField` and the `/captures/new` page. Cap upload size for the demo environment at 2 MB (configurable) and restrict to a small content-type allowlist (PDF, PNG, JPEG by default).

## Non-Goals

- The paperless-ngx skill itself. Attachments stay at `LifecycleStage.Captured`; no routing or classification of file content in this iteration.
- Multiple attachments per Capture (1:N). One file per Capture only.
- File previews, thumbnails, OCR, virus scanning, content sniffing beyond declared MIME.
- External object storage (S3/MinIO). Filesystem only for now.

## Domain Model

Add a value object in `FlowHub.Core/Captures/`:

```csharp
public sealed record Attachment(
    string FileName,
    string ContentType,
    long SizeBytes,
    string RelativePath,
    DateTimeOffset UploadedAt);
```

Extend the `Capture` record with an optional `Attachment? Attachment` property (1:0..1).

**Content rule:** when an upload accompanies a Capture, `Capture.Content` is set to `Path.GetFileName(attachment.FileName)`. Any typed text on the same submission is ignored. Rationale: keeps Captures non-empty, gives the dashboard something readable to display, and matches the paperless-ngx mental model ("the document *is* the capture").

## Driving Port

New port in `FlowHub.Core/Captures/`:

```csharp
public interface IAttachmentStorage
{
    Task<string> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
}
```

Returns the **relative** storage path (DB-portable).

`ICaptureService.SubmitAsync` gets an overload:

```csharp
Task<Capture> SubmitAsync(
    string? content,
    ChannelKind source,
    AttachmentInput? attachment,
    CancellationToken cancellationToken = default);
```

`AttachmentInput` is a transient DTO (in `FlowHub.Core/Captures/`) carrying `Stream`, `FileName`, `ContentType`, `SizeBytes` — never persisted.

## Configuration

New `UploadOptions` (in `FlowHub.Core/Captures/` — kept next to the port it constrains), bound under `FlowHub:Uploads`:

```jsonc
"FlowHub": {
  "Uploads": {
    "MaxBytes": 2097152,
    "AllowedContentTypes": [
      "application/pdf",
      "image/png",
      "image/jpeg"
    ],
    "StoragePath": "App_Data/uploads"
  }
}
```

- `MaxBytes` — 2 MB demo default. Override via `FlowHub__Uploads__MaxBytes` (12-Factor III).
- `AllowedContentTypes` — server-authoritative allowlist.
- `StoragePath` — relative to `IHostEnvironment.ContentRootPath`.

Bind with `AddOptions<UploadOptions>().ValidateDataAnnotations().ValidateOnStart()` so misconfiguration fails at startup. `[Required]` on `StoragePath`, sanity `[Range]` on `MaxBytes`.

An `IUploadPolicy` thin wrapper (over `IOptionsMonitor<UploadOptions>`) is injected into UI components so they get live values without taking a hard dependency on `IOptions<>`.

## Storage Adapter

`FilesystemAttachmentStorage` (in `FlowHub.Persistence`):

- Resolves absolute root from `IHostEnvironment.ContentRootPath` + `UploadOptions.StoragePath`.
- Layout: `App_Data/uploads/<yyyy>/<MM>/<guid><ext>` — month-sharded, collision-free via GUID, extension preserved for paperless-ngx hand-off.
- Returns the relative path (without the absolute prefix) for DB storage.

`EfCaptureService.SubmitAsync` orchestration when `attachment != null`:

1. Re-validate size + content type against `UploadOptions` (server-authoritative).
2. `IAttachmentStorage.SaveAsync(...)` → relative path.
3. Build `Attachment` VO.
4. Persist `Capture` (with owned `Attachment`) via EF.
5. If step 4 throws → best-effort `File.Delete(absolutePath)` to avoid orphan blobs.

## Persistence

`Attachment` mapped as an **EF Core owned entity** of `Capture` (`OwnsOne`). New nullable columns on `Captures`:

- `Attachment_FileName` (TEXT, NULL)
- `Attachment_ContentType` (TEXT, NULL)
- `Attachment_SizeBytes` (INTEGER, NULL)
- `Attachment_RelativePath` (TEXT, NULL)
- `Attachment_UploadedAt` (TEXT/DATETIMEOFFSET, NULL)

Configure `.Navigation(c => c.Attachment).IsRequired(false)` so EF treats the owned type as `null` when all columns are `NULL` (the C# property `FileName` is non-nullable).

New migration: `AddCaptureAttachment` (additive — safe on existing dev DBs).

`.gitignore`: add `source/FlowHub.Web/App_Data/uploads/` so demo blobs never get committed.

## UI

### QuickCaptureField (appbar)

- `Adornment.Start` paperclip icon (`Icons.Material.Filled.AttachFile`) wrapping a hidden `MudFileUpload<IBrowserFile>` with `MaxAllowedSize` and `Accept` bound to `IUploadPolicy`.
- States:
  - *idle* → unchanged
  - *file staged* → text field becomes read-only; shows `📎 invoice-042.pdf · 1.2 MB` plus a clear-X
  - *submitting* → existing hourglass behavior
- Submit posts via the new overload.

### NewCapture page (`/captures/new`)

- Existing text area stays.
- Below it: a `MudFileUpload` drop zone (`HideInputOnDrop`, single file, `Accept` from policy).
- When a file is chosen, the text area is disabled with helper text "File overrides text".

### Validation

- Client-side: `MaxAllowedSize` + `Accept` for UX only.
- Server-side: a `CaptureSubmissionValidator` (FluentValidation at the `FlowHub.Web` boundary) checks `SizeBytes ≤ MaxBytes` and `ContentType ∈ Allowed`. Failures surface via `ProblemDetails` (API) or `MudSnackbar` (UI).

## DI Wiring (`Program.cs`)

```csharp
builder.Services
    .AddOptions<UploadOptions>()
    .Bind(builder.Configuration.GetSection("FlowHub:Uploads"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IAttachmentStorage, FilesystemAttachmentStorage>();
builder.Services.AddSingleton<IUploadPolicy, UploadPolicy>();
```

## Testing

`tests/FlowHub.Web.ComponentTests` (bUnit):
- `QuickCaptureField_FileStaged_SubmitsAttachment`
- `QuickCaptureField_FileExceedsLimit_ShowsError`
- `QuickCaptureField_DisallowedContentType_ShowsError`
- `NewCapture_FileChosen_DisablesTextArea`

`tests/FlowHub.Persistence.Tests` (new project — placeholder slot already in repo layout):
- `EfCaptureService_SubmitWithAttachment_PersistsOwnedEntity` (SQLite in-memory)
- `EfCaptureService_SubmitWithAttachment_DbThrows_DeletesFile` (fake storage + forced DB error)
- `FilesystemAttachmentStorage_SaveAsync_WritesUnderConfiguredRoot` (temp dir; asserts `<yyyy>/<MM>/<guid><ext>` layout and relative return path)

`tests/FlowHub.Core.Tests`:
- `Capture_WithAttachment_RecordEquality` (sanity check on value-record equality with owned VO)

TDD per CLAUDE.md: failing test first, then implementation. Test naming `MethodName_StateUnderTest_ExpectedBehavior`.

## Documentation & Housekeeping

- `vault/Blöcke/04 Persitence - b) PVA.md` — strike-through the two TODO items and add commit ref once landed.
- `CHANGELOG.md` (Unreleased / Added): "File attachments on Capture (paperless-ngx prep) with 2 MB demo limit, configurable via `FlowHub:Uploads`."
- `docs/ai-usage.md` — append entry per CAS grading rubric (KI / Sub-Systeme / Reflexion bucket).

## Risks & Open Questions

- **Form size limits**: Blazor Server / Kestrel default request body limit is 30 MB; our policy caps at 2 MB but we should also set `KestrelServerOptions.Limits.MaxRequestBodySize` and `FormOptions.MultipartBodyLengthLimit` to match `MaxBytes` so the framework rejects oversized uploads early. (Implementation plan to confirm exact wiring.)
- **Orphan files on crash**: best-effort delete on DB failure covers normal errors; process kill mid-write can still leave orphan blobs. Acceptable for demo; a sweeper job is a later concern.
- **Path traversal**: `Path.GetFileName(attachment.FileName)` strips any directory components before persisting; storage path is GUID-based so the user-supplied name is never used on disk.
