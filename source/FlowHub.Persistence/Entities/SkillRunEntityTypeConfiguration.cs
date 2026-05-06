using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowHub.Persistence.Entities;

internal sealed class SkillRunEntityTypeConfiguration : IEntityTypeConfiguration<SkillRunEntity>
{
    public void Configure(EntityTypeBuilder<SkillRunEntity> builder)
    {
        builder.ToTable("SkillRuns");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.SkillName).IsRequired().HasMaxLength(64);

        builder.HasOne(r => r.Skill)
            .WithMany()
            .HasForeignKey(r => r.SkillName)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Capture)
            .WithMany()
            .HasForeignKey(r => r.CaptureId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
