namespace Erp.Modules.Sales;

public sealed class SalesService
{
    public IReadOnlyList<string> GetInitialBacklog()
    {
        return
        [
            "Cadastro de clientes, produtos e transportadoras",
            "Orcamentos com itens e calculo tributario basico",
            "Pedidos com reaproveitamento do fluxo comercial"
        ];
    }
}
