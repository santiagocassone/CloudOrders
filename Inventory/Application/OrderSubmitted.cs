namespace Inventory.Application;

public sealed record InventoryItem(Guid ProductId, int AvailableQuantity, int ReservedQuantity, DateTime UpdatedAt);
