using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace HungarianVatDeclarationGenerator.Api.Middleware;

public sealed class GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IOptions<JsonOptions> jsonOptions
    )
{
    private readonly RequestDelegate _next = next
        ?? throw new ArgumentNullException(nameof(next));
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));
    private readonly JsonSerializerOptions _jsonOptions = (jsonOptions
        ?? throw new ArgumentNullException(nameof(jsonOptions))).Value.SerializerOptions;

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error: {Message}", ex.Message);
            await HandleException(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Request cancelled or timed out");
            await HandleException(
                context,
                HttpStatusCode.RequestTimeout,
                "Request processing timed out. Please try again with a smaller file.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleException(
                context,
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred. Please try again later.");
        }
    }

    private async Task HandleException(
            HttpContext context,
            HttpStatusCode statusCode,
            string message
        )
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            error = message,
            statusCode = (int)statusCode
        };

        string json = JsonSerializer.Serialize(response, _jsonOptions);

        await context.Response.WriteAsync(json);
    }
}
