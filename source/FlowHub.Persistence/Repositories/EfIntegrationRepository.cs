using FlowHub.Core.Health;
using FlowHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowHub.Persistence.Repositories;

internal sealed class EfIntegrationRepository : IIntegrationRepository
{
    private readonly FlowHubDbContext _db;

    public EfIntegrationRepository(FlowHubDbContext db) => _db = db;

    public async Task<IReadOnlyList<IntegrationHealth>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _db.Integrations.AsNoTracking().ToListAsync(cancellationToken);
        return entities.Select(ToDomain).ToList();
    }

    public async Task<IntegrationHealth?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Integrations.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Name == name, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task UpsertAsync(IntegrationHealth integration, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Integrations
            .FirstOrDefaultAsync(i => i.Name == integration.Name, cancellationToken);
        if (entity is null)
        {
            _db.Integrations.Add(ToEntity(integration));
        }
        else
        {
            entity.Status = integration.Status.ToString();
            entity.LastWriteAt = integration.LastWriteAt;
            entity.LastWriteDurationMs = integration.LastWriteDuration.HasValue
                ? (long)integration.LastWriteDuration.Value.TotalMilliseconds
                : null;
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddHealthSampleAsync(
        string integrationName, HealthStatus status, TimeSpan? duration,
        CancellationToken cancellationToken = default)
    {
        _db.IntegrationHealthSamples.Add(new IntegrationHealthSampleEntity
        {
            Id = Guid.NewGuid(),
            IntegrationName = integrationName,
            SampledAt = DateTimeOffset.UtcNow,
            Status = status.ToString(),
            DurationMs = duration.HasValue ? (long)duration.Value.TotalMilliseconds : null,
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IntegrationHealthSample>> GetRecentSamplesAsync(
        string integrationName, int count, CancellationToken cancellationToken = default)
    {
        var entities = await _db.IntegrationHealthSamples.AsNoTracking()
            .Where(s => s.IntegrationName == integrationName)
            .OrderByDescending(s => s.SampledAt)
            .Take(count)
            .ToListAsync(cancellationToken);
        return entities.Select(ToSampleDomain).ToList();
    }

    private static IntegrationHealth ToDomain(IntegrationEntity e) => new(
        Name: e.Name,
        Status: Enum.Parse<HealthStatus>(e.Status),
        LastWriteAt: e.LastWriteAt,
        LastWriteDuration: e.LastWriteDurationMs.HasValue
            ? TimeSpan.FromMilliseconds(e.LastWriteDurationMs.Value)
            : null);

    private static IntegrationEntity ToEntity(IntegrationHealth h) => new()
    {
        Name = h.Name,
        Status = h.Status.ToString(),
        LastWriteAt = h.LastWriteAt,
        LastWriteDurationMs = h.LastWriteDuration.HasValue
            ? (long)h.LastWriteDuration.Value.TotalMilliseconds
            : null,
    };

    private static IntegrationHealthSample ToSampleDomain(IntegrationHealthSampleEntity e) => new(
        Id: e.Id,
        IntegrationName: e.IntegrationName,
        SampledAt: e.SampledAt,
        Status: Enum.Parse<HealthStatus>(e.Status),
        Duration: e.DurationMs.HasValue ? TimeSpan.FromMilliseconds(e.DurationMs.Value) : null);
}
