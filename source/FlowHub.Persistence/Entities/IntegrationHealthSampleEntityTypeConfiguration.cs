using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowHub.Persistence.Entities;

internal sealed class IntegrationHealthSampleEntityTypeConfiguration
    : IEntityTypeConfiguration<IntegrationHealthSampleEntity>
{
    public void Configure(EntityTypeBuilder<IntegrationHealthSampleEntity> builder)
    {
        builder.ToTable("IntegrationHealthSamples");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.IntegrationName).IsRequired().HasMaxLength(64);
        builder.Property(s => s.Status).IsRequired().HasMaxLength(16);

        builder.HasOne(s => s.Integration)
            .WithMany(i => i.Samples)
            .HasForeignKey(s => s.IntegrationName)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.IntegrationName, s.SampledAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_IntegrationHealthSamples_IntegrationName_SampledAt_DESC");
    }
}
