using HungarianVatDeclarationGenerator.Api.Models;
using HungarianVatDeclarationGenerator.Api.Services;

namespace HungarianVatDeclarationGenerator.Api.Tests.Services;

public sealed class VatCalculationServiceTests
{
    private readonly VatCalculationService _service = new();

    [Fact]
    public void Calculate_WithEmptyInvoiceList_ReturnsZeroTotals()
    {
        // Arrange
        Invoice[] invoices = [];

        // Act
        VatDeclarationResult result = _service.Calculate(invoices);

        // Assert
        Assert.Empty(result.SummariesByVatRate);
        Assert.Equal(0m, result.GrandTotalNet);
        Assert.Equal(0m, result.GrandTotalVat);
        Assert.Equal(0m, result.GrandTotalGross);
        Assert.Equal(0, result.TotalInvoiceCount);
    }

    [Fact]
    public void Calculate_WithSingleInvoice_ReturnsCorrectTotals()
    {
        // Arrange
        List<Invoice> invoices =
        [
            new() { InvoiceNumber = "INV-001", NetAmount = 10000m, VatRate = 27 }
        ];

        // Act
        VatDeclarationResult result = _service.Calculate(invoices);

        // Assert
        Assert.Single(result.SummariesByVatRate);
        VatSummary summary = result.SummariesByVatRate[0];
        Assert.Equal(27, summary.VatRate);
        Assert.Equal(10000m, summary.TotalNetAmount);
        Assert.Equal(2700m, summary.TotalVatAmount);
        Assert.Equal(12700m, summary.TotalGrossAmount);
        Assert.Equal(1, summary.InvoiceCount);

        Assert.Equal(10000m, result.GrandTotalNet);
        Assert.Equal(2700m, result.GrandTotalVat);
        Assert.Equal(12700m, result.GrandTotalGross);
        Assert.Equal(1, result.TotalInvoiceCount);
    }

    [Fact]
    public void Calculate_WithMultipleVatRates_GroupsCorrectly()
    {
        // Arrange
        List<Invoice> invoices =
        [
            new() { InvoiceNumber = "INV-001", NetAmount = 10000m, VatRate = 27 },
            new() { InvoiceNumber = "INV-002", NetAmount = 5000m, VatRate = 18 },
            new() { InvoiceNumber = "INV-003", NetAmount = 2500m, VatRate = 5 },
            new() { InvoiceNumber = "INV-004", NetAmount = 8000m, VatRate = 27 },
            new() { InvoiceNumber = "INV-005", NetAmount = 3000m, VatRate = 18 }
        ];

        // Act
        VatDeclarationResult result = _service.Calculate(invoices);

        // Assert
        Assert.Equal(3, result.SummariesByVatRate.Count);

        // Verify 5% group
        VatSummary vat5 = result.SummariesByVatRate.First(s => s.VatRate == 5);
        Assert.Equal(2500m, vat5.TotalNetAmount);
        Assert.Equal(125m, vat5.TotalVatAmount);
        Assert.Equal(2625m, vat5.TotalGrossAmount);
        Assert.Equal(1, vat5.InvoiceCount);

        // Verify 18% group
        VatSummary vat18 = result.SummariesByVatRate.First(s => s.VatRate == 18);
        Assert.Equal(8000m, vat18.TotalNetAmount);
        Assert.Equal(1440m, vat18.TotalVatAmount);
        Assert.Equal(9440m, vat18.TotalGrossAmount);
        Assert.Equal(2, vat18.InvoiceCount);

        // Verify 27% group
        VatSummary vat27 = result.SummariesByVatRate.First(s => s.VatRate == 27);
        Assert.Equal(18000m, vat27.TotalNetAmount);
        Assert.Equal(4860m, vat27.TotalVatAmount);
        Assert.Equal(22860m, vat27.TotalGrossAmount);
        Assert.Equal(2, vat27.InvoiceCount);

        // Verify grand totals
        Assert.Equal(28500m, result.GrandTotalNet);
        Assert.Equal(6425m, result.GrandTotalVat);
        Assert.Equal(34925m, result.GrandTotalGross);
        Assert.Equal(5, result.TotalInvoiceCount);
    }

    [Fact]
    public void Calculate_OrdersSummariesByVatRate()
    {
        // Arrange
        List<Invoice> invoices =
        [
            new() { InvoiceNumber = "INV-001", NetAmount = 1000m, VatRate = 27 },
            new() { InvoiceNumber = "INV-002", NetAmount = 1000m, VatRate = 5 },
            new() { InvoiceNumber = "INV-003", NetAmount = 1000m, VatRate = 18 }
        ];

        // Act
        VatDeclarationResult result = _service.Calculate(invoices);

        // Assert
        Assert.Equal(3, result.SummariesByVatRate.Count);
        Assert.Equal(5, result.SummariesByVatRate[0].VatRate);
        Assert.Equal(18, result.SummariesByVatRate[1].VatRate);
        Assert.Equal(27, result.SummariesByVatRate[2].VatRate);
    }
}
