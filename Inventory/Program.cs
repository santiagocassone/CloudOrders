using Inventory.Application;
using Inventory.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.Configure<ServiceBusOptions>(builder.Configuration.GetSection(ServiceBusOptions.SectionName));
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
}

var app = builder.Build();
app.UseExceptionHandler();
app.UseHttpsRedirection();

app.MapHealthChecks("/health");

app.MapGet("/api/inventory/{productId:guid}/stock", async (Guid productId, OrderSubmissionService service, CancellationToken cancellationToken) =>
{
    var availableQuantity = await service.GetAvailableStockAsync(productId, cancellationToken);
    return Results.Ok(new
    {
        ProductId = productId,
        AvailableQuantity = availableQuantity,
        ReservedQuantity = 0,
        UpdatedAt = DateTime.UtcNow
    });
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
