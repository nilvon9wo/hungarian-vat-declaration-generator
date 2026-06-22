namespace HungarianVatDeclarationGenerator.Api.Models;

public sealed record Invoice
{
    public required string InvoiceNumber { get; init; }
    public required decimal NetAmount { get; init; }
    public required int VatRate { get; init; }

    public decimal VatAmount
        => NetAmount * VatRate / 100m;

    public decimal GrossAmount
        => NetAmount + VatAmount;
}
