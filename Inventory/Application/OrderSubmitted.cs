namespace Inventory.Application;

public sealed class InventoryItem
{
    public Guid ProductId { get; set; }
    public int AvailableQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public DateTime UpdatedAt { get; set; }
}
