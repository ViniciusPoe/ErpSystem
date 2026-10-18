namespace Erp.Modules.Sales;

public sealed class InvoiceSummary
{
    public int Id { get; init; }
    public int Number { get; init; }
    public int? OrderNumber { get; init; }
    public required string CustomerName { get; init; }
    public decimal TotalAmount { get; init; }
    public required string SourceType { get; init; }
    public DateTime CreatedAt { get; init; }
}
