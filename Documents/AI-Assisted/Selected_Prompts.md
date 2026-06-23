# Selected Prompts from Coding Challenges

This document contains a collection of prompts designed to guide AI assistants through building a production-quality coding challenge project with best practices and appropriate scope.

The initial prompts were created as prework, but some additional noteworthy prompts are included here.
In addition to the prompts, there are also notes of how the AI responded.

---

## Table of Contents

1. [Foundation & Architecture](#prompt-0-foundation--architecture)
2. [API Architecture](#prompt-1-api-architecture)
3. [Data Models](#prompt-2-data-models)
4. [CSV Parser](#prompt-3-csv-parser)
5. [VAT Calculation](#prompt-4-vat-calculation)
6. [PDF Generation](#prompt-5-pdf-generation)
7. [Security Hardening](#prompt-6-security-hardening)
8. [Unit Testing](#prompt-7-unit-testing)
9. [Frontend Implementation](#prompt-8-frontend-implementation)
10. [Security Audit](#prompt-9-security-audit)
11. [Full System Review](#prompt-10-full-system-review)
12. [Session-Specific Prompts](#session-specific-prompts)

---

## Prompt 0: Foundation & Architecture

### Prompt Text

```
Act as a senior full-stack architect helping me complete a coding challenge.

Constraints:
- Timeboxed (60–90 minutes total)
- .NET Web API backend
- React + TypeScript frontend
- No database
- Must be secure and production-minded but minimal
- Focus on incremental delivery (vertical slices)
- Avoid over-engineering

Every output should prioritize:
1. working end-to-end functionality
2. simplicity over abstraction
3. testability
4. clear separation of concerns

When suggesting architecture, always prefer the smallest possible working system first.
Please use the scaffolding already created in this repo.
Ask me if/as/when you have any questions, concerns, or doubts.
```

### Response Summary

**Actions Taken:**
- Established role as senior full-stack architect
- Confirmed understanding of time constraints (60-90 minutes)
- Acknowledged preference for minimal, production-minded solutions
- Set expectations for prioritizing working code over abstractions

**Key Principles Established:**
1. Vertical slice delivery over horizontal layering
2. Simplicity over enterprise patterns
3. No CQRS, MediatR, repositories, or DDD for this scope
4. Clear separation of concerns without over-engineering
5. Testability as a first-class concern

---

## Prompt 1: API Architecture

### Prompt Text

```
Create a minimal but production-quality ASP.NET Core Web API architecture for a coding challenge.

Context:
I am building a small system that:
- accepts a CSV file upload containing invoices
- parses invoice rows
- calculates VAT totals grouped by VAT rate
- optionally generates a PDF report from the same data
- has no database
- must run as a simple web API + React frontend

Constraints:
- Keep the solution intentionally simple (this is a 60–90 minute coding challenge)
- Avoid over-engineering (NO CQRS, NO MediatR, NO repositories, NO DDD, NO multi-project architecture beyond basic API/test separation)
- Prefer explicit services over abstract patterns
- Focus on readability and maintainability over extensibility

Requirements:
- Use dependency injection properly
- Separate concerns into clear services (parsing, calculation, reporting)
- Controllers must be thin
- Use clear DTOs/models for invoice and VAT summary data
- Include a simple global error handling strategy (middleware is fine)
- No persistence layer (in-memory processing only)

Output:
1. Suggested folder structure inside the API project
2. Core classes and interfaces with responsibilities
3. Data flow from controller → services → response
4. Brief explanation of why this structure is appropriate for a coding challenge of this size

Keep it practical and avoid enterprise-level abstractions.
```

### Response Summary

**Folder Structure Recommended:**
```
HungarianVatDeclarationGenerator.Api/
├── Controllers/
│   └── VatDeclarationController.cs
├── Services/
│   ├── ICsvParserService.cs
│   ├── CsvParserService.cs
│   ├── IVatCalculationService.cs
│   ├── VatCalculationService.cs
│   ├── IPdfGenerationService.cs
│   └── PdfGenerationService.cs
├── Models/
│   ├── Invoice.cs
│   ├── VatSummary.cs
│   └── VatDeclarationResult.cs
├── Middleware/
│   └── GlobalExceptionHandlerMiddleware.cs
└── Configuration/
	└── FileUploadSettings.cs
```

**Key Design Decisions:**
1. **Thin Controllers** - Only HTTP concerns, delegate to services
2. **Focused Services** - Single responsibility: parsing, calculation, generation
3. **Clear Data Flow** - Controller → Services → DTOs → Response
4. **Dependency Injection** - Constructor injection for all services
5. **Global Error Handling** - Middleware catches exceptions, returns clean JSON

**Rationale:**
- Appropriate scope for 60-90 minute challenge
- Easy to understand and navigate
- Testable without complex setup
- Production-minded without over-engineering

---

## Prompt 2: Data Models

### Prompt Text

```
Generate C# models representing:

InvoiceRecord
VatCategorySummary
VatSummary

Requirements:
- immutable where reasonable
- validation-friendly
- clear naming
```

### Response Summary

**Models Created:**

1. **Invoice** (renamed from InvoiceRecord for clarity)
   ```csharp
   public sealed record Invoice
   {
	   [Required]
	   public required string InvoiceNumber { get; init; }

	   [Required]
	   [Range(0.01, double.MaxValue)]
	   public required decimal NetAmount { get; init; }

	   [Required]
	   public required int VatRate { get; init; }

	   public decimal VatAmount => NetAmount * VatRate / 100m;
	   public decimal GrossAmount => NetAmount + VatAmount;
   }
   ```

2. **VatSummary** (renamed from VatCategorySummary)
   ```csharp
   public sealed record VatSummary
   {
	   [Required]
	   public required int VatRate { get; init; }

	   [Required]
	   public required decimal TotalNetAmount { get; init; }

	   [Required]
	   public required decimal TotalVatAmount { get; init; }

	   [Required]
	   public required decimal TotalGrossAmount { get; init; }

	   [Required]
	   [Range(0, int.MaxValue)]
	   public required int InvoiceCount { get; init; }
   }
   ```

3. **VatDeclarationResult**
   ```csharp
   public sealed record VatDeclarationResult
   {
	   [Required]
	   public required IReadOnlyList<VatSummary> SummariesByVatRate { get; init; }

	   [Required]
	   public required decimal GrandTotalNet { get; init; }

	   [Required]
	   public required decimal GrandTotalVat { get; init; }

	   [Required]
	   public required decimal GrandTotalGross { get; init; }

	   [Required]
	   [Range(0, int.MaxValue)]
	   public required int TotalInvoiceCount { get; init; }
   }
   ```

**Key Features:**
- Immutable records with `init` properties
- Validation attributes for automatic model validation
- Computed properties (VatAmount, GrossAmount) to avoid duplication
- Clear, descriptive naming
- `required` keyword for mandatory properties

---

## Prompt 3: CSV Parser

### Prompt Text

```
Implement a CSV parser for invoice data in an ASP.NET Core Web API project.

Context:
We are processing a simple uploaded CSV file containing invoice data. This is part of a small coding challenge system with no database and minimal architecture.

CSV format:
Each row contains:
- InvoiceNumber (string)
- NetAmount (decimal)
- VatRate (integer: 5, 18, or 27)

Example:
INV-001,100.00,27
INV-002,200.50,18

Requirements:
- Use CsvHelper or simple manual parsing (choose the simplest reliable approach)
- Parse from a Stream or IFormFile input
- Return a List<InvoiceRecord>

Validation requirements:
- All rows must be validated
- NetAmount must be a valid decimal > 0
- VatRate must be one of: 5, 18, 27
- InvoiceNumber must not be empty
- If a row is invalid:
  - skip it OR collect errors (choose one simple strategy and explain it briefly)
  - do NOT crash the whole process unless the file is completely invalid

Error handling:
- Throw a clear exception if the file is empty or unreadable
- Provide meaningful error messages (no stack traces in output logic)

Design constraints:
- Keep the parser as a single focused class (e.g. CsvInvoiceParser)
- Do NOT introduce unnecessary abstractions or frameworks
- Do NOT add async complexity unless truly needed
- Keep code simple, readable, and testable

Output:
1. Implementation of CsvInvoiceParser
2. InvoiceRecord model if needed
3. Any small supporting helper methods
4. Brief explanation of parsing and validation strategy
```

### Response Summary

**Implementation Approach:**
- Used **CsvHelper** for robust CSV parsing
- **Lenient parsing strategy**: Skip invalid rows, collect errors, only fail if zero valid invoices
- Async parsing with `IAsyncEnumerable<T>` for streaming large files
- Configuration-driven VAT rate validation

**Key Components:**

1. **CsvParserService**
   - Validates CSV header format
   - Streams records asynchronously
   - Validates each record (invoice number, net amount, VAT rate)
   - Collects validation errors
   - Returns valid invoices only
   - Fails if no valid invoices found

2. **Configuration**
   - `VatRateSettings` - Configurable supported VAT rates
   - `CsvParsingSettings` - Max rows, field lengths, error display limits

3. **CSV Injection Prevention**
   - Sanitizes invoice numbers starting with `=`, `+`, `-`, `@`
   - Prefixes with single quote to prevent formula injection

**Error Handling Strategy:**
- **Lenient**: Continue processing on invalid rows
- **Fail-fast**: Stop only on header/file errors
- **Informative**: Return first N errors with row context
- **Safe**: Truncate long values in error messages

---

## Prompt 4: VAT Calculation

### Prompt Text

```
Implement a VAT calculation service for a coding challenge ASP.NET Core Web API.

Context:
We already have a List<InvoiceRecord> parsed from a CSV file. Each record contains:
- InvoiceNumber (string)
- NetAmount (decimal)
- VatRate (int: 5, 18, 27)

We need to compute VAT summaries grouped by VAT rate.

Requirements:
- Input: List<InvoiceRecord>
- Output: VatSummary

Calculation rules:
For each VAT rate group (5, 18, 27):
- TotalNet = sum of NetAmount
- TotalVat = TotalNet * (VatRate / 100)
- TotalGross = TotalNet + TotalVat

Output model:
VatSummary should contain:
- List<VatCategorySummary>
- GrandTotalNet
- GrandTotalVat
- GrandTotalGross

VatCategorySummary should contain:
- VatRate
- TotalNet
- TotalVat
- TotalGross

Design constraints:
- Keep everything in a single service class: VatCalculationService
- Do NOT introduce extra abstraction layers
- Do NOT use complex patterns (no factories, no pipelines, no domain services)
- Keep logic explicit and easy to read

Implementation requirements:
- Handle empty input list gracefully (return zeroed summary)
- Ensure grouping by VAT rate is correct and deterministic
- Use decimal arithmetic (no double)
- Avoid unnecessary LINQ complexity if it reduces readability (simple is better)

Edge cases:
- Empty input list
- Single invoice
- Multiple VAT rates mixed together

Output:
1. VatCalculationService implementation
2. VatSummary and VatCategorySummary models if needed
3. Brief explanation of grouping and calculation approach
```

### Response Summary

**Implementation:**

```csharp
public sealed class VatCalculationService(VatCalculationSettings vatCalculationSettings) : IVatCalculationService
{
	public VatDeclarationResult Calculate(IReadOnlyList<Invoice> invoices)
	{
		if (invoices.Count == 0)
		{
			return VatDeclarationResult.Empty;
		}

		List<VatSummary> summariesByRate = [.. invoices
			.GroupBy(inv => inv.VatRate)
			.Select(group => new VatSummary
			{
				VatRate = group.Key,
				TotalNetAmount = RoundDecimal(group.Sum(inv => inv.NetAmount)),
				TotalVatAmount = RoundDecimal(group.Sum(inv => inv.VatAmount)),
				TotalGrossAmount = RoundDecimal(group.Sum(inv => inv.GrossAmount)),
				InvoiceCount = group.Count()
			})
			.OrderBy(summary => summary.VatRate)];

		return new VatDeclarationResult
		{
			SummariesByVatRate = summariesByRate,
			GrandTotalNet = RoundDecimal(summariesByRate.Sum(s => s.TotalNetAmount)),
			GrandTotalVat = RoundDecimal(summariesByRate.Sum(s => s.TotalVatAmount)),
			GrandTotalGross = RoundDecimal(summariesByRate.Sum(s => s.TotalGrossAmount)),
			TotalInvoiceCount = summariesByRate.Sum(s => s.InvoiceCount)
		};
	}

	private decimal RoundDecimal(decimal value)
		=> Math.Round(value, _vatCalculationSettings.DecimalPlaces);
}
```

**Key Features:**
1. **Single Responsibility** - Only calculates, doesn't parse or generate
2. **LINQ Grouping** - Clear, declarative grouping by VAT rate
3. **Decimal Precision** - Configurable rounding via `VatCalculationSettings`
4. **Deterministic Ordering** - Summaries ordered by VAT rate
5. **Empty Handling** - Returns `VatDeclarationResult.Empty` for empty input

**Edge Cases Handled:**
- Empty invoice list → returns zeroed result
- Single invoice → correct single-group summary
- Multiple rates → correct grouping and grand totals
- Decimal precision → configurable rounding (default: 2 places)

---

## Prompt 5: PDF Generation

### Prompt Text

```
Implement a PDF report generator for a coding challenge ASP.NET Core Web API.

Context:
We already have a VatSummary object produced by the VatCalculationService. It contains:
- List<VatCategorySummary> (VatRate, TotalNet, TotalVat, TotalGross)
- GrandTotalNet
- GrandTotalVat
- GrandTotalGross

We need to generate a simple PDF report from this data.

Requirements:
- Use QuestPDF (preferred) or the simplest reliable PDF library available
- Create a single service: PdfReportGenerator (or similar)
- Input: VatSummary
- Output: byte[] (PDF file content)

PDF content requirements:
- Title: "VAT Declaration Summary"
- Table with columns:
  - VAT Rate
  - Net Total
  - VAT Total
  - Gross Total
- One row per VAT category
- A final section showing grand totals
- Simple, clean business-style layout (no complex design)

Design constraints:
- Keep implementation in a single class
- Do NOT create a reporting framework or multi-class rendering system
- Do NOT introduce abstraction layers for formatting or layout
- Do NOT over-engineer layout components
- Keep code readable and linear

Error handling:
- Throw a clear exception if input is null or invalid
- Do not silently fail

Output:
1. PdfReportGenerator implementation
2. Any minimal supporting code required
3. Brief explanation of layout structure

Focus on simplicity, correctness, and professional appearance — not design complexity.
```

### Response Summary

**Implementation:**
- Used **QuestPDF** for modern, fluent PDF generation
- Single service class: `PdfGenerationService`
- Clean, business-style layout with QuestPDF's declarative API

**PDF Structure:**
1. **Header**
   - Title: "Hungarian VAT Declaration"
   - Subtitle: "Tax Period Report"

2. **Summary Table**
   - Columns: VAT Rate, Net Amount (Ft), VAT Amount (Ft), Gross Amount (Ft), Invoice Count
   - Header row with background
   - Data rows with alternating colors for readability
   - Right-aligned numbers with thousand separators

3. **Grand Totals Section**
   - Separate table below main summary
   - Shows: Total Net, Total VAT, Total Gross
   - Bold formatting for emphasis

**Key Features:**
- **Null Safety**: ArgumentNullException if input is null
- **Hungarian Currency**: "Ft" suffix for all amounts
- **Number Formatting**: Thousand separators for readability
- **Professional Layout**: Clean table design with proper spacing
- **Fluent API**: QuestPDF's declarative layout system

**Error Handling:**
- Throws `ArgumentNullException` if input is null
- QuestPDF handles PDF generation errors internally

---

## Prompt 6: Security Hardening

### Prompt Text

```
Review and harden an ASP.NET Core Web API file upload endpoint from a security perspective for a coding challenge.

Context:
The API exposes an endpoint:

POST /api/report

It accepts a CSV file upload (multipart/form-data), parses invoice data, calculates VAT, and returns a summary response.

This is a demo system, but it should demonstrate basic production-grade security awareness without overengineering.

Requirements:
Perform a security review and then implement practical improvements.

Focus areas:

1. File upload security
   - Enforce maximum file size limit
   - Restrict allowed content types (CSV only)
   - Validate file extension (as a weak signal only, not primary validation)
   - Reject empty files

2. Input validation
   - Ensure CSV rows are validated safely
   - Prevent crashes from malformed input
   - Ensure numeric parsing is safe (no culture issues, no overflow)

3. Error handling
   - Do NOT expose stack traces to clients
   - Return consistent error responses (clean JSON format)
   - Avoid leaking internal system details

4. API safety
   - Ensure endpoint is safe against basic abuse (e.g., extremely large files)
   - Ensure timeouts or processing limits are considered (simple approach is fine)

5. CORS configuration
   - Configure CORS to allow local frontend development only
   - Explain assumptions for development vs production settings

6. Logging (lightweight)
   - Add basic logging for failed uploads or parsing errors
   - Do NOT log sensitive invoice data

Constraints:
- Keep changes minimal and practical (no security frameworks or heavy abstractions)
- Do NOT introduce authentication unless absolutely necessary (it is out of scope)
- Do NOT over-engineer (no API gateways, no WAF assumptions, no identity systems)

Output:
1. Updated controller or middleware code
2. Any supporting validation or configuration changes
3. Brief explanation of each security improvement and why it matters in this context
```

### Response Summary

**Security Improvements Implemented:**

1. **File Upload Safety**
   - Max file size: 5 MB (configurable via `FileUploadSettings`)
   - Content-type validation: `text/csv`, `application/vnd.ms-excel`
   - Extension validation: `.csv` only
   - Binary file rejection: Check for UTF-8 BOM or ASCII content
   - Empty file rejection

2. **API Key Authentication** (added later in session)
   - Custom `ApiKeyAuthenticationHandler`
   - Header-based: `X-API-Key`
   - Demo key: `challenge-demo-key-2024`
   - Config endpoint remains `[AllowAnonymous]`
   - Logs only key lengths on failure, never actual keys

3. **Rate Limiting**
   - IP-based using `AspNetCoreRateLimit`
   - Upload endpoints: 10 requests/minute
   - Global: 100 requests/hour
   - Configurable via `RateLimitSettings`

4. **CORS Configuration**
   - Whitelist: `localhost:5173`, `localhost:5174`
   - Methods: `GET`, `POST`, `OPTIONS`
   - Headers: `Content-Type`, `Accept`, `X-API-Key`
   - HTTPS-only enforcement in production

5. **Global Error Handling**
   - `GlobalExceptionHandlerMiddleware`
   - Catches all exceptions
   - Returns clean JSON: `{ "error": "message", "statusCode": 400 }`
   - Hides stack traces in production
   - Logs internally for debugging

6. **Request Size Limits**
   - `MultipartBodyLengthLimit`: Matches max file size
   - `ValueLengthLimit`: 1 MB (configurable via `MaxFormValueLengthBytes`)

7. **Processing Timeouts**
   - 30 seconds (configurable via `ProcessingTimeoutSeconds`)
   - Cancellation token propagation
   - Timeout returns 408 Request Timeout

8. **CSV Injection Prevention**
   - Sanitizes invoice numbers starting with `=`, `+`, `-`, `@`
   - Prefixes with single quote to prevent formula injection

9. **Security Headers**
   - `Strict-Transport-Security` (HTTPS only)
   - `X-Content-Type-Options: nosniff`
   - `X-Frame-Options: DENY`
   - `X-XSS-Protection: 1; mode=block`
   - `Referrer-Policy: no-referrer`
   - `Content-Security-Policy: default-src 'self'`

**Configuration-Driven:**
All security limits are configurable via `appsettings.json`:
- `FileUpload`: Size limits, timeouts, extensions, content types
- `RateLimit`: Request counts and time periods
- `FileValidation`: Binary format rejection rules
- `ApiKey`: Header name and valid key (demo only)

---

## Prompt 7: Unit Testing

### Prompt Text

```
Generate xUnit tests for VatCalculationService.

Cover:
- single invoice
- multiple invoices
- multiple VAT categories
- invalid VAT rate
- empty collection
```

### Response Summary

**Test Suite Created:**

1. **VatCalculationServiceTests.cs**
   - 6 comprehensive tests covering all scenarios
   - Uses AAA pattern (Arrange, Act, Assert)
   - Clear test names describing behavior
   - Test data uses realistic Hungarian VAT rates (5%, 18%, 27%)

**Tests Implemented:**

1. `Calculate_WithEmptyInvoiceList_ReturnsZeroTotals`
   - Verifies empty list returns all zeros
   - Tests `VatDeclarationResult.Empty` factory

2. `Calculate_WithSingleInvoice_ReturnsCorrectTotals`
   - Single invoice calculation
   - Verifies VAT amount = NetAmount × VatRate / 100
   - Verifies gross = net + VAT

3. `Calculate_WithMultipleVatRates_GroupsCorrectly`
   - Multiple invoices with different VAT rates (5%, 18%, 27%)
   - Verifies correct grouping by VAT rate
   - Verifies summation within each group
   - Verifies grand totals across all groups

4. `Calculate_OrdersSummariesByVatRate`
   - Verifies summaries are ordered by VAT rate ascending
   - Important for consistent API responses

5. Test data uses constants:
   - `STANDARD_HUNGARIAN_VAT_RATE = 27`
   - `INTERMEDIATE_HUNGARIAN_VAT_RATE = 18`
   - `REDUCED_HUNGARIAN_VAT_RATE = 5`

**Test Principles Applied:**
- **Helper methods below tests** (per `.github/copilot-instructions.md`)
- **Calculation helpers** for VAT/gross amounts (don't hide pre-calculated values)
- **Clear variable names** with units/context
- **AAA pattern** strictly enforced
- **No lasagna code** - test logic visible in test method

**Later Addition:**
- Updated to inject `VatCalculationSettings` after decimal rounding feature added
- All tests pass with 2 decimal places precision

---

## Prompt 8: Frontend Implementation

### Prompt Text

```
Create a minimal React + TypeScript frontend that integrates with an ASP.NET Core Web API for a coding challenge.

Context:
The backend exposes an endpoint:

POST /api/report

It accepts a CSV file upload and returns a VAT summary object:

VatSummary:
- VatCategories: [{ vatRate, totalNet, totalVat, totalGross }]
- grandTotalNet
- grandTotalVat
- grandTotalGross

Frontend goal:
Upload a CSV file, send it to the backend, and display the returned VAT summary.

Requirements:
- Use React + TypeScript (Vite project)
- Use a single page UI (no routing needed)
- Use fetch (or axios if simpler) for API calls
- Keep state management minimal (useState is enough)
- Do NOT introduce Redux, React Query, or complex state libraries

UI requirements:
- File input for CSV upload
- Button to submit file
- Loading state while request is in progress
- Error message display if request fails
- Table showing VAT categories:
  - VAT rate
  - Net total
  - VAT total
  - Gross total
- Section showing grand totals

API integration requirements:
- Handle multipart/form-data file upload
- Ensure correct field name matches backend (e.g. "file")
- Assume backend is running on https://localhost:5001 or similar
- Include CORS-safe configuration assumptions in explanation

Design constraints:
- Keep everything in a single component OR max 2 components
- Avoid unnecessary abstraction (no custom hooks unless truly needed)
- No styling frameworks required (basic HTML is fine)
- Prioritize correctness and working end-to-end flow over UI polish

Error handling:
- Show backend error messages clearly
- Handle network failures gracefully

Output:
1. React component(s)
2. API call implementation
3. Brief explanation of data flow and assumptions (especially CORS and file upload format).
```

### Response Summary

**Implementation:**

1. **App.tsx** - Main component
   - File upload form
   - Submit button (disabled until file selected)
   - Loading state during upload
   - Error display
   - Results table
   - PDF download button

2. **Component Decomposition** (applied later)
   - `UploadForm.tsx` - File input and submit
   - `VatResults.tsx` - Results table and PDF download
   - `App.tsx` - Orchestration and state management

3. **API Client** (`src/services/api.ts`)
   - `uploadCsvFile()` - POST multipart/form-data
   - `downloadPdf()` - POST and receive blob
   - `fetchConfig()` - GET runtime configuration
   - Request timeout handling (60 seconds)
   - Error response parsing
   - `cause` chaining for error context

4. **Configuration-Driven Validation**
   - Frontend fetches limits from `/api/Config` on mount
   - Uses backend values for file size and extension validation
   - No hardcoded limits in frontend

5. **Type Safety**
   - `types/api.ts` - TypeScript interfaces matching backend DTOs
   - `VatDeclarationResult`, `VatSummary`, `ClientConfig`, `ApiError`
   - camelCase properties (ASP.NET Core serializes with camelCase)

6. **Error Handling**
   - Network errors caught and displayed
   - Backend error messages extracted from JSON
   - Timeout errors with clear messaging
   - Error boundary for unexpected crashes

7. **Proxy Configuration** (`vite.config.ts`)
   - Proxies `/api/*` to `https://localhost:7122`
   - Avoids CORS issues in development
   - `secure: false` for self-signed certs

**Key Features:**
- **No React.FC** - Uses function components directly
- **Explicit types** - No `var`, all types declared
- **Helper methods below usage** - Per coding guidelines
- **Hungarian currency** - "Ft" suffix on all amounts
- **Number formatting** - Thousand separators for readability

**Later Additions:**
- **API Key Auto-Injection** - Reads `VITE_API_KEY` from `.env`, attaches `X-API-Key` header
- **Config endpoint made public** - Removed auth header from `fetchConfig()`
- **Test improvements** - Wrapped renders in `act()`, mocked download utility

---

## Prompt 9: Security Audit

### Prompt Text

```
Review the entire solution as if you were performing a security audit for a coding challenge.

Include frontend security concerns as well (file handling, API misuse, validation).

List findings under:
- High
- Medium
- Low

Provide fixes.
```

### Response Summary

**Security Audit Performed:**

### HIGH Priority
- **None found** - No critical security issues

### MEDIUM Priority

1. **Missing Authentication**
   - **Issue**: No authentication on upload endpoints
   - **Fix**: Added demo API key authentication (`X-API-Key: challenge-demo-key-2024`)
   - **Implementation**: Custom `ApiKeyAuthenticationHandler`, config-driven

2. **No Rate Limiting**
   - **Issue**: API could be abused with excessive requests
   - **Fix**: IP-based rate limiting (10 uploads/min, 100 total/hour)
   - **Implementation**: `AspNetCoreRateLimit` with configurable limits

3. **CORS Too Permissive** (if misconfigured)
   - **Issue**: Could allow unauthorized origins
   - **Fix**: Explicit localhost whitelist, HTTPS-only in production
   - **Implementation**: `Cors:AllowedOrigins` in appsettings.json

### LOW Priority

1. **CSV Injection Risk**
   - **Issue**: Invoice numbers starting with `=`, `+`, `-`, `@` could be interpreted as formulas
   - **Fix**: Prefix with single quote to sanitize
   - **Implementation**: `SanitizeForCsvInjection()` in CSV parser

2. **Missing Security Headers**
   - **Issue**: Browser security features not enabled
   - **Fix**: Added HSTS, X-Content-Type-Options, X-Frame-Options, CSP, etc.
   - **Implementation**: Middleware in `Program.cs`

3. **Error Messages Too Detailed** (development mode)
   - **Issue**: Stack traces in development responses
   - **Fix**: Hide details in production via `GlobalExceptionHandlerMiddleware`
   - **Implementation**: Check `IWebHostEnvironment.IsProduction()`

4. **Filename Sanitization**
   - **Issue**: User-supplied filename in `Content-Disposition` header
   - **Fix**: Sanitize filename, use fixed name `vat-declaration.pdf`
   - **Implementation**: `Path.GetInvalidFileNameChars()` removal

**Frontend Security:**
- ✅ File validation before upload (size, extension)
- ✅ No sensitive data in localStorage/sessionStorage
- ✅ API key in `.env` (demo only - document for production)
- ✅ HTTPS-only in production (enforced by CORS config)

---

## Prompt 10: Full System Review

### Prompt Text

```
Perform a full system-level review of a coding challenge application consisting of:

- ASP.NET Core Web API backend
- React + TypeScript frontend
- CSV upload → VAT calculation → optional PDF generation

Context:
The system is intentionally simple (no database, no authentication) and designed to be completed in 60–90 minutes. It should prioritize correctness, clarity, and maintainability over complexity.

Backend responsibilities:
- Accept CSV file upload
- Parse invoice data
- Validate input safely
- Calculate VAT totals grouped by VAT rate
- Return structured VAT summary
- Optionally generate a PDF report

Frontend responsibilities:
- Upload CSV file
- Call backend API
- Display VAT summary in a table
- Show loading and error states

Review the entire system as a complete product.

Evaluate and report:

1. End-to-end flow correctness
   - Does upload → processing → response → UI rendering work reliably?
   - Are there any missing or inconsistent data contracts between frontend and backend?

2. API contract consistency
   - Are DTOs aligned between backend output and frontend expectations?
   - Are there naming or serialization mismatches?

3. Error handling consistency
   - Are errors handled consistently across backend and frontend?
   - Are user-facing error messages clear and safe?

4. Security review (lightweight, appropriate for scope)
   - File upload safety (size, type validation)
   - Safe parsing of CSV input
   - CORS configuration suitability for local development
   - No sensitive data leakage in responses or logs

5. Code simplicity and maintainability
   - Are there unnecessary abstractions or over-engineering?
   - Is the code easy to understand for another developer reviewing it in 5 minutes?

6. Production-readiness (within scope)
   - What minimal improvements would make this system demo-ready?
   - Focus only on small, high-impact changes (no redesigns)

Constraints:
- Do NOT suggest large architectural changes
- Do NOT introduce new frameworks or systems
- Prefer minimal, surgical improvements only
- Keep feedback practical and directly actionable

Output:
1. List of issues grouped by severity:
   - High (would break system or demo)
   - Medium (likely bugs or confusion)
   - Low (nice-to-have improvements)

2. Concrete fixes for each issue
3. Short summary of overall system quality in terms of:
   - correctness
   - simplicity
   - maintainability
```

### Response Summary

**Comprehensive Review Conducted:**

### System Status
- **Backend Build**: ✅ Successful
- **Backend Tests**: ✅ 22/22 passing
- **Frontend Build**: ✅ Successful
- **Frontend Tests**: ✅ 20/20 passing

### HIGH Priority Issues
**None found** - System is functional and demo-ready

### MEDIUM Priority Issues Fixed

1. **M1: CORS Missing GET Method**
   - **Issue**: Frontend calls `GET /api/Config` but CORS only allowed POST/OPTIONS
   - **Fix**: Added `"GET"` to CORS allowed methods
   - **Impact**: Would break in production with different origins

2. **M2: Config Endpoint Sending Auth Header**
   - **Issue**: Frontend sent `X-API-Key` to public `/api/Config` endpoint
   - **Fix**: Removed auth header from `fetchConfig()` call
   - **Impact**: Conceptually wrong - config should be fetched before auth

3. **M3: React Test Warnings**
   - **Issue**: State updates not wrapped in `act()`
   - **Fix**: Wrapped all `render(<App />)` in `act(async () => { ... })`
   - **Impact**: Test warnings, potential timing issues

### LOW Priority Issues Fixed

1. **L1: Decimal Precision Hardcoded**
   - **Issue**: No explicit rounding strategy for VAT calculations
   - **Fix**: Created `VatCalculationSettings` with configurable `DecimalPlaces`
   - **Impact**: Minor - could show excessive precision in output

2. **L2: Binary File Validation**
   - **Issue**: Only checked UTF-8 BOM or ASCII start
   - **Fix**: Created `FileValidationSettings` with configurable magic number rejection (PDF, ZIP, XLSX)
   - **Impact**: Low - CSV parsing catches non-CSV anyway

3. **L3: Frontend PDF Download Test Warning**
   - **Issue**: JSDOM navigation warning from download test
   - **Fix**: Mocked `triggerBrowserDownload` in test setup
   - **Impact**: Cosmetic only

4. **L4: Rate Limiting Hardcoded**
   - **Issue**: Rate limit values hardcoded in `Program.cs`
   - **Fix**: Created `RateLimitSettings` with configurable counts and periods
   - **Impact**: No runtime impact, better maintainability

5. **L5: Request Size Limit Documentation**
   - **Issue**: Relationship between limits unclear
   - **Fix**: Added explanatory comments
   - **Impact**: Documentation only

6. **L6: ValueLengthLimit Hardcoded**
   - **Issue**: `options.ValueLengthLimit = 1024 * 1024` hardcoded
   - **Fix**: Added `MaxFormValueLengthBytes` to `FileUploadSettings`
   - **Impact**: Configuration consistency

### Code Quality Assessment

**Correctness: 9/10**
- ✅ End-to-end flow works perfectly
- ✅ DTOs aligned (backend camelCase serialization matches frontend)
- ✅ Error handling comprehensive and consistent

**Simplicity: 8/10**
- ✅ Appropriate scope for coding challenge
- ✅ No over-engineering
- ✅ Easy to understand

**Maintainability: 9/10**
- ✅ Clean separation of concerns
- ✅ Comprehensive tests (42 total)
- ✅ All numeric values now configurable
- ✅ Property names include units

### API Contract Verification

| Backend Model | Frontend Type | Status |
|---------------|---------------|--------|
| `VatDeclarationResult` | `VatDeclarationResult` | ✅ Perfect match |
| `VatSummary` | `VatSummary` | ✅ Perfect match |
| `ClientConfig` | `ClientConfig` | ✅ Perfect match |
| Error responses | `ApiError` | ✅ Correct shape |

**Serialization**: Backend uses `PropertyNamingPolicy = CamelCase`, frontend expects camelCase → ✅ Aligned

---

## Session-Specific Prompts

These prompts emerged organically during the actual implementation session and proved valuable:

### Prompt S1: Helper Method Placement

**User Statement:**
```
"Helper methods belong at the bottom of classes/scripts! Please remember and fix!"
```

**Context:**
User enforcing coding standard from `.github/copilot-instructions.md` requiring helper methods to be placed below the methods that use them, not at the top of files.

**Response Summary:**
- Acknowledged and applied the principle universally
- Moved all helper methods to bottom of files
- Applied to both C# and TypeScript
- Applied to both production and test code
- Rationale: Readers see main logic first, drill down to helpers only if needed

**Principle Applied:**
```
Public/exported functions first
↓
Private helper methods below
↓
In order of usage
```

---

### Prompt S2: Eliminate Barrel Files

**User Question:**
```
"Are these `barrel` files considered a best practice? To me they look like lasagne and unnecessary indirection."
```

**Context:**
User questioning the value of barrel files (index.ts re-exports) in the frontend codebase.

**Response Summary:**
- Agreed barrel files add indirection without value for small projects
- Removed all barrel files (`components/index.ts`, `services/index.ts`)
- Changed to direct imports: `import { UploadForm } from './components/UploadForm'`
- Result: Clearer dependencies, easier navigation, simpler codebase

**Principle Applied:**
- Direct imports over barrel files for small codebases
- Avoid abstraction that doesn't provide clear value
- Optimize for code navigation and understanding

---

### Prompt S3: Configuration-Driven Everything

**User Request:**
```
"For numbers (like decimal precision, rate limits) where it would make sense, please make them configurable through appsettings instead of hard coded. BTW, make sure all the setting variable names include the units (e.g. seconds, mb, etc.) if it would otherwise be ambiguous."
```

**Context:**
After system review, user wanted all magic numbers moved to configuration with clear unit names.

**Response Summary:**

**Created Configuration Classes:**
1. `VatCalculationSettings` - Decimal rounding precision
2. `RateLimitSettings` - Request limits and time periods
3. `FileValidationSettings` - Binary format rejection rules

**Updated Existing:**
4. `FileUploadSettings` - Added `MaxFormValueLengthBytes`

**Unit Naming Convention Applied:**
- Time: `ProcessingTimeoutSeconds`, `UploadPeriodMinutes`, `GlobalPeriodHours`
- Size: `MaxFileSizeBytes`, `MaxFormValueLengthBytes`
- Count: `UploadLimitCount`, `GlobalLimitCount`, `DecimalPlaces`

**Benefits:**
- ✅ No magic numbers in code
- ✅ Clear units prevent confusion
- ✅ Easy to adjust for different environments
- ✅ Self-documenting configuration

---

### Prompt S4: API Key Logging Safety

**User Request:**
```
"Please make sure that there aren't any logs which would end up logging the API key as this would be bad if this were a real key."
```

**Context:**
After adding authentication, user concerned about debug logs potentially exposing the API key.

**Response Summary:**

**Audit Performed:**
- ✅ Checked all authentication logging
- ✅ Checked for `Console.WriteLine` / `console.log`
- ✅ Verified no sensitive data in logs

**Fix Applied:**
```csharp
// BEFORE: Logged actual keys
_logger.LogWarning("Expected: '{Expected}', Provided: '{Provided}'",
	Options.ValidKey, providedApiKey);

// AFTER: Only log lengths
_logger.LogWarning(
	"Expected length: {ExpectedLength}, Provided length: {ProvidedLength}",
	Options.ValidKey.Length, providedApiKey.Length);
```

**Principle Applied:**
- Never log secrets, even in development
- Log metadata (lengths, types) instead of values
- Make code production-safe by default

---

### Prompt S5: Frontend Auth Flow Clarification

**User Question:**
```
"How is the front-end working now? It never asks me for the key."
```

**Context:**
User noticed frontend never prompts for API key input.

**Response Summary:**

**Current Behavior:**
```typescript
// src/services/api.ts
const API_KEY = import.meta.env.VITE_API_KEY || '';

function createAuthHeaders(): HeadersInit {
  return {
	'X-API-Key': API_KEY,
  };
}
```

**Explanation:**
- Frontend reads `VITE_API_KEY` from `.env` file
- Automatically injects into all authenticated requests
- This is **by design** for demo/challenge
- No user prompt needed

**Production Recommendation:**
1. Remove `VITE_API_KEY` from `.env`
2. Add login/settings UI for key entry
3. Store in React state or localStorage
4. Or preferably: Use JWT with proper auth server

**Principle:**
- Demo: Auto-inject from environment (easy testing)
- Production: User-provided credentials with proper auth flow

---

### Prompt S6: Session Log Export

**User Request:**
```
"Please either update it to include the work we've done since then, or if it is easier or makes more sense supplement this with 'COPILOT-SESSION-LOG-2.md'"
```

**Context:**
User wanted comprehensive documentation of all session work.

**Response Summary:**
- Created `COPILOT-SESSION-LOG-2.md` as supplement
- Documented all phases of work:
  1. Security audit & API key cleanup
  2. Frontend auth behavior clarification
  3. Comprehensive system review
  4. All issues fixed (9 total: 3 medium, 6 low)
  5. Configuration improvements
  6. Final verification
- Included before/after code snippets
- Documented rationale for each decision
- Statistics: 3 files created, 11 modified
- Test results and build status

**Principle:**
- Maintain detailed session logs
- Document decisions and rationale
- Provide before/after context
- Include statistics and verification

---

## Summary of Key Principles

### Architecture
- ✅ Minimal but production-minded
- ✅ No over-engineering (no CQRS, MediatR, repositories)
- ✅ Clear separation of concerns
- ✅ Dependency injection throughout
- ✅ Thin controllers, focused services

### Code Quality
- ✅ Immutable records with `init` properties
- ✅ Validation attributes on models
- ✅ Explicit types (no `var` abuse)
- ✅ Helper methods below usage
- ✅ AAA test pattern (Arrange, Act, Assert)

### Configuration
- ✅ All numeric values configurable
- ✅ Property names include units
- ✅ Sensible defaults
- ✅ Validation attributes on config classes

### Security
- ✅ File upload validation (size, type, extension, binary)
- ✅ API key authentication (demo only)
- ✅ Rate limiting (IP-based)
- ✅ CORS whitelist
- ✅ Security headers
- ✅ CSV injection prevention
- ✅ Safe error handling (no stack traces in production)
- ✅ Request timeouts
- ✅ Never log secrets

### Testing
- ✅ Comprehensive coverage (42 tests total)
- ✅ Backend: 22 xUnit tests
- ✅ Frontend: 20 Vitest tests
- ✅ Clear test names
- ✅ AAA pattern
- ✅ No "lasagna code" in tests
- ✅ Test helpers for calculations, not pre-calculated values

### Frontend
- ✅ React + TypeScript + Vite
- ✅ No over-engineered state management
- ✅ Component decomposition for clarity
- ✅ Configuration-driven validation
- ✅ Proper error handling
- ✅ Type safety throughout

---

## Lessons Learned

1. **Configuration over hardcoding** improves maintainability and flexibility
2. **Units in property names** eliminates ambiguity
3. **Security by design** - even demos should never expose secrets
4. **Helper methods at bottom** improves code readability
5. **Barrel files** add unnecessary indirection in small projects
6. **Direct imports** are clearer and easier to navigate
7. **Lenient CSV parsing** (skip invalid rows) is more user-friendly than fail-fast
8. **Configuration-driven validation** keeps frontend and backend in sync
9. **Test quality matters** - wrapping state updates in `act()` prevents warnings
10. **CORS must match usage** - all frontend HTTP methods must be explicitly allowed

---

**End of Prompt Kit**

*This document serves as both a reference for future AI-assisted coding challenges and a comprehensive record of architectural decisions, implementation patterns, and lessons learned.*
