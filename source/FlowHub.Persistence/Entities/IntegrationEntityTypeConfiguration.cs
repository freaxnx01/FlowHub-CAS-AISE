using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowHub.Persistence.Entities;

internal sealed class IntegrationEntityTypeConfiguration : IEntityTypeConfiguration<IntegrationEntity>
{
    public void Configure(EntityTypeBuilder<IntegrationEntity> builder)
    {
        builder.ToTable("Integrations");
        builder.HasKey(i => i.Name);
        builder.Property(i => i.Name).HasMaxLength(64);
        builder.Property(i => i.Status).IsRequired().HasMaxLength(16);

        // Baseline catalog — every deployment knows about these six integrations.
        // Status defaults to Healthy; LastWriteAt stays null until the first real
        // write happens (no synthetic timestamps in seed data).
        builder.HasData(
            new IntegrationEntity { Name = "Wallabag",  Status = "Healthy" },
            new IntegrationEntity { Name = "Wekan",     Status = "Healthy" },
            new IntegrationEntity { Name = "Vikunja",   Status = "Healthy" },
            new IntegrationEntity { Name = "Paperless", Status = "Healthy" },
            new IntegrationEntity { Name = "Obsidian",  Status = "Healthy" },
            new IntegrationEntity { Name = "Authentik", Status = "Healthy" });
    }
}
