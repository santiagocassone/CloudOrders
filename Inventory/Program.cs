using Inventory.Application;
using Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddOptions<ServiceBusOptions>()
    .Bind(builder.Configuration.GetSection(ServiceBusOptions.SectionName))
    .Validate(options => options.UseInMemory || !string.IsNullOrWhiteSpace(options.ConnectionString),
        "ServiceBus:ConnectionString is required when UseInMemory is false.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.StockResultsQueueName),
        "ServiceBus:StockResultsQueueName is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.OrderPlacedQueueName),
        "ServiceBus:OrderPlacedQueueName is required.")
    .ValidateOnStart();

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("InventoryDb")));

builder.Services.AddScoped<OrderSubmissionService>();

var useInMemory = builder.Configuration.GetValue<bool?>($"{ServiceBusOptions.SectionName}:UseInMemory") ?? builder.Environment.IsDevelopment();
if (useInMemory)
{
    builder.Services.AddSingleton<IOrderEventsPublisher, InMemoryOrderEventsPublisher>();
}
else
{
    builder.Services.AddSingleton<IServiceBusMessageSender, ServiceBusMessageSender>();
    builder.Services.AddSingleton<IOrderEventsPublisher, ServiceBusOrderEventsPublisher>();
    builder.Services.AddHostedService<ServiceBusOrderPlacedConsumer>();
}

var app = builder.Build();
await InventoryDbInitializer.SeedAsync(app);

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.MapHealthChecks("/health");

app.MapGet("/api/inventory/{productId:guid}/stock", async (Guid productId, OrderSubmissionService service, CancellationToken cancellationToken) =>
{
    var stock = await service.GetStockAsync(productId, cancellationToken);
    return Results.Ok(stock);
});

app.MapPost("/api/inventory/reserve", async (ReserveStockRequest request, OrderSubmissionService service, CancellationToken cancellationToken) =>
{
    if (request.Quantity <= 0)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(request.Quantity)] = ["Quantity must be greater than zero."]
        });
    }

    var result = await service.ReserveAsync(request.OrderId, request.ProductId, request.Quantity, cancellationToken);
    if (!result.Reserved && result.RejectedEvent is not null)
    {
        return Results.BadRequest(result.RejectedEvent);
    }

    return Results.Ok(result.ReservedEvent);
});

app.Run();

public partial class Program;

public sealed record ReserveStockRequest(Guid OrderId, Guid ProductId, int Quantity);
