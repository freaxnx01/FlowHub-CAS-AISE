using System.Diagnostics;
using FluentAssertions;
using OpenTelemetry;
using Xunit;

namespace FlowHub.Web.ComponentTests.Observability;

// ADR 0009 §6 audit test — guards the PII policy for OTel tracing:
//  §1 allow-list: flowhub.* tags outside the closed set are stripped.
//  §2 block-list: forbidden third-party tags (http body, db.statement, gen_ai prompts,
//                 *.email/*.username/*.user.id) are stripped.
//  §4 length:     string values >256 chars are redacted.
//
// The processor type is `internal` to FlowHub.Web; we resolve it by name via the Web
// assembly (loaded as a project ref) and exercise OnEnd directly — the processor is pure
// and needs no live TracerProvider. The test fails loudly if the processor is removed.
public class TagAllowListProcessorTests
{
    private static readonly ActivitySource Source =
        new("TagAllowListProcessorTests");

    private static readonly ActivityListener Listener = StartListener();

    private static ActivityListener StartListener()
    {
        var l = new ActivityListener
        {
            ShouldListenTo = s => s.Name == Source.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(l);
        return l;
    }

    private static BaseProcessor<Activity> CreateProcessor()
    {
        var webAsm = typeof(FlowHub.Web.Components.App).Assembly;
        var t = webAsm.GetType("FlowHub.Web.Observability.TagAllowListProcessor", throwOnError: true)!;
        return (BaseProcessor<Activity>)Activator.CreateInstance(t, nonPublic: true)!;
    }

    private static Activity RunThroughProcessor(Action<Activity> seedTags)
    {
        var processor = CreateProcessor();
        using var act = Source.StartActivity("op")
            ?? throw new InvalidOperationException("ActivitySource did not produce an Activity — listener missing?");
        seedTags(act);
        processor.OnEnd(act);
        return act;
    }

    [Fact]
    public void UnknownFlowhubTag_IsStripped_AllowListedSurvives()
    {
        var captureId = Guid.NewGuid().ToString("D");
        var act = RunThroughProcessor(a =>
        {
            a.SetTag("flowhub.capture_id", captureId);  // allow-listed
            a.SetTag("flowhub.secret_thing", "leak");    // unknown → strip
        });

        act.GetTagItem("flowhub.capture_id").Should().Be(captureId);
        act.GetTagItem("flowhub.secret_thing").Should().BeNull();
    }

    [Theory]
    [InlineData("http.request.body.content")]
    [InlineData("http.response.body.size")]
    [InlineData("db.statement")]
    [InlineData("db.query.text")]
    [InlineData("gen_ai.prompt")]
    [InlineData("gen_ai.completion")]
    [InlineData("messaging.message.payload")]
    [InlineData("user.email")]
    [InlineData("operator.username")]
    [InlineData("auth.user.id")]
    public void ForbiddenTag_IsStripped(string key)
    {
        var act = RunThroughProcessor(a => a.SetTag(key, "should-not-export"));
        act.GetTagItem(key).Should().BeNull();
    }

    [Fact]
    public void LongStringValue_IsRedactedToLengthMarker()
    {
        var big = new string('A', 300); // >256
        var act = RunThroughProcessor(a => a.SetTag("flowhub.reason", big));

        act.GetTagItem("flowhub.reason").Should().Be("<redacted:length=300>");
    }
}
