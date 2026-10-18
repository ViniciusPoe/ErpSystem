namespace Erp.Modules.Sales;

public sealed class QuoteDraft
{
    public int CustomerId { get; init; }
    public int? CarrierId { get; init; }
    public required IReadOnlyList<SalesItemDraft> Items { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal FreightAmount { get; init; }
}
