using Contracts;
using MassTransit;
using NotificationService.Notifications;
using OrderService.Grpc;

namespace NotificationService.Consumers;

public class OrderPlacedConsumer(
    NotificationStore store,
    OrderGrpc.OrderGrpcClient orderClient,
    ILogger<OrderPlacedConsumer> logger) : IConsumer<OrderPlaced>
{
    public async Task Consume(ConsumeContext<OrderPlaced> context)
    {
        var orderId = context.Message.OrderId;
        logger.LogInformation("Received OrderPlaced event for {OrderId}", orderId);

        var reply = await orderClient.GetOrderByIdAsync(new GetOrderRequest
        {
            OrderId = orderId.ToString()
        });

        var notification = new Notification(
            orderId,
            $"Order confirmed: {reply.Quantity}x {reply.Item} for {reply.Customer}",
            DateTime.UtcNow);

        store.Add(notification);
        logger.LogInformation("Notification created for order {OrderId}: {Message}", orderId, notification.Message);
    }
}
