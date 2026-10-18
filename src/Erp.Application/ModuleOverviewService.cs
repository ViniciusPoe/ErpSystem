using Erp.Domain;
using Erp.Modules.Finance;
using Erp.Modules.Inventory;
using Erp.Modules.Invoicing;
using Erp.Modules.Sales;
using Erp.Modules.Service;

namespace Erp.Application;

public sealed class ModuleOverviewService
{
    public IReadOnlyList<ModuleOverview> BuildInitialPlan()
    {
        var sales = new SalesService();
        var invoicing = new InvoicingService();
        var technical = new TechnicalServiceManager();
        var finance = new FinanceService();
        var inventory = new InventoryService();

        return
        [
            new ModuleOverview
            {
                Module = ErpModule.Sales,
                Title = "Vendas",
                Objective = "Controlar ciclo comercial do orçamento até o pedido.",
                Backlog = sales.GetInitialBacklog()
            },
            new ModuleOverview
            {
                Module = ErpModule.Invoicing,
                Title = "Notas Fiscais",
                Objective = "Preparar emissao de NF-e sem comunicacao com SEFAZ nesta fase.",
                Backlog = invoicing.GetInitialBacklog()
            },
            new ModuleOverview
            {
                Module = ErpModule.TechnicalService,
                Title = "Assistencia Tecnica",
                Objective = "Gerenciar ordens de servico e historico tecnico.",
                Backlog = technical.GetInitialBacklog()
            },
            new ModuleOverview
            {
                Module = ErpModule.Finance,
                Title = "Financeiro",
                Objective = "Consolidar titulos, caixa e previsao financeira.",
                Backlog = finance.GetInitialBacklog()
            },
            new ModuleOverview
            {
                Module = ErpModule.Inventory,
                Title = "Estoque",
                Objective = "Controlar saldo, movimentacoes e inventario de itens.",
                Backlog = inventory.GetInitialBacklog()
            }
        ];
    }
}
