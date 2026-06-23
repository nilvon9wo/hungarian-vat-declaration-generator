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

See **[USING_THE_API.md](USING_THE_API.md)** for complete authentication details and examples.

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

## Running the Application

### Backend

```bash
dotnet restore
dotnet run --project HungarianVatDeclarationGenerator.Api
```

### Frontend

```bash
cd frontend
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

⚠️ **Demo-only API key authentication is implemented.** See **[USING_THE_API.md](USING_THE_API.md)** for complete details.

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
