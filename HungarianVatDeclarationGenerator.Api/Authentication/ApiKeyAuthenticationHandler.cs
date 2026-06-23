using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace HungarianVatDeclarationGenerator.Api.Authentication;

public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder
) : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    private readonly ILogger<ApiKeyAuthenticationHandler> _logger = logger.CreateLogger<ApiKeyAuthenticationHandler>();

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string headerName = Options.HeaderName;

        if (!Request.Headers.TryGetValue(headerName, out var apiKeyHeaderValues))
        {
            _logger.LogWarning("API key header '{HeaderName}' not found in request to {Path}", 
                headerName, Request.Path);
            return Task.FromResult(AuthenticateResult.Fail("Missing API key header"));
        }

        string providedApiKey = apiKeyHeaderValues.FirstOrDefault() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            _logger.LogWarning("API key header '{HeaderName}' is present but empty in request to {Path}", 
                headerName, Request.Path);
            return Task.FromResult(AuthenticateResult.Fail("API key is empty"));
        }

        if (!IsValidApiKey(providedApiKey))
        {
            _logger.LogWarning(
                "Invalid API key provided in request to {Path}. Expected length: {ExpectedLength}, Provided length: {ProvidedLength}",
                Request.Path,
                Options.ValidKey.Length,
                providedApiKey.Length);
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
        }

        _logger.LogInformation("API key authentication successful for request to {Path}", Request.Path);
        return Task.FromResult(CreateAuthenticationTicket());
    }

    private bool IsValidApiKey(string providedKey)
    {
        string expectedKey = Options.ValidKey;
        return providedKey.Equals(expectedKey, StringComparison.Ordinal);
    }

    private static AuthenticateResult CreateAuthenticationTicket()
    {
        Claim[] claims =
        [
            new Claim(ClaimTypes.Name, "ApiKeyUser"),
            new Claim(ClaimTypes.NameIdentifier, "api-key-user")
        ];

        ClaimsIdentity identity = new(claims, ApiKeyAuthenticationOptions.AuthenticationType);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, ApiKeyAuthenticationOptions.Scheme);

        return AuthenticateResult.Success(ticket);
    }
}
