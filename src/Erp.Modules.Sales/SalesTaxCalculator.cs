namespace Erp.Modules.Sales;

public static class SalesTaxCalculator
{
    public static SalesTotals Calculate(IReadOnlyList<SalesItemDraft> items, decimal discountAmount, decimal freightAmount)
    {
        var subtotal = items.Sum(i => i.Quantity * i.UnitPrice);
        var taxAmount = 0m; // Temporariamente sem regra fiscal.
        var total = subtotal + taxAmount + freightAmount - discountAmount;

        return new SalesTotals
        {
            Subtotal = decimal.Round(subtotal, 2),
            TaxAmount = decimal.Round(taxAmount, 2),
            DiscountAmount = decimal.Round(discountAmount, 2),
            FreightAmount = decimal.Round(freightAmount, 2),
            GrandTotal = decimal.Round(total, 2)
        };
    }
}
