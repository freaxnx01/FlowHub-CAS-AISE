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

        // Baseline catalog — every deployment starts with these six skills wired.
        // Counters/timestamps are populated by runtime activity, not seed data.
        builder.HasData(
            new SkillEntity { Name = "Books",     Status = "Healthy",  RoutedToday = 0 },
            new SkillEntity { Name = "Movies",    Status = "Healthy",  RoutedToday = 0 },
            new SkillEntity { Name = "Articles",  Status = "Healthy",  RoutedToday = 0 },
            new SkillEntity { Name = "Quotes",    Status = "Degraded", RoutedToday = 0 },
            new SkillEntity { Name = "Knowledge", Status = "Healthy",  RoutedToday = 0 },
            new SkillEntity { Name = "Belege",    Status = "Healthy",  RoutedToday = 0 });
    }
}
