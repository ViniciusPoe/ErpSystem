namespace Erp.Modules.Sales;

public sealed class Municipality
{
    public int Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string State { get; init; }
    public string Country { get; init; } = "Brasil";

    public override string ToString() => $"{Code} - {Name} - {State}";
}
