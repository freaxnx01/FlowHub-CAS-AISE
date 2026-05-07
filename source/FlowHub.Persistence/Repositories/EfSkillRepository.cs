using FlowHub.Core.Health;
using FlowHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowHub.Persistence.Repositories;

internal sealed class EfSkillRepository : ISkillRepository
{
    private readonly FlowHubDbContext _db;

    public EfSkillRepository(FlowHubDbContext db) => _db = db;

    public async Task<IReadOnlyList<SkillHealth>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _db.Skills.AsNoTracking().ToListAsync(cancellationToken);
        return entities.Select(ToDomain).ToList();
    }

    public async Task<SkillHealth?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Skills.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Name == name, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task UpsertAsync(SkillHealth skill, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Skills.FirstOrDefaultAsync(s => s.Name == skill.Name, cancellationToken);
        if (entity is null)
        {
            _db.Skills.Add(ToEntity(skill));
        }
        else
        {
            entity.Status = skill.Status.ToString();
            entity.RoutedToday = skill.RoutedToday;
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task IncrementRoutedTodayAsync(string name, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Skills.FirstOrDefaultAsync(s => s.Name == name, cancellationToken)
            ?? throw new KeyNotFoundException($"Skill '{name}' not found.");
        entity.RoutedToday++;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static SkillHealth ToDomain(SkillEntity e) => new(
        Name: e.Name,
        Status: Enum.Parse<HealthStatus>(e.Status),
        RoutedToday: e.RoutedToday);

    private static SkillEntity ToEntity(SkillHealth s) => new()
    {
        Name = s.Name,
        Status = s.Status.ToString(),
        RoutedToday = s.RoutedToday,
    };
}
