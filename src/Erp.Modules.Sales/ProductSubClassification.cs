namespace Erp.Modules.Sales;

public sealed class ProductSubClassification
{
    public int Id { get; init; }
    public int ClassificationId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }

    public override string ToString() => $"{Code} - {Name}";
}
