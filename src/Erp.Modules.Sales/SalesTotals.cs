namespace Erp.Modules.Sales;

public sealed class SalesTotals
{
    public decimal Subtotal { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal FreightAmount { get; init; }
    public decimal GrandTotal { get; init; }
}
