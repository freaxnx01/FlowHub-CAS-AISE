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
        builder.Property(c => c.EnrichmentDescription);

        builder.HasIndex(c => c.Stage).HasDatabaseName("IX_Captures_Stage");
        builder.HasIndex(c => c.CreatedAt).IsDescending().HasDatabaseName("IX_Captures_CreatedAt_DESC");
        builder.HasIndex(c => c.MatchedSkill).HasDatabaseName("IX_Captures_MatchedSkill");

        // 384-dim — sized for a multilingual-e5-small-class OpenAI-compatible embedder.
        // mistral-embed (1024) remains a documented swap: change this to vector(1024) + a
        // migration (see ADR 0006). Embeddings are disabled on the public demo.
        builder.Property(c => c.Embedding)
            .HasColumnType("vector(384)")
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

        builder.OwnsOne(c => c.ClassifierTrace, t =>
        {
            t.Property(x => x.Kind).HasColumnName("ClassifierTrace_Kind").HasMaxLength(16);
            t.Property(x => x.LatencyMs).HasColumnName("ClassifierTrace_LatencyMs");
            t.Property(x => x.Provider).HasColumnName("ClassifierTrace_Provider").HasMaxLength(32);
            t.Property(x => x.Model).HasColumnName("ClassifierTrace_Model").HasMaxLength(128);
            t.Property(x => x.PromptTokens).HasColumnName("ClassifierTrace_PromptTokens");
            t.Property(x => x.CompletionTokens).HasColumnName("ClassifierTrace_CompletionTokens");
        });
        builder.Navigation(c => c.ClassifierTrace).IsRequired(false);
    }
}
