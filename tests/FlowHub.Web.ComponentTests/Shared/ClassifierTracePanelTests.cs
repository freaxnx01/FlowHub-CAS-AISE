using FlowHub.Core.Classification;
using FlowHub.Web.Components.Shared;
using MudBlazor.Services;

namespace FlowHub.Web.ComponentTests.Shared;

public sealed class ClassifierTracePanelTests : TestContext
{
    public ClassifierTracePanelTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Renders_AiTrace_WithModelTokens()
    {
        var trace = new ClassifierTrace(ClassifierKind.Ai, 1200, "OpenRouter", "gemma:free", 100, 20);
        var cut = RenderComponent<ClassifierTracePanel>(p => p
            .Add(x => x.Trace, trace)
            .Add(x => x.EstimatedCostUsd, 0m));

        cut.Markup.Should().Contain("OpenRouter");
        cut.Markup.Should().Contain("gemma:free");
        cut.Markup.Should().Contain("1200");
        cut.Markup.Should().Contain("100");
        cut.Markup.Should().Contain("20");
    }

    [Fact]
    public void Renders_NoClassificationNote_WhenTraceNull()
    {
        var cut = RenderComponent<ClassifierTracePanel>(p => p
            .Add(x => x.Trace, (ClassifierTrace?)null)
            .Add(x => x.EstimatedCostUsd, (decimal?)null));

        cut.Markup.Should().Contain("without LLM classification");
    }

    [Fact]
    public void Renders_AiTrace_WithUnknownCost_ShowsEmDash()
    {
        // EstimatedCostUsd=null hits the `null => "—"` arm in FormatCost.
        var trace = new ClassifierTrace(ClassifierKind.Ai, 800, "OpenRouter", "gemma:free", 50, 10);
        var cut = RenderComponent<ClassifierTracePanel>(p => p
            .Add(x => x.Trace, trace)
            .Add(x => x.EstimatedCostUsd, (decimal?)null));

        cut.Markup.Should().Contain("Est. cost:");
        cut.Markup.Should().Contain("—");
    }

    [Fact]
    public void Renders_AiTrace_WithMeasuredCost_ShowsFourDecimalDollarAmount()
    {
        // Non-zero, non-null cost hits the `_ => "${cost:0.0000}"` arm in FormatCost.
        var trace = new ClassifierTrace(ClassifierKind.Ai, 800, "OpenRouter", "claude-haiku", 1234, 456);
        var cut = RenderComponent<ClassifierTracePanel>(p => p
            .Add(x => x.Trace, trace)
            .Add(x => x.EstimatedCostUsd, 0.0123m));

        cut.Markup.Should().Contain("$0.0123");
    }
}
