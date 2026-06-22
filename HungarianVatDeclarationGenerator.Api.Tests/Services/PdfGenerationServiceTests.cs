using HungarianVatDeclarationGenerator.Api.Models;
using HungarianVatDeclarationGenerator.Api.Services;

namespace HungarianVatDeclarationGenerator.Api.Tests.Services;

public sealed class PdfGenerationServiceTests
{
    private readonly PdfGenerationService _service = new();

    [Fact]
    public void GeneratePdf_WithNullInput_ThrowsArgumentNullException() =>
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.GeneratePdf(null!));

    [Fact]
    public void GeneratePdf_WithValidInput_ReturnsPdfBytes()
    {
        // Arrange
        VatDeclarationResult result = new()
        {
            SummariesByVatRate =
            [
                new VatSummary
                {
                    VatRate = 5,
                    TotalNetAmount = 1000m,
                    TotalVatAmount = 50m,
                    TotalGrossAmount = 1050m,
                    InvoiceCount = 1
                },
                new VatSummary
                {
                    VatRate = 27,
                    TotalNetAmount = 10000m,
                    TotalVatAmount = 2700m,
                    TotalGrossAmount = 12700m,
                    InvoiceCount = 2
                }
            ],
            GrandTotalNet = 11000m,
            GrandTotalVat = 2750m,
            GrandTotalGross = 13750m,
            TotalInvoiceCount = 3
        };

        // Act
        byte[] pdfBytes = _service.GeneratePdf(result);

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
        Assert.True(pdfBytes.Length > 1000, "PDF should be at least 1KB");
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
        VatDeclarationResult result = new()
        {
            SummariesByVatRate =
            [
                new VatSummary
                {
                    VatRate = 5,
                    TotalNetAmount = 2500m,
                    TotalVatAmount = 125m,
                    TotalGrossAmount = 2625m,
                    InvoiceCount = 1
                },
                new VatSummary
                {
                    VatRate = 18,
                    TotalNetAmount = 8000m,
                    TotalVatAmount = 1440m,
                    TotalGrossAmount = 9440m,
                    InvoiceCount = 2
                },
                new VatSummary
                {
                    VatRate = 27,
                    TotalNetAmount = 18000m,
                    TotalVatAmount = 4860m,
                    TotalGrossAmount = 22860m,
                    InvoiceCount = 2
                }
            ],
            GrandTotalNet = 28500m,
            GrandTotalVat = 6425m,
            GrandTotalGross = 34925m,
            TotalInvoiceCount = 5
        };

        // Act
        byte[] pdfBytes = _service.GeneratePdf(result);

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
        Assert.True(pdfBytes.Length > 1000, "PDF with multiple categories should be substantial");
        Assert.True(IsPdfFile(pdfBytes), "Output should be a valid PDF file");
    }

    private static bool IsPdfFile(byte[] bytes)
    {
        if (bytes.Length < 4)
        {
            return false;
        }

        return bytes[0] == 0x25 &&
               bytes[1] == 0x50 &&
               bytes[2] == 0x44 &&
               bytes[3] == 0x46;
    }
}
