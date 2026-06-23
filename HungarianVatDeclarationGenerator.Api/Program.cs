using AspNetCoreRateLimit;
using CsvHelper.Configuration;
using HungarianVatDeclarationGenerator.Api.Authentication;
using HungarianVatDeclarationGenerator.Api.Configuration;
using HungarianVatDeclarationGenerator.Api.Middleware;
using HungarianVatDeclarationGenerator.Api.Services;
using HungarianVatDeclarationGenerator.Api.Swagger;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.OpenApi;
using System.Globalization;
using System.Text.Json;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

ConfigureApplicationSettings(builder.Services, builder.Configuration);
ConfigureFrameworkOptions(builder.Services, builder.Configuration);
ConfigureCsvHelper(builder.Services);
ConfigureAuthentication(builder.Services, builder.Configuration);
ConfigureRateLimiting(builder.Services, builder.Configuration);
ConfigureWebServices(builder.Services, builder.Configuration);
ConfigureApplicationServices(builder.Services);
ConfigureCors(builder.Services, builder.Configuration, builder.Environment);

WebApplication app = builder.Build();

ConfigureSecurityHeaders(app);
ConfigureMiddleware(app);
ConfigureDevelopmentFeatures(app);
ConfigureHttpPipeline(app);

app.Run();

static void ConfigureApplicationSettings(IServiceCollection services, IConfiguration configuration)
{
    services.ConfigureSettings<VatRateSettings>(configuration, VatRateSettings.SectionName);
    services.ConfigureSettings<FileUploadSettings>(configuration, FileUploadSettings.SectionName);
    services.ConfigureSettings<CsvParsingSettings>(configuration, CsvParsingSettings.SectionName);
    services.ConfigureSettings<ApiKeySettings>(configuration, ApiKeySettings.SectionName);
    services.ConfigureSettings<VatCalculationSettings>(configuration, VatCalculationSettings.SectionName);
    services.ConfigureSettings<RateLimitSettings>(configuration, RateLimitSettings.SectionName);
    services.ConfigureSettings<FileValidationSettings>(configuration, FileValidationSettings.SectionName);
}

static void ConfigureFrameworkOptions(IServiceCollection services, IConfiguration configuration)
{
    services.Configure<JsonOptions>(options =>
    {
        options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

    FileUploadSettings fileUploadSettings = configuration
        .GetSection(FileUploadSettings.SectionName)
        .Get<FileUploadSettings>()
        ?? throw new InvalidOperationException($"Missing configuration: {FileUploadSettings.SectionName}");

    services.Configure<FormOptions>(options =>
    {
        // Defense in depth: MultipartBodyLengthLimit is set to match application-level 
        // MaxFileSizeBytes to provide consistent validation at both framework and application layers
        options.MultipartBodyLengthLimit = fileUploadSettings.MaxFileSizeBytes;

        // MaxFormValueLengthBytes protects against excessively long individual form field values
        options.ValueLengthLimit = (int)fileUploadSettings.MaxFormValueLengthBytes;
    });
}

static void ConfigureCsvHelper(IServiceCollection services)
{
    services.AddSingleton(_ => new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true,
        MissingFieldFound = null,
        BadDataFound = null,
        TrimOptions = TrimOptions.Trim
    });
    services.AddSingleton<ICsvReaderFactory, CsvReaderFactory>();
}

static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
{
    IConfigurationSection apiKeySection = configuration.GetSection(ApiKeySettings.SectionName);
    ApiKeySettings apiKeySettings = configuration
        .GetSection(ApiKeySettings.SectionName)
        .Get<ApiKeySettings>()
        ?? throw new InvalidOperationException($"Missing configuration: {ApiKeySettings.SectionName}");

    services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = ApiKeyAuthenticationOptions.Scheme;
        options.DefaultChallengeScheme = ApiKeyAuthenticationOptions.Scheme;
    })
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationOptions.Scheme,
        options =>
        {
            options.HeaderName = apiKeySettings.HeaderName;
            options.ValidKey = apiKeySettings.ValidKey;
        });
}

static void ConfigureRateLimiting(IServiceCollection services, IConfiguration configuration)
{
    RateLimitSettings rateLimitSettings = configuration
        .GetSection(RateLimitSettings.SectionName)
        .Get<RateLimitSettings>()
        ?? throw new InvalidOperationException($"Missing configuration: {RateLimitSettings.SectionName}");

    services.AddMemoryCache();
    services.Configure<IpRateLimitOptions>(options =>
    {
        options.EnableEndpointRateLimiting = true;
        options.StackBlockedRequests = false;
        options.HttpStatusCode = 429;
        options.RealIpHeader = "X-Real-IP";
        options.ClientIdHeader = "X-ClientId";
        options.GeneralRules =
        [
            new RateLimitRule
            {
                Endpoint = "POST:/api/VatDeclaration/*",
                Period = $"{rateLimitSettings.UploadPeriodMinutes}m",
                Limit = rateLimitSettings.UploadLimitCount
            },
            new RateLimitRule
            {
                Endpoint = "*",
                Period = $"{rateLimitSettings.GlobalPeriodHours}h",
                Limit = rateLimitSettings.GlobalLimitCount
            }
        ];
    });

    services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
    services.AddInMemoryRateLimiting();
}


static void ConfigureWebServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddControllers();
    services.AddEndpointsApiExplorer();

    services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        { 
            Title = "Hungarian VAT Declaration API", 
            Version = "v1",
            Description = """
                ⚠️ **DEMO ONLY - API Key Authentication**

                Click the **🔓 Authorize** button and enter: `challenge-demo-key-2024`

                **Public endpoint:** `/api/Config` does not require authentication.

                **Protected endpoints:** `/api/VatDeclaration/*` require the X-API-Key header.
                """
        });

        options.DocumentFilter<ApiKeyDocumentFilter>();
        options.OperationFilter<ApiKeyOperationFilter>();
    });
}

static void ConfigureApplicationServices(IServiceCollection services)
{
    services.AddScoped<ICsvParserService, CsvParserService>();
    services.AddScoped<IVatCalculationService, VatCalculationService>();
    services.AddScoped<IPdfGenerationService, PdfGenerationService>();
}

static void ConfigureCors(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
{
    string[] allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? throw new InvalidOperationException("Cors:AllowedOrigins must be configured");

    if (!env.IsDevelopment())
    {
        if (allowedOrigins.Any(o => !o.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Production CORS origins must use HTTPS");
        }
    }

    services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .WithMethods("GET", "POST", "OPTIONS")
                  .WithHeaders("Content-Type", "Accept", "X-API-Key")
                  .WithExposedHeaders("Content-Disposition")
                  .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
        });
    });
}

static void ConfigureSecurityHeaders(WebApplication app)
{
    app.Use(async (context, next) =>
    {
        if (context.Request.IsHttps)
        {
            context.Response.Headers.Append("Strict-Transport-Security",
                "max-age=31536000; includeSubDomains; preload");
        }

        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "no-referrer");
        context.Response.Headers.Append("Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; " +
            "font-src 'self'; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none'");
        context.Response.Headers.Append("Permissions-Policy",
            "geolocation=(), microphone=(), camera=()");

        await next();
    });
}

static void ConfigureMiddleware(WebApplication app)
{
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
}

static void ConfigureDevelopmentFeatures(WebApplication app)
{
    bool enableSwagger = app.Configuration.GetValue<bool>("Swagger:EnableInProduction", false);
    if (app.Environment.IsDevelopment() || enableSwagger)
    {
        ConfigureSwagger(app);
    }
}

static void ConfigureSwagger(WebApplication app)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Hungarian VAT Declaration API v1");
        options.RoutePrefix = string.Empty;
    });
}

static void ConfigureHttpPipeline(WebApplication app)
{
    app.UseIpRateLimiting();
    app.UseHttpsRedirection();
    app.UseCors("AllowFrontend");

    app.Use(async (context, next) =>
    {
        if (IsSwaggerPath(context.Request.Path))
        {
            context.Items["SkipApiKeyAuth"] = true;
        }
        await next();
    });

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
}

static bool IsSwaggerPath(PathString path)
{
    string pathValue = path.Value ?? string.Empty;
    return pathValue.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
        || pathValue.Equals("/", StringComparison.OrdinalIgnoreCase)
        || pathValue.Equals(string.Empty, StringComparison.OrdinalIgnoreCase);
}
