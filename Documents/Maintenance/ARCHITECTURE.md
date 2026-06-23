# Architecture Overview

## Folder Structure

```
HungarianVatDeclarationGenerator.Api/
├── Controllers/
│   └── VatDeclarationController.cs          # Thin REST controller, handles HTTP concerns only
├── Models/
│   ├── Invoice.cs                           # Domain model with calculated properties
│   ├── VatSummary.cs                        # VAT summary grouped by rate
│   └── VatDeclarationResult.cs              # Complete declaration with all summaries
├── Services/
│   ├── ICsvParserService.cs                 # Contract for CSV parsing
│   ├── CsvParserService.cs                  # CSV parsing + validation logic
│   ├── IVatCalculationService.cs            # Contract for VAT calculations
│   ├── VatCalculationService.cs             # Business logic for VAT totals
│   ├── IPdfGenerationService.cs             # Contract for PDF generation
│   └── PdfGenerationService.cs              # PDF report generation (placeholder)
├── Middleware/
│   └── GlobalExceptionHandlerMiddleware.cs  # Centralized error handling
├── Constants/
│   └── VatRates.cs                          # Supported VAT rates configuration
├── TestData/
│   └── sample-invoices.csv                  # Sample CSV for manual testing
└── Program.cs                               # DI registration + middleware pipeline
```

## Core Classes & Responsibilities

### 1. **Models** (Pure Data + Derived Properties)
- **Invoice**: Immutable record representing a single invoice
  - NetAmount, VatRate (input)
  - VatAmount, GrossAmount (calculated properties)
- **VatSummary**: Aggregated totals for one VAT rate category
- **VatDeclarationResult**: Complete report with all summaries + grand totals

### 2. **Services** (Business Logic)
- **CsvParserService**
  - Validates CSV structure (header, column count)
  - Parses rows with strong validation
  - Rejects invalid VAT rates, negative amounts, malformed data
  - Throws `InvalidOperationException` with user-friendly messages

- **VatCalculationService**
  - Groups invoices by VAT rate
  - Calculates totals per group
  - Computes grand totals
  - Pure business logic, no I/O

- **PdfGenerationService**
  - Generates PDF report from `VatDeclarationResult`
  - (To be implemented with QuestPDF)

### 3. **Controller** (HTTP Adapter)
- **VatDeclarationController**
  - File upload validation (size, type)
  - Delegates parsing → calculation → PDF generation to services
  - Returns JSON or PDF file
  - No business logic

### 4. **Middleware** (Cross-Cutting Concerns)
- **GlobalExceptionHandlerMiddleware**
  - Catches `InvalidOperationException` → 400 Bad Request (validation errors)
  - Catches all other exceptions → 500 Internal Server Error
  - Logs exceptions server-side
  - Returns safe error messages to clients

## Data Flow

```
HTTP Request (CSV Upload)
	↓
Controller validates file (size, type)
	↓
CsvParserService parses & validates rows
	↓
VatCalculationService groups & calculates totals
	↓
Controller returns JSON (or)
	↓
PdfGenerationService generates PDF
	↓
Controller returns PDF file
```

## Why This Architecture?

### ✅ **Perfect for a Coding Challenge**
1. **Simple**: No unnecessary abstractions (no repositories, no CQRS, no domain events)
2. **Clear**: Each service has one responsibility
3. **Testable**: Pure business logic in services, easy to unit test
4. **Production-ready**: Proper error handling, validation, logging
5. **Extensible**: Easy to add features (e.g., XML support, email reports) without changing existing code

### ✅ **Design Principles Applied**
- **Separation of Concerns**: Controllers handle HTTP, services handle business logic
- **Dependency Injection**: All dependencies injected via constructors
- **Immutability**: Models are records, preventing accidental mutations
- **Single Responsibility**: Each class does one thing well
- **Explicit over Implicit**: No hidden magic, clear service contracts

### ✅ **Security Considerations**
- File size limits (5 MB)
- File type validation (CSV only)
- Input validation (VAT rates, positive amounts)
- Exception sanitization (no stack traces to clients)
- CORS configured for specific origins only

### ❌ **What We Avoided (Intentionally)**
- **Repositories**: No database, no need for abstraction
- **MediatR/CQRS**: Overkill for simple CRUD-like operations
- **AutoMapper**: Simple DTOs, manual mapping is clearer
- **FluentValidation**: Will add in next step if needed (currently inline validation is sufficient)
- **Multi-project architecture**: Single API project keeps it simple

## Testing Strategy

### Unit Tests
- **VatCalculationServiceTests**: Business logic validation
  - Empty lists, single invoices, multiple VAT rates
  - Correct grouping and totals
  - Order verification

- **CsvParserServiceTests**: Input validation
  - Valid CSV parsing
  - Invalid headers, missing data
  - Invalid VAT rates, negative amounts
  - Edge cases (empty lines, whitespace)

### Integration Tests (Next Step)
- Full request → response flow
- File upload validation
- End-to-end CSV processing
- Error handling verification

## How to Verify with Swagger

The API runs at: **https://localhost:7122**

Swagger UI is available at: **https://localhost:7122** (configured as root)

### Authentication Required

**All VAT declaration endpoints require API key authentication:**

1. Click the **🔓 Authorize** button (top right)
2. Enter: `challenge-demo-key-2024`
3. Click **Authorize**, then **Close**

The `X-API-Key` header will be automatically included in all subsequent requests.

### Test Endpoints

#### 1. **GET /api/Config** (Public - No Auth)
Returns upload constraints and allowed file types for frontend.

#### 2. **POST /api/VatDeclaration/upload** (Protected)
Uploads a CSV and returns JSON summary.

**Steps:**
1. Open http://localhost:5247 in your browser
2. Click on `POST /api/VatDeclaration/upload`
3. Click "Try it out"
4. Click "Choose File" and upload `HungarianVatDeclarationGenerator.Api/TestData/sample-invoices.csv`
5. Click "Execute"

**Expected Response (200 OK):**
```json
{
  "summariesByVatRate": [
	{
	  "vatRate": 5,
	  "totalNetAmount": 2500,
	  "totalVatAmount": 125,
	  "totalGrossAmount": 2625,
	  "invoiceCount": 1
	},
	{
	  "vatRate": 18,
	  "totalNetAmount": 8000,
	  "totalVatAmount": 1440,
	  "totalGrossAmount": 9440,
	  "invoiceCount": 2
	},
	{
	  "vatRate": 27,
	  "totalNetAmount": 18000,
	  "totalVatAmount": 4860,
	  "totalGrossAmount": 22860,
	  "invoiceCount": 2
	}
  ],
  "grandTotalNet": 28500,
  "grandTotalVat": 6425,
  "grandTotalGross": 34925,
  "totalInvoiceCount": 5
}
```

#### 2. **POST /api/VatDeclaration/upload-and-generate-pdf**
Uploads a CSV and returns a PDF file.

**Steps:**
1. Click on `POST /api/VatDeclaration/upload-and-generate-pdf`
2. Click "Try it out"
3. Upload the same CSV file
4. Click "Execute"

**Expected Result:**
- Currently returns 501 Not Implemented (PDF generation is placeholder)
- Will be implemented in the next step

### Test Error Handling

Create an **invalid CSV** to test validation:

**invalid-vat-rate.csv:**
```csv
InvoiceNumber,NetAmount,VatRate
INV-001,10000,99
```

Upload this file and verify you get a **400 Bad Request** with a clear error message:
```json
{
  "error": "Line 2: Invalid VAT rate 99%. Supported rates: 5, 18, 27%",
  "statusCode": 400
}
```

## Next Steps

1. ✅ **Architecture & Core Services** (DONE)
2. 🔲 **Add FluentValidation** (if needed for more complex validation)
3. 🔲 **Implement PDF Generation** (using QuestPDF)
4. 🔲 **Add Integration Tests** (test full HTTP flow)
5. 🔲 **Build React Frontend** (file upload + display results)
6. 🔲 **Add HTTPS & Production Configuration**

## Summary

This architecture is:
- **Simple enough** to complete in 60-90 minutes
- **Production-quality** with proper error handling, validation, logging
- **Testable** with clear separation of concerns
- **Maintainable** with explicit dependencies and single responsibilities
- **Secure** with file validation and exception sanitization

No over-engineering, no unnecessary abstractions, just clean, working code.
