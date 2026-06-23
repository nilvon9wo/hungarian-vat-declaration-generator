using System.ComponentModel.DataAnnotations;

namespace HungarianVatDeclarationGenerator.Api.Configuration;

/// <summary>
/// Configuration settings for VAT calculation behavior.
/// </summary>
public sealed class VatCalculationSettings
{
    public const string SectionName = "VatCalculation";

    /// <summary>
    /// Number of decimal places to round calculated VAT amounts to.
    /// Default: 2 (standard for currency).
    /// </summary>
    [Required]
    [Range(0, 10)]
    public required int DecimalPlaces { get; init; } = 2;
}
