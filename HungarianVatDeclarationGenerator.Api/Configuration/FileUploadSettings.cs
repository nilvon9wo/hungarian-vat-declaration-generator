using System.ComponentModel.DataAnnotations;

namespace HungarianVatDeclarationGenerator.Api.Configuration;

/// <summary>
/// Configuration settings for file upload and processing limits.
/// </summary>
public sealed class FileUploadSettings
{
    public const string SectionName = "FileUpload";

    /// <summary>
    /// Maximum file size in bytes (default: 5 MB).
    /// </summary>
    [Required]
    public required long MaxFileSizeBytes { get; init; } = 5 * 1024 * 1024;

    /// <summary>
    /// Processing timeout in seconds (default: 30 seconds).
    /// </summary>
    [Required]
    public required int ProcessingTimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Allowed content types for uploaded files.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required string[] AllowedContentTypes { get; init; }

    /// <summary>
    /// Allowed file extensions.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required string[] AllowedExtensions { get; init; }

    /// <summary>
    /// Maximum length for individual form field values in bytes (default: 1 MB).
    /// This protects against excessively long form values.
    /// </summary>
    [Required]
    public required long MaxFormValueLengthBytes { get; init; } = 1024 * 1024;
}
