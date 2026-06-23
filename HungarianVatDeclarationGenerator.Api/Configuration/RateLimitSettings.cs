using System.ComponentModel.DataAnnotations;

namespace HungarianVatDeclarationGenerator.Api.Configuration;

/// <summary>
/// Configuration settings for API rate limiting.
/// </summary>
public sealed class RateLimitSettings
{
    public const string SectionName = "RateLimit";

    /// <summary>
    /// Maximum number of upload requests per IP address within the upload period.
    /// Default: 10 requests.
    /// </summary>
    [Required]
    [Range(1, 1000)]
    public required int UploadLimitCount { get; init; } = 10;

    /// <summary>
    /// Time period for upload rate limit in minutes.
    /// Default: 1 minute.
    /// </summary>
    [Required]
    [Range(1, 60)]
    public required int UploadPeriodMinutes { get; init; } = 1;

    /// <summary>
    /// Maximum number of total requests per IP address within the global period.
    /// Default: 100 requests.
    /// </summary>
    [Required]
    [Range(1, 10000)]
    public required int GlobalLimitCount { get; init; } = 100;

    /// <summary>
    /// Time period for global rate limit in hours.
    /// Default: 1 hour.
    /// </summary>
    [Required]
    [Range(1, 24)]
    public required int GlobalPeriodHours { get; init; } = 1;
}
