using Grpc.Core;
using OrderService.Grpc;

namespace OrderService.Orders;

public class OrderGrpcService(OrderStore store) : OrderGrpc.OrderGrpcBase
{
    public override Task<OrderReply> GetOrderById(GetOrderRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.OrderId, out var id))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid order ID"));

        var order = store.GetById(id)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Order {request.OrderId} not found"));

        return Task.FromResult(new OrderReply
        {
            OrderId = order.Id.ToString(),
            Item = order.Item,
            Quantity = order.Quantity,
            Customer = order.Customer,
            CreatedAt = order.CreatedAt.ToString("O")
        });
    }
}
