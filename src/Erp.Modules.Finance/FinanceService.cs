namespace Erp.Modules.Finance;

public sealed class FinanceService
{
    public IReadOnlyList<string> GetInitialBacklog()
    {
        return
        [
            "Contas a pagar e a receber",
            "Fluxo de caixa diario",
            "Conciliacao de recebimentos"
        ];
    }
}
