using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HungarianVatDeclarationGenerator.Api.Swagger;

public sealed class ApiKeyDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        OpenApiSecurityScheme securityScheme = new()
        {
            Name = "X-API-Key",
            Description = "API Key Authentication",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey
        };

        swaggerDoc.Components ??= new OpenApiComponents();
        swaggerDoc.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        if (!swaggerDoc.Components.SecuritySchemes.ContainsKey("ApiKey"))
        {
            swaggerDoc.Components.SecuritySchemes.Add("ApiKey", securityScheme);
        }

        OpenApiSecuritySchemeReference securitySchemeRef = new("ApiKey", swaggerDoc, null);
        OpenApiSecurityRequirement securityRequirement = new()
        {
            { securitySchemeRef, new List<string>() }
        };

        swaggerDoc.Security ??= [];
        swaggerDoc.Security.Add(securityRequirement);
    }
}
