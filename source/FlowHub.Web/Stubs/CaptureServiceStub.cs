using Bogus;
using FlowHub.Core.Captures;
using FlowHub.Core.Events;
using MassTransit;

namespace FlowHub.Web.Stubs;

/// <summary>
/// Bogus-backed in-memory stub for <see cref="ICaptureService"/>.
/// Block 3 Slice B: publishes <see cref="CaptureCreated"/> on submit and exposes
/// state-transition methods used by the pipeline consumers.
/// EF Core-backed implementation lands in Block 4.
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
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly object _lock = new();

    public CaptureServiceStub(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;

        var rng = new Faker { Random = new Bogus.Randomizer(42) };
        var now = DateTimeOffset.UtcNow;

        _captures = Enumerable.Range(0, 12)
            .Select(i => new Capture(
                Id: rng.Random.Guid(),
                Source: rng.PickRandom<ChannelKind>(),
                Content: SampleContent[i % SampleContent.Length],
                CreatedAt: now.AddMinutes(-(i * rng.Random.Int(2, 8) + rng.Random.Int(0, 3))),
                Stage: PickStage(rng, i),
                MatchedSkill: PickSkill(rng, i),
                FailureReason: PickFailureReason(i)))
            .ToList();
    }

    public Task<Capture?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_captures.FirstOrDefault(c => c.Id == id));
        }
    }

    public Task<IReadOnlyList<Capture>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            IReadOnlyList<Capture> all = _captures.OrderByDescending(c => c.CreatedAt).ToList();
            return Task.FromResult(all);
        }
    }

    public Task<IReadOnlyList<Capture>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            IReadOnlyList<Capture> recent = _captures.OrderByDescending(c => c.CreatedAt).Take(count).ToList();
            return Task.FromResult(recent);
        }
    }

    public Task<FailureCounts> GetFailureCountsAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var orphan = _captures.Count(c => c.Stage == LifecycleStage.Orphan);
            var unhandled = _captures.Count(c => c.Stage == LifecycleStage.Unhandled);
            return Task.FromResult(new FailureCounts(orphan, unhandled));
        }
    }

    public async Task<Capture> SubmitAsync(string content, ChannelKind source, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var capture = new Capture(
            Id: Guid.NewGuid(),
            Source: source,
            Content: content,
            CreatedAt: DateTimeOffset.UtcNow,
            Stage: LifecycleStage.Raw,
            MatchedSkill: null);

        lock (_lock)
        {
            _captures.Add(capture);
        }

        await _publishEndpoint.Publish(
            new CaptureCreated(capture.Id, capture.Content, capture.Source, capture.CreatedAt),
            cancellationToken);

        return capture;
    }

    public Task MarkClassifiedAsync(Guid id, string matchedSkill, CancellationToken cancellationToken = default) =>
        ReplaceCapture(id, c => c with { Stage = LifecycleStage.Classified, MatchedSkill = matchedSkill });

    public Task MarkRoutedAsync(Guid id, CancellationToken cancellationToken = default) =>
        ReplaceCapture(id, c => c with { Stage = LifecycleStage.Routed });

    public Task MarkOrphanAsync(Guid id, string reason, CancellationToken cancellationToken = default) =>
        ReplaceCapture(id, c => c with { Stage = LifecycleStage.Orphan, FailureReason = reason });

    public Task MarkUnhandledAsync(Guid id, string reason, CancellationToken cancellationToken = default) =>
        ReplaceCapture(id, c => c with { Stage = LifecycleStage.Unhandled, FailureReason = reason });

    public Task<CapturePage> ListAsync(CaptureFilter filter, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Capture> items;
        CaptureCursor? next;

        lock (_lock)
        {
            IEnumerable<Capture> query = _captures
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id);

            if (filter.Stages is { Count: > 0 } stages)
            {
                query = query.Where(c => stages.Contains(c.Stage));
            }

            if (filter.Source is ChannelKind src)
            {
                query = query.Where(c => c.Source == src);
            }

            if (filter.Cursor is CaptureCursor cursor)
            {
                query = query.SkipWhile(c =>
                    c.CreatedAt > cursor.CreatedAt
                    || (c.CreatedAt == cursor.CreatedAt && c.Id.CompareTo(cursor.Id) >= 0));
            }

            var limit = Math.Clamp(filter.Limit, 1, 200);
            var page = query.Take(limit + 1).ToList();

            if (page.Count > limit)
            {
                var last = page[limit - 1];
                next = new CaptureCursor(last.CreatedAt, last.Id);
                items = page.Take(limit).ToList();
            }
            else
            {
                next = null;
                items = page;
            }
        }

        return Task.FromResult(new CapturePage(items, next));
    }

    private Task ReplaceCapture(Guid id, Func<Capture, Capture> transform)
    {
        lock (_lock)
        {
            var index = _captures.FindIndex(c => c.Id == id);
            if (index < 0)
            {
                throw new KeyNotFoundException($"Capture {id} not found.");
            }
            _captures[index] = transform(_captures[index]);
        }
        return Task.CompletedTask;
    }

    private static LifecycleStage PickStage(Faker rng, int index) => index switch
    {
        2 or 8 => LifecycleStage.Orphan,
        4 => LifecycleStage.Unhandled,
        6 => LifecycleStage.Routed,
        _ => LifecycleStage.Completed,
    };

    private static string? PickSkill(Faker rng, int index)
    {
        if (index == 4)
        {
            return null;
        }
        return SkillNames[index % SkillNames.Length];
    }

    private static string? PickFailureReason(int index) => index switch
    {
        2 => "Wallabag API returned 503 Service Unavailable — the Integration was unreachable.",
        8 => "Vikunja write timed out after 30 s — the list could not be updated.",
        _ => null,
    };
}
