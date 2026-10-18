namespace Erp.Modules.Sales;

public sealed class OrderSummary
{
    public int Id { get; init; }
    public int Number { get; init; }
    public required string CustomerName { get; init; }
    public decimal TotalAmount { get; init; }
    public required string PaymentTerm { get; init; }
    public required string Status { get; init; }
    public string FreightType { get; init; } = "CIF";
    public required string SourceType { get; init; }
    public int? SourceQuoteId { get; init; }
    public int? SourceQuoteNumber { get; init; }
    public DateTime CreatedAt { get; init; }
}
