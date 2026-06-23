# PDF Report Generation - Implementation Notes

## Overview
Simple PDF generation service using QuestPDF for Hungarian VAT Declaration reports.

## Library Choice
**QuestPDF 2026.6.0** - Selected for:
- ✅ Clean, fluent C# API
- ✅ No external dependencies (native .NET)
- ✅ Community license (free for open-source/evaluation)
- ✅ Excellent table support
- ✅ Professional output quality
- ✅ Well-documented and actively maintained

## Implementation: `PdfGenerationService`

### Design Approach
Single service class with focused helper methods following project standards:
- All methods under 10 lines
- Maximum 2 levels of nesting
- Clear, descriptive method names
- No over-engineering or abstraction layers

### PDF Structure

```
┌─────────────────────────────────────────┐
│  VAT Declaration Summary (Title)        │
├─────────────────────────────────────────┤
│  ┌─────────────────────────────────┐    │
│  │ VAT Rate | Net | VAT | Gross    │    │
│  ├─────────────────────────────────┤    │
│  │   5%     | ... | ... | ...      │    │
│  │  18%     | ... | ... | ...      │    │
│  │  27%     | ... | ... | ...      │    │
│  └─────────────────────────────────┘    │
├─────────────────────────────────────────┤
│  ┌─────────────────────────────────┐    │
│  │ Grand Total Net:        XX,XXX  │    │
│  │ Grand Total VAT:        X,XXX   │    │
│  │ Grand Total Gross:      XX,XXX  │    │
│  └─────────────────────────────────┘    │
└─────────────────────────────────────────┘
```

### Layout Components

**1. Page Configuration**
- Size: A4
- Margins: 2cm all sides
- Default font: 11pt

**2. Title Section**
- Text: "VAT Declaration Summary"
- Font: 20pt bold
- Padding: 20px bottom

**3. Category Table**
- Four columns: VAT Rate (2 units), Net Total (3 units), VAT Total (3 units), Gross Total (3 units)
- Header: Grey background, bold text, 8px padding
- Data cells: Border, 8px padding, light grey borders
- Currency formatting: `N2` (e.g., "12,345.67")

**4. Grand Totals Section**
- Border: 2px medium grey
- Padding: 15px
- Three rows: Net, VAT, Gross
- Right-aligned amounts, bold text, 12pt

### Method Breakdown

| Method | Purpose | Lines |
|--------|---------|-------|
| `GeneratePdf` | Entry point, null check, document creation | 9 |
| `ConfigurePage` | Page size, margins, default font | 6 |
| `AddTitle` | Title text and styling | 7 |
| `AddCategoryTable` | Table orchestration | 7 |
| `DefineTableColumns` | Column width ratios | 8 |
| `AddTableHeader` | Header row with styling | 9 |
| `AddTableRows` | Iterate and add data rows | 9 |
| `AddDataCell` | Single table cell with border/padding | 7 |
| `AddGrandTotals` | Totals section with border | 10 |
| `AddTotalLine` | Single total row (label + amount) | 10 |
| `FormatCurrency` | Decimal to string formatting | 2 |

All methods follow project standards: ≤10 lines, ≤2 nesting levels, clear names.

### Error Handling

**Input validation:**
```csharp
ArgumentNullException.ThrowIfNull(result);
```

**No silent failures:**
- Throws clear exception if input is null
- QuestPDF throws descriptive exceptions for rendering issues
- No try-catch swallowing errors

### Currency Formatting
Uses `N2` format: `amount.ToString("N2")`
- Two decimal places
- Thousands separator
- Example: `12345.67` → `"12,345.67"`

### QuestPDF License
```csharp
QuestPDF.Settings.License = LicenseType.Community;
```
Set to **Community** (free for open-source, evaluation, and learning).

For commercial use, requires Professional or Enterprise license.

## Testing

**4 comprehensive tests** (all passing ✅):

1. **Null Input** → Throws `ArgumentNullException`
2. **Valid Input** → Returns valid PDF byte array (>1KB, valid PDF header)
3. **Empty Result** → Generates PDF with zero totals (graceful handling)
4. **Multiple Categories** → Includes all VAT rate groups

**PDF Validation:**
- Checks PDF magic number (`%PDF` = `0x25504446`)
- Verifies minimum size (>1KB for realistic PDFs)
- Ensures byte array is not null or empty

## Integration

**Controller endpoint:**
```csharp
[HttpPost("upload-and-generate-pdf")]
public async Task<IActionResult> UploadCsvAndGeneratePdf(IFormFile file, ...)
{
	// Parse CSV
	IReadOnlyList<Invoice> invoices = await _csvParser.Parse(stream, cancellationToken);

	// Calculate VAT
	VatDeclarationResult result = _vatCalculator.Calculate(invoices);

	// Generate PDF ← Uses PdfGenerationService
	byte[] pdfBytes = _pdfGenerator.GeneratePdf(result);

	return File(pdfBytes, "application/pdf", "vat-declaration.pdf");
}
```

**DI Registration:**
```csharp
services.AddScoped<IPdfGenerationService, PdfGenerationService>();
```

## Output Sample

**For input:**
```csv
InvoiceNumber,NetAmount,VatRate
INV-001,10000,27
INV-002,5000,18
```

**Generates PDF with:**
```
VAT Declaration Summary

┌──────────┬─────────────┬─────────────┬──────────────┐
│ VAT Rate │   Net Total │   VAT Total │  Gross Total │
├──────────┼─────────────┼─────────────┼──────────────┤
│   18%    │    5,000.00 │     900.00  │    5,900.00  │
│   27%    │   10,000.00 │   2,700.00  │   12,700.00  │
└──────────┴─────────────┴─────────────┴──────────────┘

┌──────────────────────────────────────────┐
│ Grand Total Net:              15,000.00  │
│ Grand Total VAT:               3,600.00  │
│ Grand Total Gross:            18,600.00  │
└──────────────────────────────────────────┘
```

## Key Design Decisions

✅ **Single service class** - No complex rendering framework  
✅ **Fluent API** - QuestPDF's natural style  
✅ **Small helper methods** - Each under 10 lines  
✅ **Clear error handling** - Throws on null input  
✅ **Professional appearance** - Clean business layout  
✅ **Testable** - Validates PDF output structure  
✅ **Simple currency formatting** - .NET built-in `N2` format  
✅ **No premature optimization** - Straightforward implementation  

## Future Enhancements (Out of Scope)

- Custom fonts/branding
- Multi-page support (pagination)
- Charts/graphs
- Localization (HUF currency symbol)
- Company logo/header
- Digital signatures

Current implementation focuses on **simplicity, correctness, and professional appearance** appropriate for a coding challenge.
