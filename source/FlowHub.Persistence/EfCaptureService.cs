using FlowHub.Core.Captures;
using FlowHub.Core.Events;
using FlowHub.Persistence.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace FlowHub.Persistence;

public sealed class EfCaptureService : ICaptureService
{
    private readonly FlowHubDbContext _db;
    private readonly IPublishEndpoint _publishEndpoint;

    public EfCaptureService(FlowHubDbContext db, IPublishEndpoint publishEndpoint)
    {
        _db = db;
        _publishEndpoint = publishEndpoint;
    }

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

    public async Task<Capture> SubmitAsync(string content, ChannelKind source, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var entity = new CaptureEntity
        {
            Id = Guid.NewGuid(),
            Content = content,
            Source = source.ToString(),
            Stage = nameof(LifecycleStage.Raw),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _db.Captures.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        await _publishEndpoint.Publish(
            new CaptureCreated(entity.Id, entity.Content, source, entity.CreatedAt),
            cancellationToken);

        return ToDomain(entity);
    }

    public Task MarkClassifiedAsync(Guid id, string matchedSkill, string? title = null, CancellationToken cancellationToken = default) =>
        UpdateAsync(id, e =>
        {
            e.Stage = nameof(LifecycleStage.Classified);
            e.MatchedSkill = matchedSkill;
            if (title is not null) { e.Title = title; }
        }, cancellationToken);

    public Task MarkRoutedAsync(Guid id, CancellationToken cancellationToken = default) =>
        UpdateAsync(id, e => e.Stage = nameof(LifecycleStage.Routed), cancellationToken);

    public Task MarkCompletedAsync(Guid id, string? externalRef, CancellationToken cancellationToken = default) =>
        UpdateAsync(id, e =>
        {
            e.Stage = nameof(LifecycleStage.Completed);
            if (externalRef is not null) { e.ExternalRef = externalRef; }
        }, cancellationToken);

    public Task MarkOrphanAsync(Guid id, string reason, CancellationToken cancellationToken = default) =>
        UpdateAsync(id, e =>
        {
            e.Stage = nameof(LifecycleStage.Orphan);
            e.FailureReason = reason;
        }, cancellationToken);

    public Task MarkUnhandledAsync(Guid id, string reason, CancellationToken cancellationToken = default) =>
        UpdateAsync(id, e =>
        {
            e.Stage = nameof(LifecycleStage.Unhandled);
            e.FailureReason = reason;
        }, cancellationToken);

    public Task ResetForRetryAsync(Guid id, CancellationToken cancellationToken = default) =>
        UpdateAsync(id, e =>
        {
            e.Stage = nameof(LifecycleStage.Raw);
            e.FailureReason = null;
        }, cancellationToken);

    public async Task<CapturePage> ListAsync(CaptureFilter filter, CancellationToken cancellationToken = default)
    {
        IQueryable<CaptureEntity> query = _db.Captures.AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id);

        if (filter.Stages is { Count: > 0 } stages)
        {
            var stageStrings = stages.Select(s => s.ToString()).ToHashSet();
            query = query.Where(c => stageStrings.Contains(c.Stage));
        }

        if (filter.Source is ChannelKind src)
        {
            var sourceString = src.ToString();
            query = query.Where(c => c.Source == sourceString);
        }

        if (filter.Cursor is CaptureCursor cursor)
        {
            query = query.Where(c =>
                c.CreatedAt < cursor.CreatedAt
                || (c.CreatedAt == cursor.CreatedAt && c.Id.CompareTo(cursor.Id) < 0));
        }

        var limit = Math.Clamp(filter.Limit, 1, 200);
        var page = await query.Take(limit + 1).ToListAsync(cancellationToken);

        IReadOnlyList<Capture> items;
        CaptureCursor? next;

        if (page.Count > limit)
        {
            var last = page[limit - 1];
            next = new CaptureCursor(last.CreatedAt, last.Id);
            items = page.Take(limit).Select(ToDomain).ToList();
        }
        else
        {
            next = null;
            items = page.Select(ToDomain).ToList();
        }

        return new CapturePage(items, next);
    }

    private async Task UpdateAsync(Guid id, Action<CaptureEntity> mutate, CancellationToken cancellationToken)
    {
        var entity = await _db.Captures.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Capture {id} not found.");
        mutate(entity);
        await _db.SaveChangesAsync(cancellationToken);
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
        ExternalRef: e.ExternalRef);
}
