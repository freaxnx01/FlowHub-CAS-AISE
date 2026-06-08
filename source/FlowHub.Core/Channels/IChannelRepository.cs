namespace FlowHub.Core.Channels;

/// <summary>
/// Driven port for capture-source <see cref="Channel"/> records (Web, API, …).
/// EF Core implementation in <c>FlowHub.Persistence</c>.
/// </summary>
public interface IChannelRepository
{
    /// <summary>Returns all known channels.</summary>
    Task<IReadOnlyList<Channel>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the channel with the given name, or <c>null</c> if none exists.</summary>
    Task<Channel?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Inserts or updates the channel (keyed by name).</summary>
    Task UpsertAsync(Channel channel, CancellationToken cancellationToken = default);
}
