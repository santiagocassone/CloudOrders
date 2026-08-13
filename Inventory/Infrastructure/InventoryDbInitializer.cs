using Inventory.Application;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure;

public static class InventoryDbInitializer
{
    public static async Task SeedAsync(WebApplication app, CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        await db.Database.EnsureCreatedAsync(cancellationToken);

        if (await db.InventoryItems.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        db.InventoryItems.AddRange(
            new InventoryItem
            {
                ProductId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                AvailableQuantity = 10,
                ReservedQuantity = 0,
                UpdatedAt = now
            },
            new InventoryItem
            {
                ProductId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                AvailableQuantity = 25,
                ReservedQuantity = 0,
                UpdatedAt = now
            },
            new InventoryItem
            {
                ProductId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                AvailableQuantity = 50,
                ReservedQuantity = 0,
                UpdatedAt = now
            });

        await db.SaveChangesAsync(cancellationToken);
    }
}
