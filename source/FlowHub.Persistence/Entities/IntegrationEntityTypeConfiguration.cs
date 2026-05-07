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
    }
}
