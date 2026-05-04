using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlowHub.Skills;

internal sealed partial class SkillsBootLogger : IHostedService
{
    private readonly ILogger<SkillsBootLogger> _log;
    private readonly IReadOnlyList<SkillsRegistrationOutcome> _outcomes;

    public SkillsBootLogger(ILogger<SkillsBootLogger> log, IEnumerable<SkillsRegistrationOutcome> outcomes)
    {
        _log = log;
        _outcomes = outcomes.ToList();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var outcome in _outcomes)
        {
            if (outcome.Registered) { LogSkillRegistered(outcome.Skill); }
            else { LogSkillNotConfigured(outcome.Skill, outcome.Reason); }
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(EventId = 4020, Level = LogLevel.Information, Message = "Skill registered (skill={Skill})")]
    private partial void LogSkillRegistered(string skill);

    [LoggerMessage(EventId = 4021, Level = LogLevel.Information,
        Message = "Skill not configured — capture matching {Skill} will go to Unhandled (reason={Reason})")]
    private partial void LogSkillNotConfigured(string skill, string reason);
}
