using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowHub.Persistence.Entities;

internal sealed class ChannelEntityTypeConfiguration : IEntityTypeConfiguration<ChannelEntity>
{
    public void Configure(EntityTypeBuilder<ChannelEntity> builder)
    {
        builder.ToTable("Channels");
        builder.HasKey(c => c.Name);
        builder.Property(c => c.Name).HasMaxLength(64);
        builder.Property(c => c.Kind).IsRequired().HasMaxLength(32);
        builder.Property(c => c.Status).IsRequired().HasMaxLength(16);
    }
}
