using FlowHub.Core.Captures;
using FlowHub.Core.Channels;
using FlowHub.Core.Health;
using FlowHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowHub.Persistence.Repositories;

internal sealed class EfChannelRepository : IChannelRepository
{
    private readonly FlowHubDbContext _db;

    public EfChannelRepository(FlowHubDbContext db) => _db = db;

    public async Task<IReadOnlyList<Channel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _db.Channels.AsNoTracking().ToListAsync(cancellationToken);
        return entities.Select(ToDomain).ToList();
    }

    public async Task<Channel?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Channels.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task UpsertAsync(Channel channel, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Channels.FirstOrDefaultAsync(c => c.Name == channel.Name, cancellationToken);
        if (entity is null)
        {
            _db.Channels.Add(ToEntity(channel));
        }
        else
        {
            entity.Kind = channel.Kind.ToString();
            entity.IsEnabled = channel.IsEnabled;
            entity.Status = channel.Status.ToString();
            entity.LastActiveAt = channel.LastActiveAt;
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static Channel ToDomain(ChannelEntity e) => new(
        Name: e.Name,
        Kind: Enum.Parse<ChannelKind>(e.Kind),
        IsEnabled: e.IsEnabled,
        Status: Enum.Parse<HealthStatus>(e.Status),
        LastActiveAt: e.LastActiveAt);

    private static ChannelEntity ToEntity(Channel c) => new()
    {
        Name = c.Name,
        Kind = c.Kind.ToString(),
        IsEnabled = c.IsEnabled,
        Status = c.Status.ToString(),
        LastActiveAt = c.LastActiveAt,
    };
}
