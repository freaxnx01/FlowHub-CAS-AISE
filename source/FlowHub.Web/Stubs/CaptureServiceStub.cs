using Bogus;
using FlowHub.Core.Captures;

namespace FlowHub.Web.Stubs;

/// <summary>
/// Bogus-backed in-memory stub for <see cref="ICaptureService"/>.
/// Block 2 test data — gets replaced by a real implementation in Block 3.
/// </summary>
public sealed class CaptureServiceStub : ICaptureService
{
    private static readonly string[] SkillNames =
        ["Movies", "Articles", "Books", "Quotes", "Knowledge", "Homelab", "Belege"];

    private static readonly string[] SampleContent =
    [
        "Inception (2010) — rewatch",
        "https://heise.de/select/ct/2026/5/2532311091092661684",
        "https://galaxus.ch/de/s18/product/eine-kurze-geschichte-der-menschheit",
        "Schmidts Katze — Bezirksch...",
        "https://example.com/weird-thing-that-no-skill-knows",
        "\"Information is the resolution of uncertainty\" — Shannon",
        "AdGuard Home self-host",
        "ct 2026/05 article snippet on opkssh",
        "The Imitation Game — Alan Turing",
        "Galaxus Quittung 2026-04-09",
        "https://jellyfin.org/",
        "Star Trek: Strange New Worlds S03",
    ];

    private readonly List<Capture> _captures;

    public CaptureServiceStub()
    {
        var rng = new Faker { Random = new Bogus.Randomizer(42) };
        var now = DateTimeOffset.UtcNow;

        _captures = Enumerable.Range(0, 12)
            .Select(i => new Capture(
                Id: rng.Random.Guid(),
                Source: rng.PickRandom<ChannelKind>(),
                Content: SampleContent[i % SampleContent.Length],
                CreatedAt: now.AddMinutes(-(i * rng.Random.Int(2, 8) + rng.Random.Int(0, 3))),
                Stage: PickStage(rng, i),
                MatchedSkill: PickSkill(rng, i)))
            .ToList();
    }

    public Task<IReadOnlyList<Capture>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Capture> all = _captures
            .OrderByDescending(c => c.CreatedAt)
            .ToList();
        return Task.FromResult(all);
    }

    public Task<IReadOnlyList<Capture>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Capture> recent = _captures
            .OrderByDescending(c => c.CreatedAt)
            .Take(count)
            .ToList();
        return Task.FromResult(recent);
    }

    public Task<FailureCounts> GetFailureCountsAsync(CancellationToken cancellationToken = default)
    {
        var orphan = _captures.Count(c => c.Stage == LifecycleStage.Orphan);
        var unhandled = _captures.Count(c => c.Stage == LifecycleStage.Unhandled);
        return Task.FromResult(new FailureCounts(orphan, unhandled));
    }

    public Task<Capture> SubmitAsync(string content, ChannelKind source, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var capture = new Capture(
            Id: Guid.NewGuid(),
            Source: source,
            Content: content,
            CreatedAt: DateTimeOffset.UtcNow,
            Stage: LifecycleStage.Raw,
            MatchedSkill: null);

        _captures.Add(capture);
        return Task.FromResult(capture);
    }

    private static LifecycleStage PickStage(Faker rng, int index)
    {
        // Distribution biased towards Routed but with a couple of failures
        // so the dashboard's Needs Attention widget has something to show.
        return index switch
        {
            2 or 8 => LifecycleStage.Orphan,
            4 => LifecycleStage.Unhandled,
            _ => LifecycleStage.Routed,
        };
    }

    private static string? PickSkill(Faker rng, int index)
    {
        if (index == 4)
        {
            return null; // Unhandled has no matched Skill.
        }

        return SkillNames[index % SkillNames.Length];
    }
}
