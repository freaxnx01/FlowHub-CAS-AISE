using FlowHub.Core.Events;
using FlowHub.Web.Notifications;
using FlowHub.Web.Pipeline;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowHub.Web.ComponentTests.Pipeline;

public sealed class CaptureNotificationConsumerTests
{
    [Fact]
    public async Task Consume_ForwardsCaptureCreatedToNotifier()
    {
        var notifier = Substitute.For<ICaptureNotifier>();
        notifier.NotifyCaptureCreatedAsync(Arg.Any<CaptureCreated>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>),
            typeof(Microsoft.Extensions.Logging.Abstractions.NullLogger<>));
        services.AddSingleton(notifier);
        services.AddMassTransitTestHarness(x => x.AddConsumer<CaptureNotificationConsumer>());

        await using var sp = services.BuildServiceProvider(true);
        var harness = sp.GetRequiredService<ITestHarness>();
        await harness.Start();

        var msg = new CaptureCreated(Guid.NewGuid(), "hi", ChannelKind.Telegram, DateTimeOffset.UtcNow);
        await sp.GetRequiredService<IBus>().Publish(msg);

        (await harness.Consumed.Any<CaptureCreated>(x => x.Context.Message.CaptureId == msg.CaptureId))
            .Should().BeTrue();

        await notifier.Received(1).NotifyCaptureCreatedAsync(
            Arg.Is<CaptureCreated>(c => c.CaptureId == msg.CaptureId),
            Arg.Any<CancellationToken>());
    }
}
