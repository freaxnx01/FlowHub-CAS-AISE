using FlowHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowHub.Persistence;

public sealed class FlowHubDbContext : DbContext
{
    public FlowHubDbContext(DbContextOptions<FlowHubDbContext> options) : base(options) { }

    internal DbSet<CaptureEntity> Captures => Set<CaptureEntity>();
    internal DbSet<ChannelEntity> Channels => Set<ChannelEntity>();
    internal DbSet<SkillEntity> Skills => Set<SkillEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FlowHubDbContext).Assembly);
    }
}
