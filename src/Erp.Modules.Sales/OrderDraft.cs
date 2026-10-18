namespace Erp.Modules.Sales;

public sealed class OrderDraft
{
    public int CustomerId { get; init; }
    public int? CarrierId { get; init; }
    public required IReadOnlyList<SalesItemDraft> Items { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal FreightAmount { get; init; }
    public string FreightType { get; init; } = "CIF";
    public required string PaymentTerm { get; init; }
}
