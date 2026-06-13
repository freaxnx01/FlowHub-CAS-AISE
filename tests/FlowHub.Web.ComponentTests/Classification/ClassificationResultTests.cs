using FlowHub.Core.Classification;
using FluentAssertions;
using Xunit;

namespace FlowHub.Web.ComponentTests.Classification;

public class ClassificationResultTests
{
    [Fact]
    public void Constructor_DefaultsVikunjaProjectAndEntitiesToNull()
    {
        var result = new ClassificationResult(["tag"], "Vikunja");

        result.VikunjaProject.Should().BeNull();
        result.Entities.Should().BeNull();
    }

    [Fact]
    public void Constructor_AllowsSettingVikunjaProjectAndEntities()
    {
        var entities = new Dictionary<string, string> { ["author"] = "Richard Gabriel" };

        var result = new ClassificationResult(["quote"], "Vikunja", "title", "Zitate", entities);

        result.VikunjaProject.Should().Be("Zitate");
        result.Entities.Should().ContainKey("author").WhoseValue.Should().Be("Richard Gabriel");
    }
}
