namespace Erp.Modules.Service;

public sealed class ServiceTicket
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Equipment { get; init; }
    public required string DefectDescription { get; init; }
    public DateTime OpenedAt { get; init; } = DateTime.Now;
}
