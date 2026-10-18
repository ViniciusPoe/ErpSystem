namespace Erp.Modules.Sales;

public static class SalesStatuses
{
    public const string QuoteOpen = "ABERTO";
    public const string QuoteApproved = "APROVADO";
    public const string QuoteRejected = "REPROVADO";

    public const string OrderOpen = "ABERTO";
    public const string OrderReadyForInvoice = "LIBERADO_NF";
    public const string OrderInvoiced = "FATURADO";
}
