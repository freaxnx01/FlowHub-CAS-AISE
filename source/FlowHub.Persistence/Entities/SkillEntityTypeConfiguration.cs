using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowHub.Persistence.Entities;

internal sealed class SkillEntityTypeConfiguration : IEntityTypeConfiguration<SkillEntity>
{
    public void Configure(EntityTypeBuilder<SkillEntity> builder)
    {
        builder.ToTable("Skills");
        builder.HasKey(s => s.Name);
        builder.Property(s => s.Name).HasMaxLength(64);
        builder.Property(s => s.Status).IsRequired().HasMaxLength(16);
        builder.Property(s => s.RoutedToday).HasDefaultValue(0);
    }
}
