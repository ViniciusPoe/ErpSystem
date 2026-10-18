namespace Erp.Modules.Inventory;

public sealed class PurchaseSummary
{
    public int Id { get; init; }
    public int? RequestId { get; init; }
    public required string SourceType { get; init; }
    public required string SupplierName { get; init; }
    public required string ProductSku { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public required string Status { get; init; }
    public DateTime CreatedAt { get; init; }
}
