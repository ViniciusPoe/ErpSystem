namespace Erp.Modules.Inventory;

public sealed class StockItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Sku { get; init; }
    public required string Description { get; init; }
    public decimal Quantity { get; set; }
}
