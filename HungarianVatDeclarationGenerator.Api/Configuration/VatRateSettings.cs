using System.ComponentModel.DataAnnotations;

namespace HungarianVatDeclarationGenerator.Api.Configuration;

/// <summary>
/// Configuration settings for VAT rate validation.
/// </summary>
public sealed class VatRateSettings
{
    public const string SectionName = "VatRates";

    /// <summary>
    /// Supported VAT rates in percentage (e.g., 5, 18, 27 for Hungary).
    /// </summary>
    [Required]
    [MinLength(1)]
    public required int[] SupportedRates { get; init; }

    /// <summary>
    /// Validates if a given VAT rate is supported.
    /// </summary>
    public bool IsValid(int rate) => SupportedRates.Contains(rate);

    /// <summary>
    /// Gets a comma-separated list of supported rates for error messages.
    /// </summary>
    public string GetSupportedRatesDisplay() => string.Join(", ", SupportedRates);
}
