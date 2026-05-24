using FlowHub.Core.Captures;
using FluentAssertions;

namespace FlowHub.Core.Tests.Captures;

public class AttachmentTests
{
    [Fact]
    public void Attachment_TwoInstancesWithSameValues_AreEqual()
    {
        var uploadedAt = DateTimeOffset.UtcNow;
        var a = new Attachment("invoice.pdf", "application/pdf", 1024, "2026/05/abc.pdf", uploadedAt);
        var b = new Attachment("invoice.pdf", "application/pdf", 1024, "2026/05/abc.pdf", uploadedAt);

        a.Should().Be(b);
    }
}
