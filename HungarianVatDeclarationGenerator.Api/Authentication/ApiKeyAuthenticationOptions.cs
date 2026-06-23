using Microsoft.AspNetCore.Authentication;

namespace HungarianVatDeclarationGenerator.Api.Authentication;

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string Scheme = "ApiKey";
    public const string AuthenticationType = "ApiKey";

    public string HeaderName { get; set; } = "X-API-Key";
    public string ValidKey { get; set; } = string.Empty;
}
