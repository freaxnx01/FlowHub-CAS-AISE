using System.Text;
using FlowHub.Core.Captures;
using FluentAssertions;

namespace FlowHub.Core.Tests.Captures;

public class CaptureCursorTests
{
    [Fact]
    public void Encode_ProducesBase64Url_WithoutPaddingOrUrlUnsafeChars()
    {
        var cursor = new CaptureCursor(DateTimeOffset.UnixEpoch, Guid.NewGuid());

        var token = cursor.Encode();

        token.Should().NotBeNullOrEmpty();
        token.Should().NotContain("=").And.NotContain("+").And.NotContain("/");
    }

    [Fact]
    public void Decode_AfterEncode_RoundTripsToSameValue()
    {
        var cursor = new CaptureCursor(new DateTimeOffset(2026, 6, 21, 12, 30, 0, TimeSpan.Zero), Guid.NewGuid());

        var roundTripped = CaptureCursor.Decode(cursor.Encode());

        roundTripped.Should().Be(cursor);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Decode_WithNullOrEmptyToken_ThrowsFormatException(string? token)
    {
        var act = () => CaptureCursor.Decode(token!);

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Decode_WithInvalidBase64_ThrowsFormatException()
    {
        // '!' is not a Base64 character → Convert.FromBase64String throws FormatException directly.
        var act = () => CaptureCursor.Decode("!!!!");

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Decode_WithValidBase64ButInvalidJson_ThrowsFormatException()
    {
        // Valid Base64Url that decodes to non-JSON → JsonException is wrapped as FormatException.
        var token = Base64Url("{not json");

        var act = () => CaptureCursor.Decode(token);

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Decode_WhenPayloadIsJsonNull_ThrowsFormatException()
    {
        // The JSON literal `null` deserializes to a null cursor.
        var token = Base64Url("null");

        var act = () => CaptureCursor.Decode(token);

        act.Should().Throw<FormatException>();
    }

    private static string Base64Url(string text) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(text))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
