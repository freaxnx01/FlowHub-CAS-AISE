using FlowHub.Core.Captures;
using FlowHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowHub.Persistence.Repositories;

internal sealed class EfTagRepository : ITagRepository
{
    private readonly FlowHubDbContext _db;

    public EfTagRepository(FlowHubDbContext db) => _db = db;

    public async Task<IReadOnlyList<string>> GetByCaptureIdAsync(
        Guid captureId, CancellationToken cancellationToken = default)
    {
        return await _db.Tags.AsNoTracking()
            .Where(t => t.CaptureId == captureId)
            .Select(t => t.Value)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Guid captureId, string value, CancellationToken cancellationToken = default)
    {
        _db.Tags.Add(new TagEntity { CaptureId = captureId, Value = value });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid captureId, string value, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Tags
            .FirstOrDefaultAsync(t => t.CaptureId == captureId && t.Value == value, cancellationToken);
        if (entity is not null)
        {
            _db.Tags.Remove(entity);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
