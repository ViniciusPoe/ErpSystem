using Erp.Domain;

namespace Erp.Application;

public sealed class ModuleOverview
{
    public required ErpModule Module { get; init; }
    public required string Title { get; init; }
    public required string Objective { get; init; }
    public required IReadOnlyList<string> Backlog { get; init; }
}
