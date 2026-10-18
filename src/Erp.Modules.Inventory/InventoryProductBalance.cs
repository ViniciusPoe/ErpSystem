namespace Erp.Modules.Inventory;

public sealed class InventoryProductBalance
{
    public int ProductId { get; init; }
    public required string Sku { get; init; }
    public required string Name { get; init; }
    public decimal Quantity { get; init; }
}
