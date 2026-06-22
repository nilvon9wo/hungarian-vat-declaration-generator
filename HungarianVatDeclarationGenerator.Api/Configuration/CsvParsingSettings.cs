using System.ComponentModel.DataAnnotations;

namespace HungarianVatDeclarationGenerator.Api.Configuration;

/// <summary>
/// Configuration settings for CSV parsing and validation.
/// </summary>
public sealed class CsvParsingSettings
{
    public const string SectionName = "CsvParsing";

    /// <summary>
    /// Maximum number of rows to process (default: 10,000).
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public required int MaxRowsToProcess { get; init; } = 10000;

    /// <summary>
    /// Maximum length for invoice number field (default: 100 characters).
    /// </summary>
    [Required]
    [Range(1, 1000)]
    public required int MaxInvoiceNumberLength { get; init; } = 100;

    /// <summary>
    /// Maximum length for any CSV field (default: 500 characters).
    /// </summary>
    [Required]
    [Range(1, 10000)]
    public required int MaxFieldLength { get; init; } = 500;

    /// <summary>
    /// Maximum number of validation errors to include in error messages (default: 5).
    /// </summary>
    [Required]
    [Range(1, 100)]
    public required int MaxErrorsToDisplay { get; init; } = 5;
}
