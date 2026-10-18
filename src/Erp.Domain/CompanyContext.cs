namespace Erp.Domain;

public sealed class CompanyContext
{
    public required string TradeName { get; init; }
    public required string TaxId { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
}
