using System.Diagnostics;
using System.Globalization;
using System.IO;
using Erp.Modules.Inventory;
using Erp.Modules.Sales;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Erp.Desktop.Wpf.Views;

public static class PdfHelper
{
    static PdfHelper()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static void GenerateAndOpenInvoicePdf(InvoiceSummary invoice)
    {
        var path = Path.Combine(Path.GetTempPath(), $"NF_{invoice.Number}_{DateTime.Now:yyyyMMddHHmmss}.pdf");

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Text($"NOTA FISCAL Nº {invoice.Number}")
                    .FontSize(22).Bold().FontColor(Colors.Blue.Darken3);

                page.Content().PaddingTop(20).Column(col =>
                {
                    col.Spacing(8);
                    col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(8).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Cliente:").Bold();
                            c.Item().Text(invoice.CustomerName);
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Pedido vinculado:").Bold();
                            c.Item().Text(invoice.OrderNumber.HasValue ? $"#{invoice.OrderNumber.Value}" : "NF direta");
                        });
                    });

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Data de emissao:").Bold();
                            c.Item().Text(invoice.CreatedAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture));
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Valor total:").Bold();
                            c.Item().Text(invoice.TotalAmount.ToString("C2", CultureInfo.CurrentCulture))
                                .FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                        });
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Gerado em ").FontSize(9).FontColor(Colors.Grey.Medium);
                    text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(9).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf(path);

        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    public static void GenerateAndOpenPurchasePdf(PurchaseSummary purchase)
    {
        var path = Path.Combine(Path.GetTempPath(), $"Compra_{purchase.Id}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
        var total = purchase.Quantity * purchase.UnitPrice;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Text($"COMPRA Nº {purchase.Id}")
                    .FontSize(22).Bold().FontColor(Colors.Green.Darken3);

                page.Content().PaddingTop(20).Column(col =>
                {
                    col.Spacing(8);
                    col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(8).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Fornecedor:").Bold();
                            c.Item().Text(purchase.SupplierName);
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Produto:").Bold();
                            c.Item().Text(purchase.ProductSku);
                        });
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Produto").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Qtd").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Vlr Unit").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Total").Bold();
                        });

                        table.Cell().Padding(4).Text(purchase.ProductSku);
                        table.Cell().Padding(4).Text(purchase.Quantity.ToString("N2", CultureInfo.CurrentCulture));
                        table.Cell().Padding(4).Text(purchase.UnitPrice.ToString("N2", CultureInfo.CurrentCulture));
                        table.Cell().Padding(4).Text(total.ToString("C2", CultureInfo.CurrentCulture)).Bold();
                    });

                    col.Item().AlignRight().Text(text =>
                    {
                        text.Span("Total: ").Bold();
                        text.Span(total.ToString("C2", CultureInfo.CurrentCulture))
                            .FontSize(16).Bold().FontColor(Colors.Green.Darken2);
                    });

                    col.Item().Text($"Data da entrega: {DateTime.Now:dd/MM/yyyy}").FontColor(Colors.Grey.Medium);
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Gerado em ").FontSize(9).FontColor(Colors.Grey.Medium);
                    text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(9).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf(path);

        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}
