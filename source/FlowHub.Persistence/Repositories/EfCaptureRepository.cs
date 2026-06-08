using FlowHub.Core.Captures;
using FlowHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using System.Globalization;

namespace FlowHub.Persistence.Repositories;

internal sealed class EfCaptureRepository : ICaptureRepository
{
    private readonly FlowHubDbContext _db;

    public EfCaptureRepository(FlowHubDbContext db) => _db = db;

    public async Task<Capture?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Captures.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyList<Capture>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _db.Captures.AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
        return entities.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Capture>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        var entities = await _db.Captures.AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
        return entities.Select(ToDomain).ToList();
    }

    public async Task<FailureCounts> GetFailureCountsAsync(CancellationToken cancellationToken = default)
    {
        var orphan = await _db.Captures.AsNoTracking()
            .CountAsync(c => c.Stage == nameof(LifecycleStage.Orphan), cancellationToken);
        var unhandled = await _db.Captures.AsNoTracking()
            .CountAsync(c => c.Stage == nameof(LifecycleStage.Unhandled), cancellationToken);
        return new FailureCounts(orphan, unhandled);
    }

    public async Task<Capture> AddAsync(Capture capture, CancellationToken cancellationToken = default)
    {
        _db.Captures.Add(ToEntity(capture));
        await _db.SaveChangesAsync(cancellationToken);
        return capture;
    }

    public async Task UpdateAsync(Capture capture, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Captures.FirstOrDefaultAsync(c => c.Id == capture.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Capture {capture.Id} not found.");
        entity.Stage = capture.Stage.ToString();
        entity.MatchedSkill = capture.MatchedSkill;
        entity.Title = capture.Title;
        entity.FailureReason = capture.FailureReason;
        entity.ExternalRef = capture.ExternalRef;
        entity.VikunjaProject = capture.VikunjaProject;
        entity.EnrichmentDescription = capture.EnrichmentDescription;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CapturePage> ListAsync(CaptureFilter filter, CancellationToken cancellationToken = default)
    {
        IQueryable<CaptureEntity> query = _db.Captures
            .Include(c => c.Tags)
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id);

        query = CaptureQueryBuilder.Apply(query, filter);

        var limit = Math.Clamp(filter.Limit, 1, 200);
        var fetched = await query.Take(limit + 1).ToListAsync(cancellationToken);

        if (fetched.Count > limit)
        {
            var last = fetched[limit - 1];
            return new CapturePage(
                fetched.Take(limit).Select(ToDomain).ToList(),
                new CaptureCursor(last.CreatedAt, last.Id));
        }

        return new CapturePage(fetched.Select(ToDomain).ToList(), null);
    }

    public async Task StoreEmbeddingAsync(
        Guid captureId, float[] embedding, CancellationToken cancellationToken = default)
    {
        // ExecuteUpdateAsync issues a single UPDATE without loading or tracking the entity —
        // keeps admin rebuild loops O(N) instead of O(N²) at NfA-04 scale (100k captures).
        var vector = new Vector(embedding);
        var rows = await _db.Captures
            .Where(c => c.Id == captureId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Embedding, vector), cancellationToken);
        if (rows == 0)
            throw new KeyNotFoundException($"Capture {captureId} not found.");
    }

    public async Task<IReadOnlyList<Capture>> SearchByEmbeddingAsync(
        float[] queryEmbedding, int limit, CancellationToken cancellationToken = default)
    {
        // Mirror the ListAsync 1..200 contract; without this, ?limit=-1 yields 500
        // and ?limit=2000000000 forces an unbounded ANN scan (DoS vector).
        var safeLimit = Math.Clamp(limit, 1, 200);

        // float[] values are IEEE 754 floats — no SQL injection risk in the literal.
        var vectorLiteral = "[" + string.Join(",",
            queryEmbedding.Select(f => f.ToString("G", CultureInfo.InvariantCulture))) + "]";

        var entities = await _db.Captures
            .FromSqlInterpolated($"""
                SELECT * FROM "Captures"
                WHERE "Embedding" IS NOT NULL
                ORDER BY "Embedding" <=> {vectorLiteral}::vector
                LIMIT {safeLimit}
                """)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Guid>> GetIdsWithoutEmbeddingAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Captures
            .AsNoTracking()
            .Where(c => c.Embedding == null)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
    }

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
        EnrichmentDescription: e.EnrichmentDescription,
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
        EnrichmentDescription = c.EnrichmentDescription,
        Attachment = c.Attachment is null ? null : new AttachmentEntity
        {
            FileName = c.Attachment.FileName,
            ContentType = c.Attachment.ContentType,
            SizeBytes = c.Attachment.SizeBytes,
            RelativePath = c.Attachment.RelativePath,
            UploadedAt = c.Attachment.UploadedAt,
        },
    };
}
