namespace Erp.Modules.Sales;

public sealed class Customer
{
    public int Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string Document { get; init; }
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

    public override string ToString() => $"{Code} - {Name}";
}
