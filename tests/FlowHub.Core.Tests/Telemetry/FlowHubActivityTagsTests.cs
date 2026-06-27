using System.Diagnostics;
using FlowHub.Core.Telemetry;
using FluentAssertions;
using Xunit;

namespace FlowHub.Core.Tests.Telemetry;

// Unit tests for the ADR 0009 §5 typed helpers. Each helper writes exactly one
// allow-listed `flowhub.*` tag and returns the activity for fluent chaining.
public class FlowHubActivityTagsTests
{
    private static readonly ActivitySource Source = new("FlowHubActivityTagsTests");
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

    private static Activity StartActivity() =>
        Source.StartActivity("op")
        ?? throw new InvalidOperationException("ActivitySource did not produce an Activity");

    [Fact]
    public void Source_IsTheCanonicalFlowHubName()
    {
        FlowHubActivityTags.Source.Should().Be("FlowHub");
    }

    [Fact]
    public void SetCaptureId_WritesGuidAsDFormat()
    {
        var id = new Guid("7c8f1234-5678-90ab-cdef-1234567890ab");
        using var a = StartActivity();
        var returned = a.SetCaptureId(id);
        returned.Should().BeSameAs(a);
        a.GetTagItem("flowhub.capture_id").Should().Be("7c8f1234-5678-90ab-cdef-1234567890ab");
    }

    [Fact]
    public void SetStage_WritesEnumValue()
    {
        using var a = StartActivity();
        a.SetStage("Classified");
        a.GetTagItem("flowhub.stage").Should().Be("Classified");
    }

    [Fact]
    public void SetClassificationSource_WritesEnumValue()
    {
        using var a = StartActivity();
        a.SetClassificationSource("AI");
        a.GetTagItem("flowhub.classification_source").Should().Be("AI");
    }

    [Theory]
    [InlineData("Vikunja")]
    [InlineData("Wallabag")]
    [InlineData(null)]
    public void SetMatchedSkill_AcceptsClosedSetIncludingNull(string? value)
    {
        using var a = StartActivity();
        a.SetMatchedSkill(value);
        a.GetTagItem("flowhub.matched_skill").Should().Be(value ?? string.Empty);
    }

    [Fact]
    public void SetSkillName_WritesValue()
    {
        using var a = StartActivity();
        a.SetSkillName("Wallabag");
        a.GetTagItem("flowhub.skill.name").Should().Be("Wallabag");
    }

    [Fact]
    public void SetSkillOutcome_WritesEnumValue()
    {
        using var a = StartActivity();
        a.SetSkillOutcome("Routed");
        a.GetTagItem("flowhub.skill.outcome").Should().Be("Routed");
    }

    [Fact]
    public void SetBodyLength_WritesIntValue()
    {
        using var a = StartActivity();
        a.SetBodyLength(342);
        a.GetTagItem("flowhub.body_length").Should().Be(342);
    }

    [Fact]
    public void SetTagCount_WritesIntValue()
    {
        using var a = StartActivity();
        a.SetTagCount(4);
        a.GetTagItem("flowhub.tag_count").Should().Be(4);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SetFallback_WritesBoolValue(bool value)
    {
        using var a = StartActivity();
        a.SetFallback(value);
        a.GetTagItem("flowhub.fallback").Should().Be(value);
    }

    [Fact]
    public void SetReason_WritesExceptionTypeName()
    {
        using var a = StartActivity();
        a.SetReason("HttpRequestException");
        a.GetTagItem("flowhub.reason").Should().Be("HttpRequestException");
    }

    [Fact]
    public void AllHelpers_NullSafe_OnNullActivity()
    {
        Activity? a = null;
        var act = () =>
        {
            a.SetCaptureId(Guid.NewGuid());
            a.SetStage("Raw");
            a.SetClassificationSource("Keyword");
            a.SetMatchedSkill("Vikunja");
            a.SetSkillName("Vikunja");
            a.SetSkillOutcome("Routed");
            a.SetBodyLength(0);
            a.SetTagCount(0);
            a.SetFallback(true);
            a.SetReason("none");
        };
        act.Should().NotThrow();
    }
}
