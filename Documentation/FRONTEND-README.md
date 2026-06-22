# Hungarian VAT Declaration Generator - Frontend

A minimal React + TypeScript frontend for uploading CSV files and displaying VAT declaration summaries.

## Tech Stack

- **React 19** - UI framework
- **TypeScript 6** - Type safety
- **Vite 8** - Build tool and dev server
- **Vitest 3** - Unit testing
- **React Testing Library** - Component testing

## Architecture

### Single-Page Application
- No routing (single view)
- Minimal state management (useState only)
- Direct fetch API calls (no React Query, no Redux)

### Components
- **App.tsx** - Main component handling:
  - File upload form
  - Loading states
  - Error display
  - Results table
  - PDF download

### API Integration
- **services/api.ts** - API client with two endpoints:
  - `uploadCsvFile(file)` - Upload CSV and get VAT summary
  - `downloadPdf(file)` - Upload CSV and download PDF report

### Type Definitions
- **types/api.ts** - TypeScript interfaces matching backend models:
  - `VatSummary`
  - `VatDeclarationResult`
  - `ApiError`

## API Contract

### Backend Endpoint
```
POST /api/VatDeclaration/upload
Content-Type: multipart/form-data
Field name: "file"
```

### Expected Response
```json
{
  "summariesByVatRate": [
	{
	  "vatRate": 27,
	  "totalNetAmount": 10000,
	  "totalVatAmount": 2700,
	  "totalGrossAmount": 12700,
	  "invoiceCount": 1
	}
  ],
  "grandTotalNet": 10000,
  "grandTotalVat": 2700,
  "grandTotalGross": 12700,
  "totalInvoiceCount": 1
}
```

### Error Response
```json
{
  "error": "CSV file contains no valid invoice data..."
}
```

## CORS Configuration

The frontend runs on `http://localhost:5173` (Vite default).

### Development Proxy
Vite proxy configuration forwards `/api/*` requests to `https://localhost:5001`:

```typescript
// vite.config.ts
server: {
  proxy: {
	'/api': {
	  target: 'https://localhost:5001',
	  changeOrigin: true,
	  secure: false
	}
  }
}
```

### Backend CORS Requirements
The ASP.NET Core backend must allow `http://localhost:5173` in CORS configuration:

```json
// appsettings.json
{
  "Cors": {
	"AllowedOrigins": [
	  "http://localhost:5173"
	]
  }
}
```

## Setup and Installation

### Prerequisites
- Node.js 20+ 
- npm or yarn

### Install Dependencies
```bash
cd HungarianVatDeclarationGenerator.Web
npm install
```

### Start Development Server
```bash
npm run dev
```

Application runs at: `http://localhost:5173`

### Build for Production
```bash
npm run build
```

Output directory: `dist/`

## Testing

### Run Unit Tests
```bash
npm test
```

### Run Tests with UI
```bash
npm run test:ui
```

### Run Tests with Coverage
```bash
npm run test:coverage
```

### Test Coverage

#### Unit Tests (`api.test.ts`)
- ✅ Successful file upload and result parsing
- ✅ HTTP error response handling (400, 500)
- ✅ Network error handling
- ✅ FormData field name validation
- ✅ PDF download success
- ✅ PDF download error handling

#### Component Tests (`App.test.tsx`)
- ✅ Render upload form
- ✅ Enable/disable submit button based on file selection
- ✅ Display validation errors (no file selected)
- ✅ Display results after successful upload
- ✅ Display error messages on upload failure
- ✅ Show loading state during processing
- ✅ Handle PDF download
- ✅ Currency formatting (HUF)

#### Integration Tests (`integration.test.ts`)
- ✅ End-to-end CSV upload flow
- ✅ Invalid CSV header rejection
- ✅ Non-CSV file rejection
- ✅ Multipart form data handling
- ✅ Large file handling

## Data Flow

```
User selects CSV file
	   ↓
User clicks "Generate VAT Declaration"
	   ↓
FormData created with file (field name: "file")
	   ↓
POST /api/VatDeclaration/upload
	   ↓
Backend processes CSV
	   ↓
JSON response with VatDeclarationResult
	   ↓
React state updated with result
	   ↓
Table rendered with VAT summaries and totals
	   ↓
User clicks "Download PDF Report" (optional)
	   ↓
POST /api/VatDeclaration/upload-and-generate-pdf
	   ↓
Backend generates PDF
	   ↓
Blob response triggers browser download
```

## Error Handling

### Validation Errors
- No file selected → Client-side validation
- Invalid file type → Backend validation (400)
- Invalid CSV format → Backend validation (400)
- Invalid VAT rates → Backend validation (400)

### Network Errors
- Connection timeout → Caught and displayed
- Server unavailable → Caught and displayed
- Request cancelled → Caught and displayed

### Display Strategy
All errors are displayed in a red alert box below the upload form with:
- Clear error message from backend
- User-friendly fallback for network errors
- Error state cleared when new file is selected

## Currency Formatting

Amounts are formatted using the Hungarian locale:
```typescript
new Intl.NumberFormat('hu-HU', {
  style: 'currency',
  currency: 'HUF',
  minimumFractionDigits: 0,
  maximumFractionDigits: 0,
}).format(value)
```

Example output: `12 700 Ft`

## Styling

Basic CSS with:
- Responsive layout (max-width: 1200px)
- Clean table styling with hover effects
- Color-coded sections (upload, results, totals)
- Accessible form controls
- Loading states and disabled states

No external CSS frameworks (intentionally minimal).

## Browser Support

- Chrome/Edge (latest)
- Firefox (latest)
- Safari (latest)

Uses modern JavaScript features:
- `fetch` API
- `FormData`
- `Blob` and `URL.createObjectURL`
- ES2020+ syntax

## Deployment Considerations

### Environment Variables
No environment variables required - API URL is relative (`/api`) and proxied in development.

### Production Build
For production, ensure:
1. Backend API is accessible from frontend domain
2. CORS allows the production frontend origin
3. HTTPS is configured for both frontend and backend

### Static Hosting
The built frontend can be hosted on:
- Azure Static Web Apps
- Netlify
- Vercel
- Any static file server

The backend API must be separately deployed and CORS-configured.

## Future Enhancements (Out of Scope)

These were intentionally excluded to keep the solution minimal:

- ❌ Client-side CSV validation
- ❌ File size validation before upload
- ❌ Progress indicators for large files
- ❌ Retry logic for failed uploads
- ❌ Local state persistence
- ❌ Multiple file uploads
- ❌ CSV preview before upload
- ❌ Advanced error recovery
- ❌ Internationalization (i18n)
- ❌ Dark mode
- ❌ Accessibility enhancements beyond basic ARIA

The current implementation focuses on **correctness**, **working end-to-end flow**, and **testability** over feature richness.
