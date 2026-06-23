namespace HungarianVatDeclarationGenerator.Api.Models;

/// <summary>
/// Client configuration settings exposed to the frontend.
/// </summary>
public sealed record ClientConfig
{
    /// <summary>
    /// Maximum file size in bytes that can be uploaded.
    /// </summary>
    public required long MaxFileSizeBytes { get; init; }

    /// <summary>
    /// Allowed file extensions (e.g., [".csv"]).
    /// </summary>
    public required string[] AllowedExtensions { get; init; }

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public required int RequestTimeoutSeconds { get; init; }
}
