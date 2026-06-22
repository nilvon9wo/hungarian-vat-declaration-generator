using System.ComponentModel.DataAnnotations;

namespace HungarianVatDeclarationGenerator.Api.Models;

public sealed record VatSummary
{
    [Required]
    public required int VatRate { get; init; }

    [Required]
    public required decimal TotalNetAmount { get; init; }

    [Required]
    public required decimal TotalVatAmount { get; init; }

    [Required]
    public required decimal TotalGrossAmount { get; init; }

    [Required]
    [Range(0, int.MaxValue)]
    public required int InvoiceCount { get; init; }
}
