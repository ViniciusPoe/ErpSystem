namespace Erp.Modules.Finance;

public sealed class FinancialEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Description { get; init; }
    public decimal Amount { get; init; }
    public DateOnly DueDate { get; init; }
    public bool IsPaid { get; set; }
}
