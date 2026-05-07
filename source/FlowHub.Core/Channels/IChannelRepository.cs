namespace FlowHub.Core.Channels;

public interface IChannelRepository
{
    Task<IReadOnlyList<Channel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Channel?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task UpsertAsync(Channel channel, CancellationToken cancellationToken = default);
}
