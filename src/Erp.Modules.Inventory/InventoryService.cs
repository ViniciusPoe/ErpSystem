namespace Erp.Modules.Inventory;

public sealed class InventoryService
{
    public IReadOnlyList<string> GetInitialBacklog()
    {
        return
        [
            "Cadastro de produtos e unidades",
            "Movimentacoes de entrada e saida",
            "Inventario e ajuste de saldo"
        ];
    }
}
