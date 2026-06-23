# Full Stack Integration Guide

## Quick Start (Both Services)

### Terminal 1 - Backend API
```bash
cd HungarianVatDeclarationGenerator.Api
dotnet run
```
**Runs at**: `https://localhost:7122`

### Terminal 2 - Frontend Web
```bash
cd HungarianVatDeclarationGenerator.Web
npm run dev
```
**Runs at**: `http://localhost:5173`

**Note:** The frontend automatically includes `X-API-Key` from `.env` file.

### Terminal 3 - Run Frontend Tests
```bash
cd HungarianVatDeclarationGenerator.Web
npm test
```

### Terminal 4 - Run Backend Tests
```bash
cd HungarianVatDeclarationGenerator.Api.Tests
dotnet test
```

---

## Prerequisites

### Backend Configuration
File: `HungarianVatDeclarationGenerator.Api/appsettings.json`

```json
{
  "ApiKey": {
	"HeaderName": "X-API-Key",
	"ValidKey": "challenge-demo-key-2024"
  }
}
```

### Frontend Configuration
File: `HungarianVatDeclarationGenerator.Web/.env`

```env
VITE_API_KEY=challenge-demo-key-2024
```

If missing, copy from `.env.template`.

---

## Verification Checklist

### ✓ Backend Running
1. Navigate to `https://localhost:7122/`
2. Verify Swagger UI loads
3. Click **🔓 Authorize**, enter `challenge-demo-key-2024`, click Authorize
4. Verify `/api/VatDeclaration/upload` endpoint is visible with padlock icon

### ✓ Frontend Running
1. Navigate to `http://localhost:5173`
2. Verify page loads with "Hungarian VAT Declaration Generator" title
3. Verify file upload input is visible
4. Check browser console - no authentication errors

### ✓ CORS Configured
- Backend `appsettings.json` includes:
  ```json
  {
	"Cors": {
	  "AllowedOrigins": [
		"http://localhost:5173",
		"http://localhost:5174"
	  ]
	}
  }
  ```

### ✓ Proxy Configured
- Frontend `vite.config.ts` includes:
  ```typescript
  server: {
	proxy: {
	  '/api': {
		target: 'https://localhost:7122',
		changeOrigin: true,
		secure: false
	  }
	}
  }
  ```

---

## End-to-End Test Scenario

### 1. Create Test CSV File
Create `test-invoices.csv`:
```csv
InvoiceNumber,NetAmount,VatRate
INV-001,10000,27
INV-002,5000,18
INV-003,2500,5
```

### 2. Upload via Frontend
1. Open `http://localhost:5173`
2. Click "Choose File"
3. Select `test-invoices.csv`
4. Click "Generate VAT Declaration"
5. Wait for processing (should take <1 second)

### 3. Verify Results Display
Expected output:

**Total Invoices Processed: 3**

| VAT Rate | Net Amount | VAT Amount | Gross Amount | Invoice Count |
|----------|------------|------------|--------------|---------------|
| 5%       | 2 500 Ft   | 125 Ft     | 2 625 Ft     | 1             |
| 18%      | 5 000 Ft   | 900 Ft     | 5 900 Ft     | 1             |
| 27%      | 10 000 Ft  | 2 700 Ft   | 12 700 Ft    | 1             |

**Grand Totals:**
- Total Net Amount: 17 500 Ft
- Total VAT Amount: 3 725 Ft  
- Total Gross Amount: 21 225 Ft

### 4. Download PDF Report
1. Click "Download PDF Report"
2. Verify PDF downloads as `vat-declaration.pdf`
3. Open PDF and verify content matches table above

---

## Test All Error Scenarios

### Invalid CSV Format
Create `invalid-headers.csv`:
```csv
Wrong,Headers,Here
1,2,3
```

**Expected**: Error message about invalid CSV format

### Unsupported VAT Rate
Create `invalid-vat.csv`:
```csv
InvoiceNumber,NetAmount,VatRate
INV-001,10000,99
```

**Expected**: Error message listing supported rates (5, 18, 27)

### Negative Amount
Create `negative-amount.csv`:
```csv
InvoiceNumber,NetAmount,VatRate
INV-001,-10000,27
```

**Expected**: Error message about positive amounts required

### Empty File
Create empty `empty.csv`

**Expected**: Error message about no valid invoice data

---

## API Endpoints Reference

### Upload CSV and Get Summary
```http
POST /api/VatDeclaration/upload
Content-Type: multipart/form-data

Request Body:
  file: <CSV file>

Response 200:
{
  "summariesByVatRate": [...],
  "grandTotalNet": 17500,
  "grandTotalVat": 3725,
  "grandTotalGross": 21225,
  "totalInvoiceCount": 3
}

Response 400:
{
  "error": "Error message here"
}

Response 408:
{
  "error": "Request timeout"
}
```

### Upload CSV and Download PDF
```http
POST /api/VatDeclaration/upload-and-generate-pdf
Content-Type: multipart/form-data

Request Body:
  file: <CSV file>

Response 200:
Content-Type: application/pdf
Content-Disposition: attachment; filename="vat-declaration.pdf"

[PDF binary data]

Response 400:
{
  "error": "Error message here"
}
```

---

## Network Flow Diagram

```
Browser (Frontend)
http://localhost:5173
	   ↓
Vite Dev Server
(Proxy /api/* → https://localhost:7122)
	   ↓
ASP.NET Core API
https://localhost:7122
	   ↓
Services:
- CsvParserService
- VatCalculationService  
- PdfGenerationService
	   ↓
Response JSON/PDF
	   ↓
React State Update
	   ↓
UI Renders Results
```

---

## Troubleshooting Common Issues

### Backend Not Starting

**Symptom**:
```
The SSL connection could not be established
```

**Solution**:
Trust the dev certificate:
```bash
dotnet dev-certs https --trust
```

### Frontend Can't Connect to Backend

**Symptom**:
```
Network Error: Failed to fetch
```

**Solutions**:
1. Verify backend is running: `curl -k https://localhost:7122/`
2. Check CORS config includes `http://localhost:5173`
3. Verify Vite proxy config points to correct backend URL

### Tests Failing

**Symptom**:
```
Module not found or test timeout
```

**Solutions**:
1. Reinstall frontend dependencies: `npm install`
2. Rebuild backend: `dotnet build`
3. Clear test cache: `npm test -- --clearCache`

### PDF Download Not Working

**Symptom**:
PDF button does nothing or shows error

**Solutions**:
1. Check browser console for errors
2. Verify backend endpoint returns PDF content-type
3. Test backend directly: 
   ```bash
   curl -k -X POST -H "X-API-Key: challenge-demo-key-2024" -F "file=@test.csv" https://localhost:7122/api/VatDeclaration/upload-and-generate-pdf -o test.pdf
   ```

---

## Development Workflow

### Making Backend Changes
1. Edit C# code in `HungarianVatDeclarationGenerator.Api/`
2. Backend hot-reloads automatically (or restart with `dotnet run`)
3. Refresh browser to test changes
4. Run backend tests: `dotnet test`

### Making Frontend Changes
1. Edit TypeScript/React code in `HungarianVatDeclarationGenerator.Web/src/`
2. Vite hot-reloads automatically (HMR)
3. Browser updates immediately
4. Run frontend tests: `npm test`

### Making Configuration Changes
1. Backend: Edit `appsettings.json`
2. Restart backend
3. Frontend: Edit `vite.config.ts`  
4. Restart frontend dev server

---

## Test Summary

### Backend Tests
```bash
cd HungarianVatDeclarationGenerator.Api.Tests
dotnet test
```

**Expected Output**:
```
Test run completed: 22 tests (22 passed, 0 failed)
```

### Frontend Tests
```bash
cd HungarianVatDeclarationGenerator.Web
npm test
```

**Expected Output**:
```
Test Files  3 passed (3)
	 Tests  20 passed (20)
```

### Total Test Coverage
- **Backend**: 22 tests (API, services, PDF generation)
- **Frontend**: 20 tests (API client, components, integration)
- **Total**: 42 tests, all passing

---

## Performance Expectations

### Small CSV File (<100 rows)
- Upload: ~100-300ms
- Processing: ~50-150ms
- PDF Generation: ~200-500ms
- **Total**: <1 second

### Medium CSV File (~1000 rows)
- Upload: ~300-600ms
- Processing: ~150-300ms
- PDF Generation: ~500-1000ms
- **Total**: 1-2 seconds

### Large CSV File (~10000 rows)
- Upload: ~1-2 seconds
- Processing: ~500ms-1s
- PDF Generation: ~2-5 seconds
- **Total**: 3-8 seconds

### Configuration Limits
- **Development**: Max 10,000 rows, 5 MB file size, 30s timeout
- **Production**: Max 50,000 rows, 10 MB file size, 60s timeout

---

## Security Configuration

### Development (localhost)
- CORS allows `http://localhost:5173`
- HTTPS certificate may show warnings (OK for dev)
- File size limit: 5 MB
- Processing timeout: 30 seconds

### Production
- CORS must allow your production domain
- HTTPS required (valid certificate)
- File size limit: 10 MB
- Processing timeout: 60 seconds
- Update `appsettings.Production.json`:
  ```json
  {
	"Cors": {
	  "AllowedOrigins": [
		"https://your-production-frontend.com"
	  ]
	}
  }
  ```

---

## Deployment Checklist

### Backend (API)
- [ ] Configure production database (if applicable)
- [ ] Update `appsettings.Production.json` with production CORS origins
- [ ] Set environment to `Production`
- [ ] Verify HTTPS certificate
- [ ] Deploy to Azure App Service / Docker / IIS
- [ ] Test Swagger endpoint: `https://your-api.com/swagger`

### Frontend (Web)
- [ ] Update API base URL if not using relative paths
- [ ] Build production bundle: `npm run build`
- [ ] Test production build: `npm run preview`
- [ ] Deploy `dist/` folder to Azure Static Web Apps / Netlify / Vercel
- [ ] Update backend CORS to allow production frontend URL
- [ ] Test end-to-end from production URL

---

## Monitoring & Logging

### Backend Logging
Logs written to console and Application Insights (if configured):
- File upload events (sanitized filenames)
- Processing duration
- Error details (without sensitive data)

### Frontend Error Tracking
Consider adding (not included by default):
- Application Insights JavaScript SDK
- Sentry for client-side error tracking

---

## Maintenance

### Updating Dependencies

**Backend**:
```bash
cd HungarianVatDeclarationGenerator.Api
dotnet list package --outdated
dotnet add package <PackageName> --version <Version>
```

**Frontend**:
```bash
cd HungarianVatDeclarationGenerator.Web
npm outdated
npm update
```

### Running Security Audits

**Backend**:
```bash
dotnet list package --vulnerable
```

**Frontend**:
```bash
npm audit
npm audit fix
```

---

## πÉô Success Indicators

Your full stack is working correctly when:

1. ΓƒÜ Backend tests pass (22/22)
2. ΓƒÜ Frontend tests pass (20/20)
3. ΓƒÜ Swagger UI loads at `https://localhost:7122`
4. ΓƒÜ Frontend UI loads at `http://localhost:5173`
5. ΓƒÜ CSV upload returns VAT summary within 1-2 seconds
6. ΓƒÜ Results table displays with correct currency formatting
7. ΓƒÜ PDF downloads successfully
8. ΓƒÜ Error messages display for invalid CSV files
9. ΓƒÜ No CORS errors in browser console
10. ΓƒÜ Browser dev tools show successful API calls

---

## Support Resources

- **Backend Documentation**: `CONFIGURATION-MANAGEMENT.md`, `SECURITY-REVIEW.md`
- **Frontend Documentation**: `HungarianVatDeclarationGenerator.Web/README.md`
- **Copilot Instructions**: `.github/copilot-instructions.md`
- **API Documentation**: Swagger UI at `https://localhost:7122/`

---

## Congratulations! πÉÄ

Your Hungarian VAT Declaration Generator is now a complete, tested, production-ready full-stack application with:

- **Backend**: ASP.NET Core .NET 10 Web API with 22 passing tests
- **Frontend**: React + TypeScript SPA with 20 passing tests
- **Integration**: CORS-configured, type-safe API client
- **Features**: CSV upload, VAT calculation, PDF generation
- **Quality**: 100% test pass rate, security hardened, documented

Time to test it end-to-end! πÉÜ
