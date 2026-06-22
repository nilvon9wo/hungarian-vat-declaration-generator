using CsvHelper.Configuration;
using HungarianVatDeclarationGenerator.Api.Configuration;
using HungarianVatDeclarationGenerator.Api.Middleware;
using HungarianVatDeclarationGenerator.Api.Services;
using Microsoft.AspNetCore.Http.Json;
using System.Globalization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

ConfigureApplicationSettings(builder.Services, builder.Configuration);
ConfigureFrameworkOptions(builder.Services, builder.Configuration);
ConfigureCsvHelper(builder.Services);
ConfigureWebServices(builder.Services);
ConfigureApplicationServices(builder.Services);
ConfigureCors(builder.Services, builder.Configuration);

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
}

static void ConfigureFrameworkOptions(IServiceCollection services, IConfiguration configuration)
{
    services.Configure<JsonOptions>(options =>
    {
        options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

    FileUploadSettings fileUploadSettings = configuration
        .GetSection(FileUploadSettings.SectionName)
        .Get<FileUploadSettings>()
        ?? throw new InvalidOperationException($"Missing configuration: {FileUploadSettings.SectionName}");

    services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = fileUploadSettings.MaxFileSizeBytes;
        options.ValueLengthLimit = 1024 * 1024;
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

static void ConfigureWebServices(IServiceCollection services)
{
    services.AddControllers();
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new() { Title = "Hungarian VAT Declaration API", Version = "v1" });
    });
}

static void ConfigureApplicationServices(IServiceCollection services)
{
    services.AddScoped<ICsvParserService, CsvParserService>();
    services.AddScoped<IVatCalculationService, VatCalculationService>();
    services.AddScoped<IPdfGenerationService, PdfGenerationService>();
}

static void ConfigureCors(IServiceCollection services, IConfiguration configuration)
{
    string[] allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:5173", "http://localhost:5174"];

    services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .WithMethods("POST")
                  .WithHeaders("Content-Type", "Accept")
                  .WithExposedHeaders("Content-Disposition");
        });
    });
}

static void ConfigureSecurityHeaders(WebApplication app)
{
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "no-referrer");
        await next();
    });
}

static void ConfigureMiddleware(WebApplication app)
{
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
}

static void ConfigureDevelopmentFeatures(WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        ConfigureSwagger(app);
        app.UseCors("AllowFrontend");
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
    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();
}

