namespace Erp.Modules.Service;

public sealed class TechnicalServiceManager
{
    public IReadOnlyList<string> GetInitialBacklog()
    {
        return
        [
            "Abertura de OS com checklist tecnico",
            "Controle de pecas aplicadas",
            "Historico de manutencoes por equipamento"
        ];
    }
}
