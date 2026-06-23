using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HungarianVatDeclarationGenerator.Api.Swagger;

public sealed class ApiKeyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        bool hasAllowAnonymous = context.MethodInfo.GetCustomAttributes(true)
            .Any(attr => attr is AllowAnonymousAttribute)
            || context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
                .Any(attr => attr is AllowAnonymousAttribute) == true;

        if (hasAllowAnonymous)
        {
            operation.Security?.Clear();
        }
    }
}
