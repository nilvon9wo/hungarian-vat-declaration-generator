namespace HungarianVatDeclarationGenerator.Api.Models;

public sealed record VatDeclarationResult
{
    public required IReadOnlyList<VatSummary> SummariesByVatRate { get; init; }
    public required decimal GrandTotalNet { get; init; }
    public required decimal GrandTotalVat { get; init; }
    public required decimal GrandTotalGross { get; init; }
    public required int TotalInvoiceCount { get; init; }

    public static VatDeclarationResult Empty => new()
    {
        SummariesByVatRate = [],
        GrandTotalNet = 0m,
        GrandTotalVat = 0m,
        GrandTotalGross = 0m,
        TotalInvoiceCount = 0
    };
}
