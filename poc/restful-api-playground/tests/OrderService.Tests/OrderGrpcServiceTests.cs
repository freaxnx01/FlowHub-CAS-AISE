using FluentAssertions;
using Grpc.Core;
using OrderService.Grpc;
using OrderService.Orders;
using Xunit;

namespace OrderService.Tests;

public class OrderGrpcServiceTests
{
    [Fact]
    public async Task GetOrderById_ExistingOrder_ReturnsReply()
    {
        var store = new OrderStore();
        var order = store.Add("Widget", 3, "Alice");
        var service = new OrderGrpcService(store);

        var reply = await service.GetOrderById(
            new GetOrderRequest { OrderId = order.Id.ToString() },
            context: null!);

        reply.OrderId.Should().Be(order.Id.ToString());
        reply.Item.Should().Be("Widget");
        reply.Quantity.Should().Be(3);
        reply.Customer.Should().Be("Alice");
    }

    [Fact]
    public async Task GetOrderById_UnknownId_ThrowsNotFound()
    {
        var service = new OrderGrpcService(new OrderStore());

        var act = async () => await service.GetOrderById(
            new GetOrderRequest { OrderId = Guid.NewGuid().ToString() },
            context: null!);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrderById_InvalidGuid_ThrowsInvalidArgument()
    {
        var service = new OrderGrpcService(new OrderStore());

        var act = async () => await service.GetOrderById(
            new GetOrderRequest { OrderId = "not-a-guid" },
            context: null!);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }
}
