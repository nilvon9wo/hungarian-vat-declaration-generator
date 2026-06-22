using System.ComponentModel.DataAnnotations;

namespace HungarianVatDeclarationGenerator.Api.Models;

public sealed class InvoiceCsvRecord
{
    [Required]
    public required string InvoiceNumber { get; init; }

    [Required]
    public required decimal NetAmount { get; init; }

    [Required]
    public required int VatRate { get; init; }
}
