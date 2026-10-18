namespace Erp.Modules.Invoicing;

public sealed class InvoicingService
{
    public IReadOnlyList<string> GetInitialBacklog()
    {
        return
        [
            "Emissao de NF-e em modo rascunho (sem SEFAZ)",
            "Calculo de impostos e totais por item",
            "Geracao de DANFE para impressao local"
        ];
    }
}
