using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowHub.Persistence.Entities;

internal sealed class CaptureEntityTypeConfiguration : IEntityTypeConfiguration<CaptureEntity>
{
    public void Configure(EntityTypeBuilder<CaptureEntity> builder)
    {
        builder.ToTable("Captures");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Content).IsRequired();
        builder.Property(c => c.Source).IsRequired().HasMaxLength(32);
        builder.Property(c => c.Stage).IsRequired().HasMaxLength(32);
        builder.Property(c => c.MatchedSkill).HasMaxLength(64);
        builder.Property(c => c.Title).HasMaxLength(512);
        builder.Property(c => c.ExternalRef).HasMaxLength(256);
        builder.Property(c => c.VikunjaProject).HasMaxLength(64);

        builder.HasIndex(c => c.Stage).HasDatabaseName("IX_Captures_Stage");
        builder.HasIndex(c => c.CreatedAt).IsDescending().HasDatabaseName("IX_Captures_CreatedAt_DESC");
        builder.HasIndex(c => c.MatchedSkill).HasDatabaseName("IX_Captures_MatchedSkill");

        builder.Property(c => c.Embedding)
            .HasColumnType("vector(1024)")
            .IsRequired(false);

        builder.OwnsOne(c => c.Attachment, a =>
        {
            a.Property(x => x.FileName).HasColumnName("Attachment_FileName").HasMaxLength(512);
            a.Property(x => x.ContentType).HasColumnName("Attachment_ContentType").HasMaxLength(128);
            a.Property(x => x.SizeBytes).HasColumnName("Attachment_SizeBytes");
            a.Property(x => x.RelativePath).HasColumnName("Attachment_RelativePath").HasMaxLength(256);
            a.Property(x => x.UploadedAt).HasColumnName("Attachment_UploadedAt");
        });
        builder.Navigation(c => c.Attachment).IsRequired(false);
    }
}
