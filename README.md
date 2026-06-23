# Hungarian VAT Declaration Generator

## Overview

This application processes invoice data from a CSV file and generates a simplified Hungarian VAT declaration summary.

The solution consists of:

- ASP.NET Core Web API backend
- React + TypeScript frontend
- PDF report generation
- Automated unit tests

## Assumptions

The challenge specification did not define the format of the uploaded source file.

For this implementation, invoice data is provided as a CSV file with the following structure:

```csv
InvoiceNumber,NetAmount,VatRate
INV-001,10000,27
INV-002,5000,18
INV-003,2500,5
```

### Supported VAT Rates

- 5%
- 18%
- 27%

## VAT Calculation

For each invoice:

```text
VAT Amount = NetAmount × VatRate / 100
Gross Amount = NetAmount + VAT Amount
```

Results are grouped by VAT category and summarized in the generated report.

---

## Features

### File Upload

- Upload invoice data as CSV
- Validation of file type
- Validation of file size
- Validation of invoice data

### VAT Summary

The application calculates:

- Net amount totals
- VAT amount totals
- Gross amount totals

Totals are grouped by VAT category.

### PDF Report

The application generates a downloadable PDF report containing:

- VAT category breakdown
- Net totals
- VAT totals
- Gross totals
- Grand totals

---

## Security Considerations

The following security measures are implemented.

### File Validation

- Only `.csv` files are accepted
- Maximum file size: 5 MB
- Invalid files are rejected

### Input Validation

- Net amounts must be positive
- VAT rates must be one of the supported values (5, 18, 27)
- Malformed rows are rejected

### Error Handling

- Internal exceptions are never exposed to clients
- API returns safe, user-friendly error messages
- Stack traces are logged server-side only

### Transport Security

- HTTPS redirection enabled (assumed for production deployment)
- CORS restricted to known frontend origin (configurable)

### Data Handling

- Files are processed in memory only
- No persistent storage of uploaded files
- No persistent storage of generated reports

---

## Design Decisions

### 1. No Database

A database was intentionally not used because the application is stateless by design. The requirement is to process uploaded files and return a computed report, not to store invoices or maintain historical data.

This keeps the solution simple, fast to evaluate, and aligned with the time-constrained nature of the challenge.

---

### 2. CSV as Input Format

CSV was chosen because:

- It is the most common format for invoice exports in real-world systems
- It is easy to validate and parse in .NET using standard libraries
- It avoids unnecessary complexity of Excel or XML parsing unless explicitly required

The format was assumed due to lack of specification and documented accordingly.

---

### 3. Demo-Only API Key Authentication

A simple API key authentication scheme is implemented for demonstration purposes:

- **Header:** `X-API-Key`
- **Demo value:** `challenge-demo-key-2024`

This is **intentionally minimal** for the coding challenge context. Real production systems should use:
- JWT tokens with Azure AD, Auth0, or similar
- Secret management (Azure Key Vault, AWS Secrets Manager)
- Claims-based authorization
- Proper RBAC and user management

The `/api/Config` endpoint is intentionally public (no auth required) because it only exposes non-sensitive configuration data needed by the frontend before user interaction.

See **[Documents/UserGuide/USING_THE_API.md](Documents/UserGuide/USING_THE_API.md)** for complete authentication details and examples.

---

### 4. In-Memory Processing

All uploaded files are processed in memory and discarded immediately after processing to:

- Reduce attack surface
- Avoid file system vulnerabilities
- Simplify deployment and evaluation

---

### 5. Simplified Architecture

A minimal layered structure was chosen:

- Controller layer for HTTP handling
- Service layer for business logic
- Isolated parsing and reporting services

This avoids over-engineering patterns (CQRS, MediatR, etc.) that are unnecessary for the problem scope.

---

### 6. FluentValidation Not Used

**Decision:** Inline validation with `DataAnnotations` and explicit service-level checks.

**Why:**
- For this challenge scope, inline validation is sufficient and more transparent
- FluentValidation would add dependency weight without proportional value
- Validation logic is simple enough to keep in-line with models and services

**Trade-off:** FluentValidation would be beneficial in larger applications where:
- Complex, multi-step validation rules exist
- Validation needs to be shared across layers
- Custom error messages and localization are required

---

### 7. No InterpolatedLogging

**Decision:** Standard `ILogger` with structured logging placeholders (`{Parameter}`).

**Why:**
- Standard Microsoft pattern, universally understood
- No additional dependencies
- Performance overhead avoided

**Alternative considered:** [InterpolatedLogging](https://github.com/Drizin/InterpolatedLogging)
- Improves maintainability by using C# string interpolation for structured logs
- Makes structured logging more intuitive and less error-prone
- **Not used** because it's non-standard and introduces minor performance cost

**Trade-off:** Standard placeholders require careful alignment between format string and arguments. Interpolated logging would eliminate this friction for teams prioritizing maintainability over convention.

---

### 8. No Functional Monads (Try/Option)

**Decision:** Traditional exception handling and nullable reference types.

**Why:**
- Standard .NET patterns, familiar to all C# developers
- No external dependencies
- Exceptions are well-understood and debuggable

**Alternative considered:** [LanguageExt](https://github.com/louthy/language-ext) monads (`Try<T>`, `Option<T>`)
- Eliminates exceptions-as-control-flow anti-pattern
- Makes error paths explicit in type signatures
- Improves composability and testability for functional-style code

**Not used** because:
- Non-standard in mainstream .NET development
- Steep learning curve for developers unfamiliar with functional programming
- Overkill for a coding challenge context

**Trade-off:** Monadic error handling improves maintainability for teams comfortable with functional programming, but adds conceptual overhead for traditional C# developers.

---

### 9. Interface-Based Dependency Injection

**Decision:** Interfaces for all services (`ICsvParserService`, `IVatCalculationService`, etc.).

**Why:**
- Common .NET convention, expected by most teams
- Enables easy mocking in unit tests
- Familiar to reviewers evaluating the code

**Alternative considered:** Virtual classes with [Fody.Virtuosity](https://github.com/Fody/Virtuosity)
- Eliminates interface boilerplate
- Auto-makes methods virtual for mocking without manual `virtual` keywords
- Reduces "interface tax" where every service has a single implementation

**Not used** because:
- Non-standard approach, adds dependency on Fody
- Interfaces are widely understood and expected in .NET codebases
- Challenge scope doesn't justify introducing unfamiliar tooling

**Trade-off:** Interfaces add boilerplate (one extra file per service), but provide clarity and follow established patterns. Virtual classes + Virtuosity would reduce clutter but confuse reviewers unfamiliar with the tooling.

---

## Running the Application

### Backend

```bash
dotnet restore
dotnet run --project HungarianVatDeclarationGenerator.Api
```

### Frontend

```bash
cd HungarianVatDeclarationGenerator.Web
npm install
npm run dev
```

---

## Running Tests

```bash
dotnet test
```

---

## API Authentication

⚠️ **Demo-only API key authentication is implemented.** See **[Documents/UserGuide/USING_THE_API.md](Documents/UserGuide/USING_THE_API.md)** for complete details.

**Quick reference:**
- Header: `X-API-Key`
- Value: `challenge-demo-key-2024`
- Public endpoint: `/api/Config` (no auth required)
- Protected endpoints: `/api/VatDeclaration/*` (auth required)

---

## API Endpoints

### Get Configuration (Public)

```http
GET /api/Config
```

Returns upload limits and allowed file types. **No authentication required.**

### Generate VAT Summary

```http
POST /api/VatDeclaration/upload
Header: X-API-Key: challenge-demo-key-2024
```

Accepts a CSV file upload and returns a structured VAT summary.

### Generate PDF Report

```http
POST /api/VatDeclaration/upload-and-generate-pdf
Header: X-API-Key: challenge-demo-key-2024
```

Accepts a CSV file upload and returns a PDF document.

---

## AI-Assisted Development

The file `ai_log.md` contains the complete, unedited AI-assisted development history used during implementation. It includes prompts, iterations, debugging steps, and security-related discussions.

---

## Future Improvements

Potential enhancements if this were extended beyond a coding challenge:

- Authentication and authorization
- Audit logging
- Support for additional VAT categories
- Excel file support
- Cloud storage integration
- CI/CD pipeline with automated deployment
