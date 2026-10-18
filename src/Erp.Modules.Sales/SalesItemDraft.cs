namespace Erp.Modules.Sales;

public sealed class SalesItemDraft
{
    public int ProductId { get; init; }
    public required string Description { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}
