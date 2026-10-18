namespace Erp.Modules.Inventory;

public sealed class Supplier
{
    public int Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string TaxId { get; init; }
    public bool IsActive { get; init; } = true;
    public string? ContactName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Street { get; init; }
    public string? Number { get; init; }
    public string? District { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? ZipCode { get; init; }
    public decimal MinimumOrder { get; init; }
    public string? Segment { get; init; }

    public override string ToString() => $"{Code} - {Name}";
}
