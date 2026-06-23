using System.ComponentModel.DataAnnotations;

namespace HungarianVatDeclarationGenerator.Api.Configuration;

/// <summary>
/// Configuration settings for API key authentication.
/// 
/// ⚠️ DEMO/CHALLENGE PURPOSES ONLY ⚠️
/// This simple API key approach is for demonstration only.
/// In production:
/// - Use JWT tokens with proper authentication server (Azure AD, Auth0, IdentityServer)
/// - Never store credentials in appsettings.json or source control
/// - Use Azure Key Vault, AWS Secrets Manager, or similar secret management
/// - Implement proper token rotation and expiration
/// </summary>
public sealed class ApiKeySettings
{
    public const string SectionName = "ApiKey";

    /// <summary>
    /// API key header name (default: X-API-Key).
    /// </summary>
    [Required]
    public string HeaderName { get; init; } = "X-API-Key";

    /// <summary>
    /// Valid API key value.
    /// ⚠️ FOR DEMO ONLY - never store real credentials in config files!
    /// </summary>
    [Required]
    [MinLength(16)]
    public string ValidKey { get; init; } = string.Empty;
}
