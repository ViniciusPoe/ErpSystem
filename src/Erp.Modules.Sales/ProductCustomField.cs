namespace Erp.Modules.Sales;

public sealed class ProductCustomField
{
    public int Id { get; init; }
    public int SubClassificationId { get; init; }
    public required string FieldName { get; init; }
    public bool IsRequired { get; init; }

    public override string ToString() => IsRequired ? $"{FieldName} *" : FieldName;
}
