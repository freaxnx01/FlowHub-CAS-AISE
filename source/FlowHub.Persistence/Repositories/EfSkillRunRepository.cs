using FlowHub.Core.Captures;
using FlowHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowHub.Persistence.Repositories;

internal sealed class EfSkillRunRepository : ISkillRunRepository
{
    private readonly FlowHubDbContext _db;

    public EfSkillRunRepository(FlowHubDbContext db) => _db = db;

    public async Task<SkillRun> AddAsync(SkillRun skillRun, CancellationToken cancellationToken = default)
    {
        _db.SkillRuns.Add(new SkillRunEntity
        {
            Id = skillRun.Id,
            SkillName = skillRun.SkillName,
            CaptureId = skillRun.CaptureId,
            StartedAt = skillRun.StartedAt,
            CompletedAt = skillRun.CompletedAt,
            Success = skillRun.Success,
            FailureReason = skillRun.FailureReason,
        });
        await _db.SaveChangesAsync(cancellationToken);
        return skillRun;
    }

    public async Task<IReadOnlyList<SkillRun>> GetByCaptureIdAsync(
        Guid captureId, CancellationToken cancellationToken = default)
    {
        var entities = await _db.SkillRuns.AsNoTracking()
            .Where(r => r.CaptureId == captureId)
            .OrderByDescending(r => r.StartedAt)
            .ToListAsync(cancellationToken);
        return entities.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<SkillRun>> GetBySkillNameAsync(
        string skillName, CancellationToken cancellationToken = default)
    {
        var entities = await _db.SkillRuns.AsNoTracking()
            .Where(r => r.SkillName == skillName)
            .OrderByDescending(r => r.StartedAt)
            .ToListAsync(cancellationToken);
        return entities.Select(ToDomain).ToList();
    }

    private static SkillRun ToDomain(SkillRunEntity e) => new(
        Id: e.Id,
        SkillName: e.SkillName,
        CaptureId: e.CaptureId,
        StartedAt: e.StartedAt,
        CompletedAt: e.CompletedAt,
        Success: e.Success,
        FailureReason: e.FailureReason);
}
