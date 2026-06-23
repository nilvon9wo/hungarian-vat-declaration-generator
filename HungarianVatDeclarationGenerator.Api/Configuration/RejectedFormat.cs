using System.ComponentModel.DataAnnotations;

namespace HungarianVatDeclarationGenerator.Api.Configuration;

/// <summary>
/// Represents a binary file format to reject based on magic number.
/// </summary>
public sealed class RejectedFormat
{
    /// <summary>
    /// Human-readable name of the format (e.g., "PDF", "ZIP").
    /// </summary>
    [Required]
    public required string Name { get; init; }

    /// <summary>
    /// Hex-encoded magic number bytes (e.g., "25504446" for PDF "%PDF").
    /// </summary>
    [Required]
    [MinLength(2)]
    public required string MagicNumberHex { get; init; }

    /// <summary>
    /// User-facing error message when this format is detected.
    /// </summary>
    [Required]
    public required string ErrorMessage { get; init; }
}
