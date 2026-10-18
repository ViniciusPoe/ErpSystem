namespace Erp.Modules.Sales;

public sealed class QuoteData
{
    public int Id { get; init; }
    public int Number { get; init; }
    public int CustomerId { get; init; }
    public int? CarrierId { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal FreightAmount { get; init; }
    public required string Status { get; init; }
    public required IReadOnlyList<SalesItemDraft> Items { get; init; }
}
