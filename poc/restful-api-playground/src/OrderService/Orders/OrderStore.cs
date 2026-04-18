using System.Collections.Concurrent;

namespace OrderService.Orders;

public class OrderStore
{
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();

    public Order Add(string item, int quantity, string customer)
    {
        var order = new Order(Guid.NewGuid(), item, quantity, customer, DateTime.UtcNow);
        _orders[order.Id] = order;
        return order;
    }

    public Order? GetById(Guid id) =>
        _orders.GetValueOrDefault(id);

    public IReadOnlyList<Order> GetAll() =>
        _orders.Values.ToList();
}
