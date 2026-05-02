// tests/FlowHub.Web.ComponentTests/Captures/CaptureCursorTests.cs
using FlowHub.Core.Captures;
using FluentAssertions;

namespace FlowHub.Web.ComponentTests.Captures;

public sealed class CaptureCursorTests
{
    [Fact]
    public void Encode_AndThen_Decode_RoundTripsValues()
    {
        var original = new CaptureCursor(
            CreatedAt: new DateTimeOffset(2026, 5, 2, 10, 0, 0, TimeSpan.FromHours(2)),
            Id: Guid.Parse("11111111-2222-3333-4444-555555555555"));

        var token = original.Encode();
        var decoded = CaptureCursor.Decode(token);

        decoded.Should().Be(original);
    }

    [Fact]
    public void Encode_ReturnsUrlSafeBase64()
    {
        var cursor = new CaptureCursor(DateTimeOffset.UtcNow, Guid.NewGuid());

        var token = cursor.Encode();

        token.Should().NotContain("+");
        token.Should().NotContain("/");
        token.Should().NotContain("=");
    }

    [Fact]
    public void Decode_MalformedToken_Throws()
    {
        Action act = () => CaptureCursor.Decode("not-a-valid-cursor");
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Decode_EmptyString_Throws()
    {
        Action act = () => CaptureCursor.Decode(string.Empty);
        act.Should().Throw<FormatException>();
    }
}
