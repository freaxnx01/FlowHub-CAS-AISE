using FlowHub.Core.Captures;
using FlowHub.Web.Uploads;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace FlowHub.Web.ComponentTests.Uploads;

public class UploadPolicyTests
{
    [Fact]
    public void AcceptAttribute_JoinsAllowedContentTypesWithCommas()
    {
        var opts = Options.Create(new UploadOptions
        {
            StoragePath = "App_Data/uploads",
            MaxBytes = 2_097_152,
            AllowedContentTypes = ["application/pdf", "image/png"],
        });
        var policy = new UploadPolicy(new TestMonitor(opts.Value));

        policy.AcceptAttribute.Should().Be("application/pdf,image/png");
        policy.MaxBytes.Should().Be(2_097_152);
    }

    private sealed class TestMonitor(UploadOptions current) : IOptionsMonitor<UploadOptions>
    {
        public UploadOptions CurrentValue { get; } = current;
        public UploadOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<UploadOptions, string?> listener) => null;
    }
}
