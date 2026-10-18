namespace Erp.Modules.Sales;

public sealed class QuoteSummary
{
    public int Id { get; init; }
    public int Number { get; init; }
    public required string CustomerName { get; init; }
    public decimal TotalAmount { get; init; }
    public required string Status { get; init; }
    public DateTime CreatedAt { get; init; }
}
