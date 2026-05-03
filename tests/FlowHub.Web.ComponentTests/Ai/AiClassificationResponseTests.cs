using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using FlowHub.AI;
using FluentAssertions;

namespace FlowHub.Web.ComponentTests.Ai;

public sealed class AiClassificationResponseTests
{
    [Fact]
    public void Tags_HasDescription_ForSchemaGeneration()
    {
        var prop = typeof(AiClassificationResponse).GetProperty(nameof(AiClassificationResponse.Tags))!;
        var desc = prop.GetCustomAttribute<DescriptionAttribute>();

        desc.Should().NotBeNull();
        desc!.Description.Should().Contain("tags");
    }

    [Fact]
    public void MatchedSkill_AllowedValuesEnumeratesWallabagVikunjaEmpty()
    {
        var prop = typeof(AiClassificationResponse).GetProperty(nameof(AiClassificationResponse.MatchedSkill))!;
        var allowed = prop.GetCustomAttribute<AllowedValuesAttribute>();

        allowed.Should().NotBeNull();
        allowed!.Values.Should().BeEquivalentTo(new object[] { "Wallabag", "Vikunja", "" });
    }

    [Fact]
    public void Title_IsNullableString()
    {
        var prop = typeof(AiClassificationResponse).GetProperty(nameof(AiClassificationResponse.Title))!;

        prop.PropertyType.Should().Be<string>();
        // Reading nullability via reflection in C# 10+ requires NullabilityInfoContext.
        var ctx = new NullabilityInfoContext();
        ctx.Create(prop).WriteState.Should().Be(NullabilityState.Nullable);
    }
}
