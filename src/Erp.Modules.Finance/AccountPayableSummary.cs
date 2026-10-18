namespace Erp.Modules.Finance;

public sealed class AccountPayableSummary
{
    public int Id { get; init; }
    public int? PurchaseId { get; init; }
    public int? SupplierId { get; init; }
    public string? SupplierName { get; init; }
    public required string Description { get; init; }
    public int InstallmentNumber { get; init; }
    public DateTime DueDate { get; init; }
    public decimal Amount { get; init; }
    public required string PaymentTerm { get; init; }
    public required string Status { get; init; }
    public int? RecurringId { get; init; }
}
