using FlowHub.Api.Requests;
using FlowHub.Api.Validation;

namespace FlowHub.Web.ComponentTests.Api;

public sealed class CreateCaptureRequestValidatorTests
{
    private static readonly CreateCaptureRequestValidator Sut = new();

    [Fact]
    public async Task Valid_PassesValidation()
    {
        var request = new CreateCaptureRequest("https://example.com", ChannelKind.Api);

        var result = await Sut.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n")]
    public async Task EmptyContent_FailsValidation(string content)
    {
        var request = new CreateCaptureRequest(content, ChannelKind.Api);

        var result = await Sut.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCaptureRequest.Content)
            && e.ErrorMessage.Contains("not be empty", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ContentExceeding8192Chars_FailsValidation()
    {
        var request = new CreateCaptureRequest(new string('x', 8193), ChannelKind.Api);

        var result = await Sut.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCaptureRequest.Content)
            && e.ErrorMessage.Contains("8192", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UnknownSource_FailsValidation()
    {
        // Cast an out-of-range int to the enum to drive the IsInEnum rule.
        var request = new CreateCaptureRequest("ok", (ChannelKind)999);

        var result = await Sut.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCaptureRequest.Source));
    }
}
