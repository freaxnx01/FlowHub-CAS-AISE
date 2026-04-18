using FluentAssertions;
using OrderService.Orders;
using Xunit;

namespace OrderService.Tests;

public class OrderStoreTests
{
    [Fact]
    public void Add_NewOrder_AssignsIdAndTimestamp()
    {
        var store = new OrderStore();

        var order = store.Add("Widget", 3, "Alice");

        order.Id.Should().NotBe(Guid.Empty);
        order.Item.Should().Be("Widget");
        order.Quantity.Should().Be(3);
        order.Customer.Should().Be("Alice");
        order.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GetById_ExistingOrder_ReturnsOrder()
    {
        var store = new OrderStore();
        var added = store.Add("Widget", 3, "Alice");

        var found = store.GetById(added.Id);

        found.Should().Be(added);
    }

    [Fact]
    public void GetById_UnknownId_ReturnsNull()
    {
        var store = new OrderStore();

        var found = store.GetById(Guid.NewGuid());

        found.Should().BeNull();
    }

    [Fact]
    public void GetAll_MultipleOrders_ReturnsAll()
    {
        var store = new OrderStore();
        store.Add("A", 1, "X");
        store.Add("B", 2, "Y");
        store.Add("C", 3, "Z");

        var all = store.GetAll();

        all.Should().HaveCount(3);
        all.Select(o => o.Item).Should().BeEquivalentTo(["A", "B", "C"]);
    }
}
