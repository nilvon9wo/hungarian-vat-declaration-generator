using HungarianVatDeclarationGenerator.Api.Configuration;
using HungarianVatDeclarationGenerator.Api.Models;
using HungarianVatDeclarationGenerator.Api.Services;

namespace HungarianVatDeclarationGenerator.Api.Tests.Services;

public sealed class VatCalculationServiceTests
{
    // Hungarian VAT rates as defined by Hungarian tax law
    private const int STANDARD_HUNGARIAN_VAT_RATE = 27;
    private const int INTERMEDIATE_HUNGARIAN_VAT_RATE = 18;
    private const int REDUCED_HUNGARIAN_VAT_RATE = 5;

    // Sample invoice identifiers to demonstrate invoice format consistency
    private const string SAMPLE_INVOICE_NUMBER_001 = "INV-001";
    private const string SAMPLE_INVOICE_NUMBER_002 = "INV-002";
    private const string SAMPLE_INVOICE_NUMBER_003 = "INV-003";
    private const string SAMPLE_INVOICE_NUMBER_004 = "INV-004";
    private const string SAMPLE_INVOICE_NUMBER_005 = "INV-005";

    private readonly VatCalculationService _service = new(new VatCalculationSettings
    {
        DecimalPlaces = 2
    });

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
        decimal netAmount = 10000m;

        List<Invoice> invoices =
        [
            new() { InvoiceNumber = SAMPLE_INVOICE_NUMBER_001, NetAmount = netAmount, VatRate = STANDARD_HUNGARIAN_VAT_RATE }
        ];

        // Act
        VatDeclarationResult result = _service.Calculate(invoices);

        // Assert
        Assert.Single(result.SummariesByVatRate);
        VatSummary summary = result.SummariesByVatRate[0];
        Assert.Equal(STANDARD_HUNGARIAN_VAT_RATE, summary.VatRate);
        Assert.Equal(netAmount, summary.TotalNetAmount);
        Assert.Equal(CalculateVat(netAmount, STANDARD_HUNGARIAN_VAT_RATE), summary.TotalVatAmount);
        Assert.Equal(CalculateGross(netAmount, STANDARD_HUNGARIAN_VAT_RATE), summary.TotalGrossAmount);
        Assert.Equal(1, summary.InvoiceCount);

        Assert.Equal(netAmount, result.GrandTotalNet);
        Assert.Equal(CalculateVat(netAmount, STANDARD_HUNGARIAN_VAT_RATE), result.GrandTotalVat);
        Assert.Equal(CalculateGross(netAmount, STANDARD_HUNGARIAN_VAT_RATE), result.GrandTotalGross);
        Assert.Equal(1, result.TotalInvoiceCount);
    }

    [Fact]
    public void Calculate_WithMultipleVatRates_GroupsCorrectly()
    {
        // Arrange
        decimal vat27Invoice1Net = 10000m;
        decimal vat18Invoice2Net = 5000m;
        decimal vat5Invoice3Net = 2500m;
        decimal vat27Invoice4Net = 8000m;
        decimal vat18Invoice5Net = 3000m;

        List<Invoice> invoices =
        [
            new() { InvoiceNumber = SAMPLE_INVOICE_NUMBER_001, NetAmount = vat27Invoice1Net, VatRate = STANDARD_HUNGARIAN_VAT_RATE },
            new() { InvoiceNumber = SAMPLE_INVOICE_NUMBER_002, NetAmount = vat18Invoice2Net, VatRate = INTERMEDIATE_HUNGARIAN_VAT_RATE },
            new() { InvoiceNumber = SAMPLE_INVOICE_NUMBER_003, NetAmount = vat5Invoice3Net, VatRate = REDUCED_HUNGARIAN_VAT_RATE },
            new() { InvoiceNumber = SAMPLE_INVOICE_NUMBER_004, NetAmount = vat27Invoice4Net, VatRate = STANDARD_HUNGARIAN_VAT_RATE },
            new() { InvoiceNumber = SAMPLE_INVOICE_NUMBER_005, NetAmount = vat18Invoice5Net, VatRate = INTERMEDIATE_HUNGARIAN_VAT_RATE }
        ];

        decimal vat5GroupNet = vat5Invoice3Net;
        decimal vat18GroupNet = vat18Invoice2Net + vat18Invoice5Net;
        decimal vat27GroupNet = vat27Invoice1Net + vat27Invoice4Net;

        // Act
        VatDeclarationResult result = _service.Calculate(invoices);

        // Assert
        Assert.Equal(3, result.SummariesByVatRate.Count);

        VatSummary vat5 = result.SummariesByVatRate.First(s => s.VatRate == REDUCED_HUNGARIAN_VAT_RATE);
        Assert.Equal(vat5GroupNet, vat5.TotalNetAmount);
        Assert.Equal(CalculateVat(vat5GroupNet, REDUCED_HUNGARIAN_VAT_RATE), vat5.TotalVatAmount);
        Assert.Equal(CalculateGross(vat5GroupNet, REDUCED_HUNGARIAN_VAT_RATE), vat5.TotalGrossAmount);
        Assert.Equal(1, vat5.InvoiceCount);

        VatSummary vat18 = result.SummariesByVatRate.First(s => s.VatRate == INTERMEDIATE_HUNGARIAN_VAT_RATE);
        Assert.Equal(vat18GroupNet, vat18.TotalNetAmount);
        Assert.Equal(CalculateVat(vat18GroupNet, INTERMEDIATE_HUNGARIAN_VAT_RATE), vat18.TotalVatAmount);
        Assert.Equal(CalculateGross(vat18GroupNet, INTERMEDIATE_HUNGARIAN_VAT_RATE), vat18.TotalGrossAmount);
        Assert.Equal(2, vat18.InvoiceCount);

        VatSummary vat27 = result.SummariesByVatRate.First(s => s.VatRate == STANDARD_HUNGARIAN_VAT_RATE);
        Assert.Equal(vat27GroupNet, vat27.TotalNetAmount);
        Assert.Equal(CalculateVat(vat27GroupNet, STANDARD_HUNGARIAN_VAT_RATE), vat27.TotalVatAmount);
        Assert.Equal(CalculateGross(vat27GroupNet, STANDARD_HUNGARIAN_VAT_RATE), vat27.TotalGrossAmount);
        Assert.Equal(2, vat27.InvoiceCount);

        Assert.Equal(vat5GroupNet + vat18GroupNet + vat27GroupNet, result.GrandTotalNet);
        Assert.Equal(
            CalculateVat(vat5GroupNet, REDUCED_HUNGARIAN_VAT_RATE) 
            + CalculateVat(vat18GroupNet, INTERMEDIATE_HUNGARIAN_VAT_RATE)
            + CalculateVat(vat27GroupNet, STANDARD_HUNGARIAN_VAT_RATE),
            result.GrandTotalVat);
        Assert.Equal(
            CalculateGross(vat5GroupNet, REDUCED_HUNGARIAN_VAT_RATE)
            + CalculateGross(vat18GroupNet, INTERMEDIATE_HUNGARIAN_VAT_RATE)
            + CalculateGross(vat27GroupNet, STANDARD_HUNGARIAN_VAT_RATE),
            result.GrandTotalGross);
        Assert.Equal(5, result.TotalInvoiceCount);
    }

    [Fact]
    public void Calculate_OrdersSummariesByVatRate()
    {
        // Arrange
        decimal sameAmount = 1000m;

        List<Invoice> invoices =
        [
            new() { InvoiceNumber = SAMPLE_INVOICE_NUMBER_001, NetAmount = sameAmount, VatRate = STANDARD_HUNGARIAN_VAT_RATE },
            new() { InvoiceNumber = SAMPLE_INVOICE_NUMBER_002, NetAmount = sameAmount, VatRate = REDUCED_HUNGARIAN_VAT_RATE },
            new() { InvoiceNumber = SAMPLE_INVOICE_NUMBER_003, NetAmount = sameAmount, VatRate = INTERMEDIATE_HUNGARIAN_VAT_RATE }
        ];

        // Act
        VatDeclarationResult result = _service.Calculate(invoices);

        // Assert
        Assert.Equal(3, result.SummariesByVatRate.Count);
        Assert.Equal(REDUCED_HUNGARIAN_VAT_RATE, result.SummariesByVatRate[0].VatRate);
        Assert.Equal(INTERMEDIATE_HUNGARIAN_VAT_RATE, result.SummariesByVatRate[1].VatRate);
        Assert.Equal(STANDARD_HUNGARIAN_VAT_RATE, result.SummariesByVatRate[2].VatRate);
    }

    private static decimal CalculateVat(decimal netAmount, int vatRate) =>
        netAmount * vatRate / 100;

    private static decimal CalculateGross(decimal netAmount, int vatRate) =>
        netAmount + CalculateVat(netAmount, vatRate);
}
