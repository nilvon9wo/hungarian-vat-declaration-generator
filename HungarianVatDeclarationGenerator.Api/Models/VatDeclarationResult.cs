using System.ComponentModel.DataAnnotations;

namespace HungarianVatDeclarationGenerator.Api.Models;

public sealed record VatDeclarationResult
{
    [Required]
    public required IReadOnlyList<VatSummary> SummariesByVatRate { get; init; }

    [Required]
    public required decimal GrandTotalNet { get; init; }

    [Required]
    public required decimal GrandTotalVat { get; init; }

    [Required]
    public required decimal GrandTotalGross { get; init; }

    [Required]
    [Range(0, int.MaxValue)]
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
