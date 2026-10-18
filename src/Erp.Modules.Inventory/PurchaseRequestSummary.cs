namespace Erp.Modules.Inventory;

public sealed class PurchaseRequestSummary
{
    public int Id { get; init; }
    public required string ProductSku { get; init; }
    public decimal RequestedQuantity { get; init; }
    public required string Status { get; init; }
    public string? SupplierName { get; init; }
    public decimal? ApprovedQuantity { get; init; }
    public decimal? ApprovedUnitPrice { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
}
