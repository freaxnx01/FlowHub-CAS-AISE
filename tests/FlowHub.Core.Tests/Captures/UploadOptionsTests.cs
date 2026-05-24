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
}
