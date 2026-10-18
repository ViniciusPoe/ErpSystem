namespace Erp.Modules.Sales;

public sealed class Product
{
    public int Id { get; init; }
    public required string Sku { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public decimal Quantity { get; init; }
    public bool IsActive { get; init; } = true;

    // New fields
    public string? Code { get; init; }
    public string? Description { get; init; }
    public string? Barcode { get; init; }
    public decimal CostPrice { get; init; }
    public string? ImagePath { get; init; }
    public string? Color { get; init; }
    public int? SubClassificationId { get; init; }
    public string? Ncm { get; init; }
    public decimal Weight { get; init; }
    public decimal Width { get; init; }
    public decimal Height { get; init; }
    public decimal Depth { get; init; }
    public string? Voltage { get; init; }
    public int? BrandId { get; init; }

    // Calculated
    public decimal ProfitMargin => CostPrice > 0 ? Math.Round((UnitPrice - CostPrice) / CostPrice * 100, 2) : 0;

    // Display helpers (used by other views in ComboBox)
    public string? ClassificationDisplay { get; init; }
    public string? SubClassificationDisplay { get; init; }
    public string? BrandDisplay { get; init; }

    public override string ToString() => $"{Sku} | Estoque: {Quantity:N2}";
}
