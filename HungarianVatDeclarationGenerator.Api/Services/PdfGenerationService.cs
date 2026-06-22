using HungarianVatDeclarationGenerator.Api.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HungarianVatDeclarationGenerator.Api.Services;

public sealed class PdfGenerationService : IPdfGenerationService
{
    public byte[] GeneratePdf(VatDeclarationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page);
                page.Content().Column(column =>
                {
                    AddTitle(column);
                    AddCategoryTable(column, result);
                    AddGrandTotals(column, result);
                });
            });
        }).GeneratePdf();
    }

    private static void ConfigurePage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(2, Unit.Centimetre);
        page.DefaultTextStyle(x => x.FontSize(11));
    }

    private static void AddTitle(ColumnDescriptor column) => column.Item()
            .PaddingBottom(20)
            .Text("VAT Declaration Summary")
            .FontSize(20)
            .Bold();

    private static void AddCategoryTable(ColumnDescriptor column, VatDeclarationResult result)
        => column.Item().Table(table =>
            {
                DefineTableColumns(table);
                AddTableHeader(table);
                AddTableRows(table, result);
            });

    private static void DefineTableColumns(TableDescriptor table)
        => table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);
                columns.RelativeColumn(3);
                columns.RelativeColumn(3);
                columns.RelativeColumn(3);
            });

    private static void AddTableHeader(TableDescriptor table)
        => table.Header(header =>
        {
            CreateHeader(header, "VAT Rate");
            CreateHeader(header, "Net Total");
            CreateHeader(header, "VAT Total");
            CreateHeader(header, "Gross Total");
        });

    private static void CreateHeader(TableCellDescriptor header, string headerText)
        => header.Cell()
            .Background(Colors.Grey.Lighten2)
            .Padding(8)
            .Text(headerText)
            .Bold();

    private static void AddTableRows(TableDescriptor table, VatDeclarationResult result)
    {
        foreach (VatSummary summary in result.SummariesByVatRate)
        {
            AddDataCell(table, $"{summary.VatRate}%");
            AddDataCell(table, FormatCurrency(summary.TotalNetAmount));
            AddDataCell(table, FormatCurrency(summary.TotalVatAmount));
            AddDataCell(table, FormatCurrency(summary.TotalGrossAmount));
        }
    }

    private static void AddDataCell(TableDescriptor table, string text) => table.Cell()
            .Border(1)
            .BorderColor(Colors.Grey.Lighten1)
            .Padding(8)
            .Text(text);

    private static void AddGrandTotals(ColumnDescriptor column, VatDeclarationResult result)
        => column.Item()
            .PaddingTop(20)
            .Border(2)
            .BorderColor(Colors.Grey.Medium)
            .Padding(15)
            .Column(totalsColumn =>
            {
                AddTotalLine(totalsColumn, "Grand Total Net:", result.GrandTotalNet);
                AddTotalLine(totalsColumn, "Grand Total VAT:", result.GrandTotalVat);
                AddTotalLine(totalsColumn, "Grand Total Gross:", result.GrandTotalGross);
            });

    private static void AddTotalLine(ColumnDescriptor column, string label, decimal amount)
        => column.Item()
            .Row(row =>
            {
                row.RelativeItem()
                    .Text(label)
                    .Bold();
                row.RelativeItem()
                    .AlignRight()
                    .Text(FormatCurrency(amount))
                    .Bold()
                    .FontSize(12);
            });

    private static string FormatCurrency(decimal amount)
        => amount.ToString("N2");
}
