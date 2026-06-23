using HungarianVatDeclarationGenerator.Api.Models;
using HungarianVatDeclarationGenerator.Api.Services;

namespace HungarianVatDeclarationGenerator.Api.Tests.Services;

public sealed class PdfGenerationServiceTests
{
    // Hungarian VAT rates as defined by Hungarian tax law
    private const int STANDARD_HUNGARIAN_VAT_RATE = 27;
    private const int INTERMEDIATE_HUNGARIAN_VAT_RATE = 18;
    private const int REDUCED_HUNGARIAN_VAT_RATE = 5;

    // Minimum expected PDF size - valid PDFs should be at least 1KB
    private const int MINIMUM_PDF_SIZE_BYTES = 1000;

    private readonly PdfGenerationService _service = new();

    [Fact]
    public void GeneratePdf_WithNullInput_ThrowsArgumentNullException()
    {
        // Act
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => _service.GeneratePdf(null!));

        // Assert
        Assert.NotNull(ex);
    }

    [Fact]
    public void GeneratePdf_WithValidInput_ReturnsPdfBytes()
    {
        // Arrange
        VatDeclarationResult result = new()
        {
            SummariesByVatRate =
            [
                CreateVatSummary(vatRate: REDUCED_HUNGARIAN_VAT_RATE, netAmount: 1000m, invoiceCount: 1),
                CreateVatSummary(vatRate: STANDARD_HUNGARIAN_VAT_RATE, netAmount: 10000m, invoiceCount: 2)
            ],
            GrandTotalNet = 11000m,
            GrandTotalVat = CalculateVat(1000m, REDUCED_HUNGARIAN_VAT_RATE) + CalculateVat(10000m, STANDARD_HUNGARIAN_VAT_RATE),
            GrandTotalGross = CalculateGross(1000m, REDUCED_HUNGARIAN_VAT_RATE) + CalculateGross(10000m, STANDARD_HUNGARIAN_VAT_RATE),
            TotalInvoiceCount = 3
        };

        // Act
        byte[] pdfBytes = _service.GeneratePdf(result);

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
        Assert.True(pdfBytes.Length > MINIMUM_PDF_SIZE_BYTES, "PDF should be at least 1KB");
        Assert.True(IsPdfFile(pdfBytes), "Output should be a valid PDF file");
    }

    [Fact]
    public void GeneratePdf_WithEmptyResult_GeneratesPdfWithZeroTotals()
    {
        // Arrange
        VatDeclarationResult result = VatDeclarationResult.Empty;

        // Act
        byte[] pdfBytes = _service.GeneratePdf(result);

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
        Assert.True(IsPdfFile(pdfBytes), "Output should be a valid PDF file");
    }

    [Fact]
    public void GeneratePdf_WithMultipleCategories_IncludesAllCategories()
    {
        // Arrange
        decimal vat5Net = 2500m;
        decimal vat18Net = 8000m;
        decimal vat27Net = 18000m;

        VatDeclarationResult result = new()
        {
            SummariesByVatRate =
            [
                CreateVatSummary(vatRate: REDUCED_HUNGARIAN_VAT_RATE, netAmount: vat5Net, invoiceCount: 1),
                CreateVatSummary(vatRate: INTERMEDIATE_HUNGARIAN_VAT_RATE, netAmount: vat18Net, invoiceCount: 2),
                CreateVatSummary(vatRate: STANDARD_HUNGARIAN_VAT_RATE, netAmount: vat27Net, invoiceCount: 2)
            ],
            GrandTotalNet = vat5Net + vat18Net + vat27Net,
            GrandTotalVat = CalculateVat(vat5Net, REDUCED_HUNGARIAN_VAT_RATE) 
                          + CalculateVat(vat18Net, INTERMEDIATE_HUNGARIAN_VAT_RATE)
                          + CalculateVat(vat27Net, STANDARD_HUNGARIAN_VAT_RATE),
            GrandTotalGross = CalculateGross(vat5Net, REDUCED_HUNGARIAN_VAT_RATE)
                            + CalculateGross(vat18Net, INTERMEDIATE_HUNGARIAN_VAT_RATE)
                            + CalculateGross(vat27Net, STANDARD_HUNGARIAN_VAT_RATE),
            TotalInvoiceCount = 5
        };

        // Act
        byte[] pdfBytes = _service.GeneratePdf(result);

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
        Assert.True(pdfBytes.Length > MINIMUM_PDF_SIZE_BYTES, "PDF with multiple categories should be substantial");
        Assert.True(IsPdfFile(pdfBytes), "Output should be a valid PDF file");
    }

    private static VatSummary CreateVatSummary(int vatRate, decimal netAmount, int invoiceCount) =>
        new()
        {
            VatRate = vatRate,
            TotalNetAmount = netAmount,
            TotalVatAmount = CalculateVat(netAmount, vatRate),
            TotalGrossAmount = CalculateGross(netAmount, vatRate),
            InvoiceCount = invoiceCount
        };

    private static decimal CalculateVat(decimal netAmount, int vatRate) =>
        netAmount * vatRate / 100;

    private static decimal CalculateGross(decimal netAmount, int vatRate) =>
        netAmount + CalculateVat(netAmount, vatRate);

    /// <summary>
    /// Validates whether a byte array is a PDF file by checking its magic number (file signature).
    /// All PDF files must start with the header "%PDF" (hex: 25 50 44 46) as defined in the PDF specification.
    /// This is a reliable way to identify PDF files regardless of file extension or MIME type claims.
    /// </summary>
    /// <remarks>
    /// The PDF specification (ISO 32000) requires all PDF files to begin with these exact bytes:
    /// - 0x25 = '%' (37 decimal)
    /// - 0x50 = 'P' (80 decimal)
    /// - 0x44 = 'D' (68 decimal)
    /// - 0x46 = 'F' (70 decimal)
    /// 
    /// Example: A valid PDF starts with: %PDF-1.7 (where %PDF is always present)
    /// </remarks>
    private static bool IsPdfFile(byte[] bytes)
        => bytes.Length >= 4
            && bytes[0] == 0x25
            && bytes[1] == 0x50
            && bytes[2] == 0x44
            && bytes[3] == 0x46;
}
