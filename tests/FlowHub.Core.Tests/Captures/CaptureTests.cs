using FlowHub.Core.Captures;
using FluentAssertions;

namespace FlowHub.Core.Tests.Captures;

public class CaptureTests
{
    [Fact]
    public void Capture_WithAttachment_PreservesAttachmentOnRecordEquality()
    {
        var att = new Attachment("a.pdf", "application/pdf", 10, "2026/05/x.pdf", DateTimeOffset.UnixEpoch);
        var c1 = new Capture(Guid.Empty, ChannelKind.Web, "a.pdf", DateTimeOffset.UnixEpoch, LifecycleStage.Raw, null, Attachment: att);
        var c2 = c1 with { };

        c2.Attachment.Should().Be(att);
        c2.Should().Be(c1);
    }
}
