namespace Erp.Modules.Finance;

public sealed class RecurringPayableSummary
{
    public int Id { get; init; }
    public required string Description { get; init; }
    public int? SupplierId { get; init; }
    public string? SupplierName { get; init; }
    public decimal MonthlyAmount { get; init; }
    public DateTime StartDate { get; init; }
    public bool IsActive { get; init; }
}
