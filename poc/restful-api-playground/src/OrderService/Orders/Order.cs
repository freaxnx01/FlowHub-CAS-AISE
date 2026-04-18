namespace OrderService.Orders;

public record Order(Guid Id, string Item, int Quantity, string Customer, DateTime CreatedAt);
