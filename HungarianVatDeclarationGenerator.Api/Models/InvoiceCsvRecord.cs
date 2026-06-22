namespace HungarianVatDeclarationGenerator.Api.Models;

public sealed class InvoiceCsvRecord
{
    public required string InvoiceNumber { get; init; }
    public required decimal NetAmount { get; init; }
    public required int VatRate { get; init; }
}
