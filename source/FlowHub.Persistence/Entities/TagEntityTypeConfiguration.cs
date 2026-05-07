using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowHub.Persistence.Entities;

internal sealed class TagEntityTypeConfiguration : IEntityTypeConfiguration<TagEntity>
{
    public void Configure(EntityTypeBuilder<TagEntity> builder)
    {
        builder.ToTable("Tags");
        builder.HasKey(t => new { t.CaptureId, t.Value });
        builder.Property(t => t.Value).HasMaxLength(64);

        builder.HasOne(t => t.Capture)
            .WithMany(c => c.Tags)
            .HasForeignKey(t => t.CaptureId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
