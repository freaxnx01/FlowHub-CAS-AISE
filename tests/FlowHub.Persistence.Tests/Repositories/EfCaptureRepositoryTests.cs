using FlowHub.Core.Captures;
using FlowHub.Core.Classification;
using FlowHub.Persistence.Repositories;
using FlowHub.Persistence.Tests.Fixtures;

namespace FlowHub.Persistence.Tests.Repositories;

[Collection(PostgresGroup.Name)]
public sealed class EfCaptureRepositoryTests(PostgresFixture fixture)
{
    private static Capture NewRawCapture(string content = "content", ChannelKind source = ChannelKind.Web) =>
        new(Guid.NewGuid(), source, content, DateTimeOffset.UtcNow, LifecycleStage.Raw, null);

    [Fact]
    public async Task AddAsync_PersistsCapture_AndReturnsDomainObject()
    {
        var db = await fixture.CreateFreshDbAsync();
        var repo = new EfCaptureRepository(db);
        var capture = NewRawCapture("https://example.com");

        var saved = await repo.AddAsync(capture);

        saved.Id.Should().Be(capture.Id);
        saved.Content.Should().Be("https://example.com");
        saved.Stage.Should().Be(LifecycleStage.Raw);
        saved.Source.Should().Be(ChannelKind.Web);

        var found = await repo.GetByIdAsync(capture.Id);
        found.Should().NotBeNull();
        found!.Id.Should().Be(capture.Id);
    }

    [Fact]
    public async Task UpdateAsync_PersistsEnrichmentDescription_AndReadsBack()
    {
        var db = await fixture.CreateFreshDbAsync();
        var repo = new EfCaptureRepository(db);
        var capture = NewRawCapture("\"Luck is what happens when preparation meets opportunity.\" — Seneca");
        await repo.AddAsync(capture);

        await repo.UpdateAsync(capture with
        {
            Stage = LifecycleStage.Classified,
            MatchedSkill = "Vikunja",
            VikunjaProject = "Zitate",
            EnrichmentDescription = "About Seneca: Roman Stoic philosopher and statesman.",
        });

        var found = await repo.GetByIdAsync(capture.Id);
        found!.EnrichmentDescription.Should().Be("About Seneca: Roman Stoic philosopher and statesman.");
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var db = await fixture.CreateFreshDbAsync();
        var repo = new EfCaptureRepository(db);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllCaptures_OrderedByCreatedAtDesc()
    {
        var db = await fixture.CreateFreshDbAsync();
        var repo = new EfCaptureRepository(db);
        var old = NewRawCapture("old") with { CreatedAt = DateTimeOffset.UtcNow.AddHours(-1) };
        var recent = NewRawCapture("recent") with { CreatedAt = DateTimeOffset.UtcNow };
        await repo.AddAsync(old);
        await repo.AddAsync(recent);

        var all = await repo.GetAllAsync();

        all.Should().HaveCount(2);
        all[0].Content.Should().Be("recent");
        all[1].Content.Should().Be("old");
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsTopNByCreatedAtDesc()
    {
        var db = await fixture.CreateFreshDbAsync();
        var repo = new EfCaptureRepository(db);
        for (var i = 0; i < 5; i++)
        {
            await repo.AddAsync(NewRawCapture($"item-{i}") with
            {
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-i)
            });
        }

        var result = await repo.GetRecentAsync(3);

        result.Should().HaveCount(3);
        result[0].Content.Should().Be("item-0");
    }

    [Fact]
    public async Task GetFailureCountsAsync_CountsOrphanAndUnhandled()
    {
        var db = await fixture.CreateFreshDbAsync();
        var repo = new EfCaptureRepository(db);
        var a = NewRawCapture("a");
        var b = NewRawCapture("b");
        var c = NewRawCapture("c");
        await repo.AddAsync(a);
        await repo.AddAsync(b);
        await repo.AddAsync(c);
        await repo.UpdateAsync(a with { Stage = LifecycleStage.Orphan, FailureReason = "x" });
        await repo.UpdateAsync(b with { Stage = LifecycleStage.Orphan, FailureReason = "x" });
        await repo.UpdateAsync(c with { Stage = LifecycleStage.Unhandled, FailureReason = "y" });

        var counts = await repo.GetFailureCountsAsync();

        counts.OrphanCount.Should().Be(2);
        counts.UnhandledCount.Should().Be(1);
    }

    [Fact]
    public async Task ListAsync_FiltersByStage()
    {
        var db = await fixture.CreateFreshDbAsync();
        var repo = new EfCaptureRepository(db);
        var raw = NewRawCapture("raw");
        var orphan = NewRawCapture("orphan");
        await repo.AddAsync(raw);
        await repo.AddAsync(orphan);
        await repo.UpdateAsync(orphan with { Stage = LifecycleStage.Orphan, FailureReason = "x" });

        var page = await repo.ListAsync(new CaptureFilter(
            Stages: [LifecycleStage.Orphan],
            Source: null,
            Limit: 50,
            Cursor: null));

        page.Items.Should().HaveCount(1);
        page.Items[0].Content.Should().Be("orphan");
    }

    [Fact]
    public async Task ListAsync_FiltersBySource()
    {
        var db = await fixture.CreateFreshDbAsync();
        var repo = new EfCaptureRepository(db);
        await repo.AddAsync(NewRawCapture("web", ChannelKind.Web));
        await repo.AddAsync(NewRawCapture("api", ChannelKind.Api));

        var page = await repo.ListAsync(new CaptureFilter(
            Stages: null,
            Source: ChannelKind.Api,
            Limit: 50,
            Cursor: null));

        page.Items.Should().HaveCount(1);
        page.Items[0].Content.Should().Be("api");
    }

    [Fact]
    public async Task ListAsync_CursorPagination_ReturnsNextCursor()
    {
        var db = await fixture.CreateFreshDbAsync();
        var repo = new EfCaptureRepository(db);
        for (var i = 0; i < 5; i++)
        {
            await repo.AddAsync(NewRawCapture($"item-{i}") with
            {
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-i)
            });
        }

        var page1 = await repo.ListAsync(new CaptureFilter(null, null, 3, null));

        page1.Items.Should().HaveCount(3);
        page1.Next.Should().NotBeNull();

        var page2 = await repo.ListAsync(new CaptureFilter(null, null, 3, page1.Next));

        page2.Items.Should().HaveCount(2);
        page2.Next.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_AppliesAllFieldChanges()
    {
        var db = await fixture.CreateFreshDbAsync();
        var repo = new EfCaptureRepository(db);
        var original = NewRawCapture("original");
        await repo.AddAsync(original);

        await repo.UpdateAsync(original with
        {
            Stage = LifecycleStage.Classified,
            MatchedSkill = "Wallabag",
            Title = "Some Title",
        });

        var updated = await repo.GetByIdAsync(original.Id);
        updated!.Stage.Should().Be(LifecycleStage.Classified);
        updated.MatchedSkill.Should().Be("Wallabag");
        updated.Title.Should().Be("Some Title");
    }

    [Fact]
    public async Task UpdateAsync_PersistsAndReadsBack_ClassifierTrace()
    {
        var db = await fixture.CreateFreshDbAsync();
        var repo = new EfCaptureRepository(db);
        var capture = NewRawCapture("classify me");
        await repo.AddAsync(capture);

        var traced = capture with
        {
            Stage = LifecycleStage.Classified,
            MatchedSkill = "Vikunja",
            ClassifierTrace = new ClassifierTrace(ClassifierKind.Ai, 1234, "OpenRouter", "gemma:free", 100, 20),
        };
        await repo.UpdateAsync(traced, default);
        var read = await repo.GetByIdAsync(capture.Id, default);

        read!.ClassifierTrace.Should().NotBeNull();
        read.ClassifierTrace!.Kind.Should().Be(ClassifierKind.Ai);
        read.ClassifierTrace.LatencyMs.Should().Be(1234);
        read.ClassifierTrace.Provider.Should().Be("OpenRouter");
        read.ClassifierTrace.Model.Should().Be("gemma:free");
        read.ClassifierTrace.PromptTokens.Should().Be(100);
        read.ClassifierTrace.CompletionTokens.Should().Be(20);
    }

    [Fact]
    public async Task UpdateAsync_NullTrace_StaysNull()
    {
        var db = await fixture.CreateFreshDbAsync();
        var repo = new EfCaptureRepository(db);
        var capture = NewRawCapture("no trace");
        await repo.AddAsync(capture);

        await repo.UpdateAsync(
            capture with { Stage = LifecycleStage.Classified, MatchedSkill = "Vikunja" }, default);
        var read = await repo.GetByIdAsync(capture.Id, default);

        read!.ClassifierTrace.Should().BeNull();
    }

    // --- Pin previously-uncovered ListAsync filter branches (#96 ratchet) ----

    [Fact]
    public async Task ListAsync_FiltersBySearchTerm_CaseInsensitive()
    {
        var db = await fixture.CreateFreshDbAsync();
        var repo = new EfCaptureRepository(db);
        await repo.AddAsync(NewRawCapture("invoice for Acme Corp"));
        await repo.AddAsync(NewRawCapture("random note"));

        var page = await repo.ListAsync(new CaptureFilter(
            Stages: null, Source: null, Limit: 50, Cursor: null, SearchTerm: "ACME"));

        page.Items.Should().ContainSingle();
        page.Items[0].Content.Should().Be("invoice for Acme Corp");
    }

    [Fact]
    public async Task ListAsync_FiltersBySearchTerm_AlsoMatchesTitle()
    {
        var db = await fixture.CreateFreshDbAsync();
        var repo = new EfCaptureRepository(db);
        var c1 = NewRawCapture("content one");
        var c2 = NewRawCapture("content two");
        await repo.AddAsync(c1);
        await repo.AddAsync(c2);
        await repo.UpdateAsync(c1 with { Title = "Quarterly review", Stage = LifecycleStage.Classified, MatchedSkill = "Books" });
        await repo.UpdateAsync(c2 with { Title = "Lunch menu", Stage = LifecycleStage.Classified, MatchedSkill = "Books" });

        var page = await repo.ListAsync(new CaptureFilter(
            Stages: null, Source: null, Limit: 50, Cursor: null, SearchTerm: "quarterly"));

        page.Items.Should().ContainSingle();
        page.Items[0].Title.Should().Be("Quarterly review");
    }

    [Fact]
    public async Task ListAsync_FiltersByTag_ReturnsOnlyTaggedCaptures()
    {
        var db = await fixture.CreateFreshDbAsync();
        var repo = new EfCaptureRepository(db);
        var tags = new EfTagRepository(db);
        var c1 = NewRawCapture("billing-content");
        var c2 = NewRawCapture("other-content");
        await repo.AddAsync(c1);
        await repo.AddAsync(c2);
        await tags.AddAsync(c1.Id, "billing");

        var page = await repo.ListAsync(new CaptureFilter(
            Stages: null, Source: null, Limit: 50, Cursor: null, Tag: "billing"));

        page.Items.Should().ContainSingle();
        page.Items[0].Id.Should().Be(c1.Id);
    }
}
