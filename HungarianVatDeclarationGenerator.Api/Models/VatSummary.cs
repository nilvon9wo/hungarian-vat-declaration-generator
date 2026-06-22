namespace HungarianVatDeclarationGenerator.Api.Models;

public sealed record VatSummary
{
    public required int VatRate { get; init; }
    public required decimal TotalNetAmount { get; init; }
    public required decimal TotalVatAmount { get; init; }
    public required decimal TotalGrossAmount { get; init; }
    public required int InvoiceCount { get; init; }
}
