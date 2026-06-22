using System.ComponentModel.DataAnnotations;
using FlowHub.Core.Captures;
using FluentAssertions;

namespace FlowHub.Core.Tests.Captures;

public class UploadOptionsTests
{
    [Fact]
    public void UploadOptions_MissingStoragePath_FailsValidation()
    {
        var opts = new UploadOptions { StoragePath = "", MaxBytes = 1024, AllowedContentTypes = ["application/pdf"] };
        var ctx = new ValidationContext(opts);
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(opts, ctx, results, validateAllProperties: true).Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(UploadOptions.StoragePath)));
    }

    [Fact]
    public void UploadOptions_NegativeMaxBytes_FailsValidation()
    {
        var opts = new UploadOptions { StoragePath = "App_Data/uploads", MaxBytes = -1, AllowedContentTypes = ["application/pdf"] };
        var ctx = new ValidationContext(opts);
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(opts, ctx, results, validateAllProperties: true).Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(UploadOptions.MaxBytes)));
    }

    // --- Pin the defaults (issue #96): a zero-config `new UploadOptions()` must
    //     keep producing the documented defaults; surviving Stryker mutants
    //     showed the existing tests didn't lock these strings/sizes down.

    [Fact]
    public void DefaultStoragePath_PinsContract()
    {
        new UploadOptions().StoragePath.Should().Be("App_Data/uploads");
    }

    [Fact]
    public void DefaultAllowedContentTypes_PinsContract()
    {
        new UploadOptions().AllowedContentTypes.Should().Equal(
            "application/pdf",
            "image/png",
            "image/jpeg");
    }

    [Fact]
    public void DefaultMaxBytes_IsTwoMegabytes()
    {
        UploadOptions.DefaultMaxBytes.Should().Be(2 * 1024 * 1024);
        new UploadOptions().MaxBytes.Should().Be(UploadOptions.DefaultMaxBytes);
    }
}
