namespace Erp.Modules.Finance;

public sealed class AccountReceivableSummary
{
    public int Id { get; init; }
    public int? OrderId { get; init; }
    public int? InvoiceId { get; init; }
    public required string CustomerName { get; init; }
    public int InstallmentNumber { get; init; }
    public DateTime DueDate { get; init; }
    public decimal Amount { get; init; }
    public required string PaymentTerm { get; init; }
    public required string Status { get; init; }
}
