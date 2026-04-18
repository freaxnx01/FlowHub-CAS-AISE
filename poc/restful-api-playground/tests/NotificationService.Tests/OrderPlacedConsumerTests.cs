using Contracts;
using FluentAssertions;
using Grpc.Core;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NotificationService.Consumers;
using NotificationService.Notifications;
using NSubstitute;
using OrderService.Grpc;
using Xunit;

namespace NotificationService.Tests;

public class OrderPlacedConsumerTests
{
    [Fact]
    public async Task Consume_OrderPlacedEvent_CreatesNotificationUsingGrpcReply()
    {
        var orderId = Guid.NewGuid();
        var store = new NotificationStore();

        var grpcClient = Substitute.For<OrderGrpc.OrderGrpcClient>();
        grpcClient
            .GetOrderByIdAsync(Arg.Is<GetOrderRequest>(r => r.OrderId == orderId.ToString()))
            .Returns(FakeAsyncUnaryCall(new OrderReply
            {
                OrderId = orderId.ToString(),
                Item = "Widget",
                Quantity = 5,
                Customer = "Alice",
                CreatedAt = DateTime.UtcNow.ToString("O")
            }));

        await using var provider = new ServiceCollection()
            .AddSingleton(store)
            .AddSingleton(grpcClient)
            .AddSingleton(NullLogger<OrderPlacedConsumer>.Instance)
            .AddMassTransitTestHarness(x => x.AddConsumer<OrderPlacedConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        await harness.Bus.Publish(new OrderPlaced(orderId, DateTime.UtcNow));

        (await harness.Consumed.Any<OrderPlaced>()).Should().BeTrue();

        var notifications = store.GetAll();
        notifications.Should().ContainSingle();
        notifications[0].OrderId.Should().Be(orderId);
        notifications[0].Message.Should().Contain("5x Widget").And.Contain("Alice");
    }

    private static AsyncUnaryCall<T> FakeAsyncUnaryCall<T>(T response) =>
        new(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => [],
            () => { });
}
