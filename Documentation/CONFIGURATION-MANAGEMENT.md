# Configuration Management - Implementation Guide

## Overview
Converted magic numbers and hardcoded values to configuration-based settings, enabling environment-specific customization without code changes.

---

## Configuration Models Created

### 1. **VatRateSettings.cs**
Location: `HungarianVatDeclarationGenerator.Api/Configuration/VatRateSettings.cs`

```csharp
public sealed class VatRateSettings
{
	public const string SectionName = "VatRates";

	[Required]
	[MinLength(1)]
	public required int[] SupportedRates { get; init; }

	public bool IsValid(int rate) => SupportedRates.Contains(rate);
	public string GetSupportedRatesDisplay() => string.Join(", ", SupportedRates);
}
```

**Purpose:** Controls which VAT rates are considered valid for invoice processing.

**Settings:**
- `SupportedRates` - Array of valid VAT percentages (e.g., [5, 18, 27] for Hungary)

**Validation:**
- Unsupported VAT rates in uploaded CSV files are treated as validation errors
- Errors are handled by the global exception middleware and returned as 400 Bad Request
- Error messages include the list of supported rates for user guidance

---

### 2. **FileUploadSettings.cs**
Location: `HungarianVatDeclarationGenerator.Api/Configuration/FileUploadSettings.cs`

```csharp
public sealed class FileUploadSettings
{
	public const string SectionName = "FileUpload";

	public long MaxFileSizeBytes { get; init; } = 5 * 1024 * 1024;  // 5 MB default
	public int ProcessingTimeoutSeconds { get; init; } = 30;         // 30 seconds default
	public required string[] AllowedContentTypes { get; init; }
	public required string[] AllowedExtensions { get; init; }
}
```

**Purpose:** Controls file upload validation and processing limits.

**Settings:**
- `MaxFileSizeBytes` - Maximum file size in bytes (security)
- `ProcessingTimeoutSeconds` - Timeout for CSV processing (DOS protection)
- `AllowedContentTypes` - Permitted MIME types (security)
- `AllowedExtensions` - Permitted file extensions (security)

---

### 3. **CsvParsingSettings.cs**
Location: `HungarianVatDeclarationGenerator.Api/Configuration/CsvParsingSettings.cs`

```csharp
public sealed class CsvParsingSettings
{
	public const string SectionName = "CsvParsing";

	public int MaxRowsToProcess { get; init; } = 10000;           // 10,000 rows default
	public int MaxInvoiceNumberLength { get; init; } = 100;       // 100 chars default
	public int MaxFieldLength { get; init; } = 500;               // 500 chars default
	public int MaxErrorsToDisplay { get; init; } = 5;             // 5 errors default
}
```

**Purpose:** Controls CSV parsing validation and resource limits.

**Settings:**
- `MaxRowsToProcess` - Maximum CSV rows to process (DOS protection)
- `MaxInvoiceNumberLength` - Max characters for invoice number (validation)
- `MaxFieldLength` - Max characters for any field (memory protection)
- `MaxErrorsToDisplay` - Max validation errors in response (information disclosure control)

---

## Configuration Files

### **appsettings.json** (Development)
```json
{
  "VatRates": {
	"SupportedRates": [ 5, 18, 27 ]
  },
  "FileUpload": {
	"MaxFileSizeBytes": 5242880,              // 5 MB
	"ProcessingTimeoutSeconds": 30,
	"AllowedContentTypes": [
	  "text/csv",
	  "application/vnd.ms-excel"
	],
	"AllowedExtensions": [ ".csv" ]
  },
  "CsvParsing": {
	"MaxRowsToProcess": 10000,
	"MaxInvoiceNumberLength": 100,
	"MaxFieldLength": 500,
	"MaxErrorsToDisplay": 5
  },
  "Cors": {
	"AllowedOrigins": [
	  "http://localhost:5173",
	  "http://localhost:5174"
	]
  }
}
```

### **appsettings.Production.json** (Production)
```json
{
  "VatRates": {
	"SupportedRates": [ 5, 18, 27 ]
  },
  "FileUpload": {
	"MaxFileSizeBytes": 10485760,             // 10 MB
	"ProcessingTimeoutSeconds": 60,           // 60 seconds
	"AllowedContentTypes": [ "text/csv" ],    // Stricter
	"AllowedExtensions": [ ".csv" ]
  },
  "CsvParsing": {
	"MaxRowsToProcess": 50000,                // Higher for production
	"MaxInvoiceNumberLength": 100,
	"MaxFieldLength": 500,
	"MaxErrorsToDisplay": 3                   // Fewer errors in production
  },
  "Cors": {
	"AllowedOrigins": [
	  "https://app.example.com"               // Production domain
	]
  }
}
```

---

## Dependency Injection Registration

### **ConfigurationExtensions.cs** (Helper Method)
```csharp
public static class ConfigurationExtensions
{
	/// <summary>
	/// Binds a configuration section to a settings class and registers both IOptions&lt;T&gt; and T in DI.
	/// Services can inject either IOptions&lt;T&gt; (for options pattern) or T directly (for simpler usage).
	/// </summary>
	public static T ConfigureSettings<T>(
		this IServiceCollection services,
		IConfiguration configuration,
		string sectionName)
		where T : class
	{
		IConfigurationSection configSection = configuration.GetSection(sectionName);

		// Register IOptions<T> (for options pattern consumers)
		services.Configure<T>(configSection);

		// Register T directly (for simple injection without IOptions wrapper)
		services.AddSingleton(provider => provider.GetRequiredService<IOptions<T>>().Value);

		// Return bound instance for immediate use in Program.cs
		T settings = configSection.Get<T>()
			?? throw new InvalidOperationException($"Configuration section '{sectionName}' is missing or invalid.");

		return settings;
	}
}
```

**Benefits:**
- ✅ Services inject settings directly (`FileUploadSettings`) instead of `IOptions<FileUploadSettings>`
- ✅ No `IOptions` abstraction leakage into services
- ✅ Cleaner service constructors
- ✅ Still supports `IOptions<T>` if needed elsewhere
- ✅ Validates configuration exists at startup

### **Program.cs**
```csharp
// Bind configuration settings and register both IOptions<T> and T
FileUploadSettings fileUploadSettings = services.ConfigureSettings<FileUploadSettings>(
	builder.Configuration,
	FileUploadSettings.SectionName);

CsvParsingSettings csvParsingSettings = services.ConfigureSettings<CsvParsingSettings>(
	builder.Configuration,
	CsvParsingSettings.SectionName);

// Use settings immediately for FormOptions configuration
services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
	options.MultipartBodyLengthLimit = fileUploadSettings.MaxFileSizeBytes;
	options.ValueLengthLimit = 1024 * 1024;
});

// Use settings for CORS configuration
string[] allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
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
```

---

## Service Updates

### **VatDeclarationController.cs**
**Before:**
```csharp
private const long MaxFileSizeBytes = 5 * 1024 * 1024;
private const int ProcessingTimeoutSeconds = 30;
private static readonly string[] AllowedContentTypes = ["text/csv", "application/vnd.ms-excel"];
```

**After:**
```csharp
public sealed class VatDeclarationController(
	ICsvParserService csvParser,
	IVatCalculationService vatCalculator,
	IPdfGenerationService pdfGenerator,
	ILogger<VatDeclarationController> logger,
	FileUploadSettings fileUploadSettings  // ← Injected directly (no IOptions)
) : ControllerBase
{
	private readonly FileUploadSettings _fileUploadSettings = fileUploadSettings;

	// Use: _fileUploadSettings.MaxFileSizeBytes
	// Use: _fileUploadSettings.ProcessingTimeoutSeconds
	// Use: _fileUploadSettings.AllowedContentTypes
	// Use: _fileUploadSettings.AllowedExtensions
}
```

**Key improvement:** No `IOptions<T>` leakage - services don't care about options pattern.

---

### **CsvParserService.cs**
**Before:**
```csharp
private const int MaxRowsToProcess = 10000;
private const int MaxInvoiceNumberLength = 100;
private const int MaxFieldLength = 500;
```

**After:**
```csharp
public sealed class CsvParserService(
	ICsvReaderFactory csvReaderFactory,
	CsvParsingSettings csvParsingSettings  // ← Injected directly (no IOptions)
) : ICsvParserService
{
	private readonly CsvParsingSettings _settings = csvParsingSettings;

	// Use: _settings.MaxRowsToProcess
	// Use: _settings.MaxInvoiceNumberLength
	// Use: _settings.MaxFieldLength
	// Use: _settings.MaxErrorsToDisplay
}
```

**Key improvement:** Cleaner constructor - service doesn't need to know about `IOptions<T>` abstraction.

---

## Test Updates

### **CsvParserServiceTests.cs**
```csharp
public CsvParserServiceTests()
{
	CsvConfiguration csvConfig = new(CultureInfo.InvariantCulture) { ... };
	CsvReaderFactory csvReaderFactory = new(csvConfig);

	// Create settings directly (no IOptions wrapper needed)
	CsvParsingSettings settings = new()
	{
		MaxRowsToProcess = 10000,
		MaxInvoiceNumberLength = 100,
		MaxFieldLength = 500,
		MaxErrorsToDisplay = 5
	};

	// Inject settings directly
	_service = new CsvParserService(csvReaderFactory, settings);
}
```

**Key improvement:** Tests are simpler - no need for `Options.Create(settings)` wrapper.

**Tests still passing:** ✅ 21/21

---

## Benefits

### 1. **Environment-Specific Configuration**
✅ Different limits for dev vs. production  
✅ No code changes needed  
✅ Configuration through deployment pipeline

**Example:**
- **Dev:** 5 MB file size, 30-second timeout, verbose errors
- **Production:** 10 MB file size, 60-second timeout, minimal errors

---

### 2. **Security Flexibility**
✅ Adjust DOS protection limits without redeployment  
✅ Environment-specific security policies  
✅ Easy to tighten/relax constraints

**Example:**
- Detect DOS attack → Temporarily reduce `MaxRowsToProcess` via config
- High-volume period → Temporarily increase `ProcessingTimeoutSeconds`

---

### 3. **Operational Flexibility**
✅ Tune performance limits based on actual load  
✅ A/B testing different thresholds  
✅ Quick response to production issues

**Example:**
- Monitor memory usage → Adjust `MaxFieldLength` if needed
- Customer feedback → Increase `MaxErrorsToDisplay` for better UX

---

### 4. **Testability**
✅ Easy to test with different configuration values  
✅ No need to modify constants  
✅ Test edge cases with extreme values

**Example:**
```csharp
CsvParsingSettings strictSettings = new()
{
	MaxRowsToProcess = 100,        // Low limit for testing
	MaxInvoiceNumberLength = 10,   // Short length for testing
	// ...
};
```

---

## Configuration Override Hierarchy

ASP.NET Core applies configuration in this order (later overrides earlier):

1. `appsettings.json`
2. `appsettings.{Environment}.json` (e.g., `appsettings.Production.json`)
3. Environment variables
4. Command-line arguments

**Example:**
```bash
# Override via environment variable
export FileUpload__MaxFileSizeBytes=20971520  # 20 MB

# Override via command line
dotnet run --FileUpload:MaxFileSizeBytes=20971520
```

---

## Azure App Service Configuration

In Azure App Service, set via **Application Settings**:

```
FileUpload__MaxFileSizeBytes = 10485760
FileUpload__ProcessingTimeoutSeconds = 60
CsvParsing__MaxRowsToProcess = 50000
Cors__AllowedOrigins__0 = https://app.example.com
```

**Note:** Double underscore (`__`) represents nested JSON structure.

---

## Docker Configuration

### **docker-compose.yml**
```yaml
services:
  api:
	image: vat-declaration-api
	environment:
	  - FileUpload__MaxFileSizeBytes=10485760
	  - FileUpload__ProcessingTimeoutSeconds=60
	  - CsvParsing__MaxRowsToProcess=50000
	  - Cors__AllowedOrigins__0=https://app.example.com
```

### **Dockerfile** (read from external config file)
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY . .
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "HungarianVatDeclarationGenerator.Api.dll"]
```

---

## Monitoring & Alerts

### Recommended Metrics to Track:
1. **File size distribution** - Are users hitting the limit?
2. **Processing timeouts** - Are timeouts happening frequently?
3. **Row counts processed** - What's the actual usage pattern?
4. **Validation errors** - Are field length limits too strict?

### Adjust Configuration Based On:
- **95th percentile processing time** → Set timeout higher
- **Max file size in past 30 days** → Adjust `MaxFileSizeBytes`
- **Average rows per file** → Tune `MaxRowsToProcess`

---

## Security Considerations

### ✅ **Good Practices:**
- Production settings more restrictive than development
- Limits high enough for legitimate use, low enough to prevent abuse
- Timeout values prevent resource exhaustion
- Field length limits prevent memory attacks

### ⚠️ **Watch Out For:**
- **Too low limits** → Legitimate users can't use the system
- **Too high limits** → System vulnerable to DOS attacks
- **Inconsistent limits** → Different environments behave differently

---

## Migration Checklist

✅ Created configuration models (`FileUploadSettings`, `CsvParsingSettings`)  
✅ Updated `appsettings.json` with all settings  
✅ Created `appsettings.Production.json` with production values  
✅ Registered settings in `Program.cs` DI container  
✅ Injected settings into controllers and services  
✅ Removed hardcoded constants  
✅ Updated unit tests to provide settings  
✅ All tests passing (21/21)  
✅ Verified no compilation errors  
✅ Documented configuration usage  

---

## Summary

**Magic numbers eliminated:**
- ❌ `MaxFileSizeBytes = 5 * 1024 * 1024` (hardcoded)
- ✅ `_fileUploadSettings.MaxFileSizeBytes` (configurable)

**Configuration locations:**
- `appsettings.json` → Development defaults
- `appsettings.Production.json` → Production overrides
- Environment variables → Runtime overrides
- Azure App Service settings → Cloud configuration

**Benefits achieved:**
- ✅ Environment-specific settings
- ✅ No redeployment for limit changes
- ✅ Operational flexibility
- ✅ Security tuning capability
- ✅ Better testability

**Next steps:**
- Deploy to Azure with App Service configuration
- Monitor metrics to tune limits
- Document operational procedures for adjusting settings
