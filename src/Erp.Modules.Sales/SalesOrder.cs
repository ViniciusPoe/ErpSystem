namespace Erp.Modules.Sales;

public sealed class SalesOrder
{
    public int Id { get; init; }
    public int Number { get; init; }
    public int CustomerId { get; init; }
    public int? CarrierId { get; init; }
    public DateTime CreatedAt { get; init; }
    public decimal TotalAmount { get; init; }
}
