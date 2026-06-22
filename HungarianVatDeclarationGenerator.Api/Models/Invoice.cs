using System.ComponentModel.DataAnnotations;

namespace HungarianVatDeclarationGenerator.Api.Models;

public sealed record Invoice
{
    [Required]
    public required string InvoiceNumber { get; init; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public required decimal NetAmount { get; init; }

    [Required]
    public required int VatRate { get; init; }

    public decimal VatAmount
        => NetAmount * VatRate / 100m;

    public decimal GrossAmount
        => NetAmount + VatAmount;
}
