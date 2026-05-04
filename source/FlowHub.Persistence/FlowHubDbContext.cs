using FlowHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowHub.Persistence;

public sealed class FlowHubDbContext : DbContext
{
    public FlowHubDbContext(DbContextOptions<FlowHubDbContext> options) : base(options) { }

    internal DbSet<CaptureEntity> Captures => Set<CaptureEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var captures = modelBuilder.Entity<CaptureEntity>();
        captures.ToTable("Captures");
        captures.HasKey(c => c.Id);
        captures.Property(c => c.Content).IsRequired();
        captures.Property(c => c.Source).IsRequired().HasMaxLength(32);
        captures.Property(c => c.Stage).IsRequired().HasMaxLength(32);
        captures.Property(c => c.MatchedSkill).HasMaxLength(64);
        captures.Property(c => c.Title).HasMaxLength(512);
        captures.Property(c => c.ExternalRef).HasMaxLength(256);

        // Drives the Dashboard "Needs Attention" count + lifecycle filter on /captures.
        captures.HasIndex(c => c.Stage).HasDatabaseName("IX_Captures_Stage");
        // Drives Recent Captures (DESC) and the cursor pagination on /captures.
        captures.HasIndex(c => c.CreatedAt).IsDescending().HasDatabaseName("IX_Captures_CreatedAt_DESC");
    }
}
