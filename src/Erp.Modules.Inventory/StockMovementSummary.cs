namespace Erp.Modules.Inventory;

public sealed class StockMovementSummary
{
    public int Id { get; init; }
    public required string MovementType { get; init; }
    public required string ProductSku { get; init; }
    public decimal Quantity { get; init; }
    public required string SourceType { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
}
