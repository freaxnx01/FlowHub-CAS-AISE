using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlowHub.AI;

internal sealed partial class AiBootLogger : IHostedService
{
    private readonly ILogger<AiBootLogger> _log;
    private readonly AiRegistrationOutcome _outcome;

    public AiBootLogger(ILogger<AiBootLogger> log, AiRegistrationOutcome outcome)
    {
        _log = log;
        _outcome = outcome;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_outcome.UsesAi)
        {
            var provider = _outcome.Provider!.Value.ToString();
            var model = _outcome.Model!;
            LogProviderRegistered(provider, model);
        }
        else
        {
            LogProviderNotConfigured(_outcome.Reason);
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(
        EventId = 3020,
        Level = LogLevel.Information,
        Message = "AI provider registered (provider={Provider}, model={Model})")]
    private partial void LogProviderRegistered(string provider, string model);

    [LoggerMessage(
        EventId = 3021,
        Level = LogLevel.Information,
        Message = "AI provider not configured — falling back to KeywordClassifier (reason={Reason})")]
    private partial void LogProviderNotConfigured(string reason);
}
