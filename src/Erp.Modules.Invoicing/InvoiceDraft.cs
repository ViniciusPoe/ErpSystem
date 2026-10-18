namespace Erp.Modules.Invoicing;

public sealed class InvoiceDraft
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string CustomerName { get; init; }
    public required string OperationNature { get; init; }
    public decimal ProductTotal { get; init; }
    public bool IsTransmittedToSefaz => false;
}
