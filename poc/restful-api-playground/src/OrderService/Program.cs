using Contracts;
using MassTransit;
using OrderService.Orders;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<OrderStore>();
builder.Services.AddOpenApi();
builder.Services.AddGrpc();

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
    });
});

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();
app.MapGrpcService<OrderGrpcService>();

var orders = app.MapGroup("/orders").WithTags("Orders");

orders.MapGet("/", (OrderStore store) => store.GetAll());

orders.MapGet("/{id:guid}", (Guid id, OrderStore store) =>
    store.GetById(id) is { } order
        ? Results.Ok(order)
        : Results.NotFound());

orders.MapPost("/", async (CreateOrderRequest request, OrderStore store, IPublishEndpoint publish) =>
{
    var order = store.Add(request.Item, request.Quantity, request.Customer);
    await publish.Publish(new OrderPlaced(order.Id, order.CreatedAt));
    return Results.Created($"/orders/{order.Id}", order);
});

app.Run();

public record CreateOrderRequest(string Item, int Quantity, string Customer);
