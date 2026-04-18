using MassTransit;
using NotificationService.Consumers;
using NotificationService.Notifications;
using OrderService.Grpc;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<NotificationStore>();
builder.Services.AddOpenApi();

var grpcAddress = builder.Configuration["OrderService:GrpcAddress"]
    ?? "http://localhost:5003";
builder.Services.AddGrpcClient<OrderGrpc.OrderGrpcClient>(o =>
{
    o.Address = new Uri(grpcAddress);
});

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderPlacedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

var notifications = app.MapGroup("/notifications").WithTags("Notifications");

notifications.MapGet("/", (NotificationStore store) => store.GetAll());

app.Run();
