using FlowHub.Core.Captures;
using FluentAssertions;

namespace FlowHub.Core.Tests.Captures;

public class FailureCountsTests
{
    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(1, 0, true)]
    [InlineData(0, 1, true)]
    [InlineData(2, 3, true)]
    public void AnyFailures_IsTrue_WhenEitherCountIsPositive(int orphans, int unhandled, bool expected)
    {
        var counts = new FailureCounts(orphans, unhandled);

        counts.AnyFailures.Should().Be(expected);
    }
}
