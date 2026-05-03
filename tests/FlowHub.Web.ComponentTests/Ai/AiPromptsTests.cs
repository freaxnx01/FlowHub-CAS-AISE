using FlowHub.AI;
using FluentAssertions;
using Microsoft.Extensions.AI;

namespace FlowHub.Web.ComponentTests.Ai;

public sealed class AiPromptsTests
{
    [Fact]
    public void BuildMessages_AnyContent_FirstMessageIsSystemPrompt()
    {
        var messages = AiPrompts.BuildMessages("https://example.com");

        messages.Should().HaveCount(2);
        messages[0].Role.Should().Be(ChatRole.System);
        messages[0].Text.Should().Contain("FlowHub");
        messages[0].Text.Should().Contain("Wallabag");
        messages[0].Text.Should().Contain("Vikunja");
    }

    [Fact]
    public void BuildMessages_AnyContent_SecondMessageIsRawUserContent()
    {
        const string content = "todo: buy milk on Saturday";

        var messages = AiPrompts.BuildMessages(content);

        messages[1].Role.Should().Be(ChatRole.User);
        messages[1].Text.Should().Be(content);
    }

    [Fact]
    public void SystemPrompt_HasNoGermanRoutingTokens()
    {
        // Spec D6 / Prompt strategy: the system prompt is English to keep Llama 3.1
        // routing tokens stable. Capture content can still be German — that's the user
        // message, not the system prompt.
        AiPrompts.SystemPrompt.Should().NotContain("Ablage");
        AiPrompts.SystemPrompt.Should().NotContain("Aufgabe");
    }
}
