using System.ComponentModel.DataAnnotations;

namespace HungarianVatDeclarationGenerator.Api.Configuration;

/// <summary>
/// Configuration settings for file content validation including binary format rejection.
/// </summary>
public sealed class FileValidationSettings
{
    public const string SectionName = "FileValidation";

    /// <summary>
    /// Enable rejection of known binary file formats (PDF, ZIP, etc.).
    /// Default: true.
    /// </summary>
    [Required]
    public required bool RejectBinaryFormats { get; init; } = true;

    /// <summary>
    /// List of rejected file format magic numbers (hex byte sequences).
    /// Each entry is a hex string representing the file signature to reject.
    /// </summary>
    [Required]
    public required RejectedFormat[] RejectedFormats { get; init; }
}
