using System.Globalization;
using System.Text;
using CsvHelper.Configuration;
using HungarianVatDeclarationGenerator.Api.Configuration;
using HungarianVatDeclarationGenerator.Api.Models;
using HungarianVatDeclarationGenerator.Api.Services;

namespace HungarianVatDeclarationGenerator.Api.Tests.Services;

public sealed class CsvParserServiceTests
{
    private const int STANDARD_HUNGARIAN_VAT_RATE = 27;
    private const int INTERMEDIATE_HUNGARIAN_VAT_RATE = 18;
    private const int REDUCED_HUNGARIAN_VAT_RATE = 5;
    private const int INVALID_VAT_RATE = 20;
    private const int CUSTOM_TEST_VAT_RATE_HIGH = 20;
    private const int CUSTOM_TEST_VAT_RATE_LOW = 10;

    private const string SAMPLE_INVOICE_NUMBER_001 = "INV-001";
    private const string SAMPLE_INVOICE_NUMBER_002 = "INV-002";
    private const string SAMPLE_INVOICE_NUMBER_003 = "INV-003";

    private const decimal SAMPLE_NET_AMOUNT_LARGE = 10000m;
    private const decimal SAMPLE_NET_AMOUNT_MEDIUM = 5000m;
    private const decimal SAMPLE_NET_AMOUNT_SMALL = 2500m;
    private const decimal NEGATIVE_NET_AMOUNT = -10000m;
    private const decimal ZERO_NET_AMOUNT = 0m;

    private const string INVALID_NET_AMOUNT_TEXT = "invalid";

    private const string CSV_HEADER = "InvoiceNumber,NetAmount,VatRate";

    private readonly CsvParserService _service;

    public CsvParserServiceTests()
    {
        CsvConfiguration csvConfig = CreateCsvConfiguration();
        CsvReaderFactory csvReaderFactory = new(csvConfig);
        CsvParsingSettings settings = CreateCsvParsingSettings();
        VatRateSettings vatRateSettings = CreateDefaultVatRateSettings();
        _service = new CsvParserService(csvReaderFactory, settings, vatRateSettings);
    }

    [Fact]
    public async Task Parse_WithValidCsv_ReturnsInvoices()
    {
        // Arrange
        string csv = $"""
            {CSV_HEADER}
            {SAMPLE_INVOICE_NUMBER_001},{SAMPLE_NET_AMOUNT_LARGE},{STANDARD_HUNGARIAN_VAT_RATE}
            {SAMPLE_INVOICE_NUMBER_002},{SAMPLE_NET_AMOUNT_MEDIUM},{INTERMEDIATE_HUNGARIAN_VAT_RATE}
            {SAMPLE_INVOICE_NUMBER_003},{SAMPLE_NET_AMOUNT_SMALL},{REDUCED_HUNGARIAN_VAT_RATE}
            """;
        MemoryStream stream = CreateStream(csv);

        // Act
        IReadOnlyList<Invoice> invoices = await _service.Parse(stream);

        // Assert
        Assert.Equal(3, invoices.Count);

        Assert.Equal(SAMPLE_INVOICE_NUMBER_001, invoices[0].InvoiceNumber);
        Assert.Equal(SAMPLE_NET_AMOUNT_LARGE, invoices[0].NetAmount);
        Assert.Equal(STANDARD_HUNGARIAN_VAT_RATE, invoices[0].VatRate);

        Assert.Equal(SAMPLE_INVOICE_NUMBER_002, invoices[1].InvoiceNumber);
        Assert.Equal(SAMPLE_NET_AMOUNT_MEDIUM, invoices[1].NetAmount);
        Assert.Equal(INTERMEDIATE_HUNGARIAN_VAT_RATE, invoices[1].VatRate);

        Assert.Equal(SAMPLE_INVOICE_NUMBER_003, invoices[2].InvoiceNumber);
        Assert.Equal(SAMPLE_NET_AMOUNT_SMALL, invoices[2].NetAmount);
        Assert.Equal(REDUCED_HUNGARIAN_VAT_RATE, invoices[2].VatRate);
    }

    [Fact]
    public async Task Parse_WithEmptyFile_ThrowsException()
    {
        // Arrange
        MemoryStream stream = CreateStream("");

        // Act
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.Parse(stream));

        // Assert
        Assert.Contains("empty", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Parse_WithMissingHeader_ThrowsException()
    {
        // Arrange
        string csv = $"""
            {SAMPLE_INVOICE_NUMBER_001},{SAMPLE_NET_AMOUNT_LARGE},{STANDARD_HUNGARIAN_VAT_RATE}
            """;
        MemoryStream stream = CreateStream(csv);

        // Act
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.Parse(stream));

        // Assert
        Assert.Contains("Invalid CSV header", ex.Message);
    }

    [Fact]
    public async Task Parse_WithInvalidHeader_ThrowsException()
    {
        // Arrange
        const string INVALID_CSV_HEADER = "Invoice,Amount,Rate";
        string csv = $"""
            {INVALID_CSV_HEADER}
            {SAMPLE_INVOICE_NUMBER_001},{SAMPLE_NET_AMOUNT_LARGE},{STANDARD_HUNGARIAN_VAT_RATE}
            """;
        MemoryStream stream = CreateStream(csv);

        // Act
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.Parse(stream));

        // Assert
        Assert.Contains("Invalid CSV header", ex.Message);
    }

    [Fact]
    public async Task Parse_WithNoDataRows_ThrowsException()
    {
        // Arrange
        string csv = CSV_HEADER;
        MemoryStream stream = CreateStream(csv);

        // Act
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.Parse(stream));

        // Assert
        Assert.Contains("no valid invoice data", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Parse_WithInvalidVatRate_ThrowsException()
    {
        // Arrange
        string csv = $"""
            {CSV_HEADER}
            {SAMPLE_INVOICE_NUMBER_001},{SAMPLE_NET_AMOUNT_LARGE},{INVALID_VAT_RATE}
            """;
        MemoryStream stream = CreateStream(csv);

        // Act
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.Parse(stream));

        // Assert
        Assert.Contains("Invalid VAT rate", ex.Message);
        Assert.Contains(INVALID_VAT_RATE.ToString(), ex.Message);
    }

    [Fact]
    public async Task Parse_WithNegativeNetAmount_ThrowsException()
    {
        // Arrange
        string csv = $"""
            {CSV_HEADER}
            {SAMPLE_INVOICE_NUMBER_001},{NEGATIVE_NET_AMOUNT},{STANDARD_HUNGARIAN_VAT_RATE}
            """;
        MemoryStream stream = CreateStream(csv);

        // Act
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.Parse(stream));

        // Assert
        Assert.Contains("must be positive", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Parse_WithZeroNetAmount_ThrowsException()
    {
        // Arrange
        string csv = $"""
            {CSV_HEADER}
            {SAMPLE_INVOICE_NUMBER_001},{ZERO_NET_AMOUNT},{STANDARD_HUNGARIAN_VAT_RATE}
            """;
        MemoryStream stream = CreateStream(csv);

        // Act
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.Parse(stream));

        // Assert
        Assert.Contains("must be positive", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Parse_WithEmptyInvoiceNumber_ThrowsException()
    {
        // Arrange
        string csv = $"""
            {CSV_HEADER}
            ,{SAMPLE_NET_AMOUNT_LARGE},{STANDARD_HUNGARIAN_VAT_RATE}
            """;
        MemoryStream stream = CreateStream(csv);

        // Act
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.Parse(stream));

        // Assert
        Assert.Contains("Invoice number cannot be empty", ex.Message);
    }

    [Fact]
    public async Task Parse_WithInvalidNetAmount_ThrowsException()
    {
        // Arrange
        string csv = $"""
            {CSV_HEADER}
            {SAMPLE_INVOICE_NUMBER_001},{INVALID_NET_AMOUNT_TEXT},{STANDARD_HUNGARIAN_VAT_RATE}
            """;
        MemoryStream stream = CreateStream(csv);

        // Act
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.Parse(stream));

        // Assert
        Assert.Contains("Invalid data format", ex.Message);
    }

    [Fact]
    public async Task Parse_WithWrongColumnCount_ThrowsException()
    {
        // Arrange
        string csv = $"""
            {CSV_HEADER}
            {SAMPLE_INVOICE_NUMBER_001},{SAMPLE_NET_AMOUNT_LARGE}
            """;
        MemoryStream stream = CreateStream(csv);

        // Act
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.Parse(stream));

        // Assert
        Assert.Contains("Invalid data format", ex.Message);
    }

    [Fact]
    public async Task Parse_SkipsEmptyLines()
    {
        // Arrange
        string csv = $"""
            {CSV_HEADER}
            {SAMPLE_INVOICE_NUMBER_001},{SAMPLE_NET_AMOUNT_LARGE},{STANDARD_HUNGARIAN_VAT_RATE}

            {SAMPLE_INVOICE_NUMBER_002},{SAMPLE_NET_AMOUNT_MEDIUM},{INTERMEDIATE_HUNGARIAN_VAT_RATE}
            """;
        MemoryStream stream = CreateStream(csv);

        // Act
        IReadOnlyList<Invoice> invoices = await _service.Parse(stream);

        // Assert
        Assert.Equal(2, invoices.Count);
    }

    [Fact]
    public async Task Parse_HandlesWhitespace()
    {
        // Arrange
        string csv = $"""
            {CSV_HEADER}
             {SAMPLE_INVOICE_NUMBER_001} , {SAMPLE_NET_AMOUNT_LARGE} , {STANDARD_HUNGARIAN_VAT_RATE} 
            """;
        MemoryStream stream = CreateStream(csv);

        // Act
        IReadOnlyList<Invoice> invoices = await _service.Parse(stream);

        // Assert
        Assert.Single(invoices);
        Assert.Equal(SAMPLE_INVOICE_NUMBER_001, invoices[0].InvoiceNumber);
        Assert.Equal(SAMPLE_NET_AMOUNT_LARGE, invoices[0].NetAmount);
        Assert.Equal(STANDARD_HUNGARIAN_VAT_RATE, invoices[0].VatRate);
    }

    [Fact]
    public async Task Parse_WithCustomVatRates_RespectsConfiguredRates()
    {
        // Arrange
        CsvParserService customService = CreateServiceWithCustomVatRates();
        string csv = CreateCsvWithUnsupportedVatRates();
        MemoryStream stream = CreateStream(csv);

        // Act
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => customService.Parse(stream));

        // Assert
        AssertExceptionContainsExpectedMessages(exception);
    }

    private static CsvParserService CreateServiceWithCustomVatRates()
    {
        CsvReaderFactory factory = new(CreateCsvConfiguration());
        CsvParsingSettings settings = CreateCsvParsingSettings();
        VatRateSettings customRates = CreateCustomVatRateSettings();
        return new CsvParserService(factory, settings, customRates);
    }

    private static string CreateCsvWithUnsupportedVatRates() =>
        $"""
        {CSV_HEADER}
        {SAMPLE_INVOICE_NUMBER_001},{SAMPLE_NET_AMOUNT_LARGE},{STANDARD_HUNGARIAN_VAT_RATE}
        {SAMPLE_INVOICE_NUMBER_002},{SAMPLE_NET_AMOUNT_MEDIUM},{INTERMEDIATE_HUNGARIAN_VAT_RATE}
        """;

    private static void AssertExceptionContainsExpectedMessages(InvalidOperationException exception)
    {
        Assert.Contains("no valid invoice data", exception.Message);
        Assert.Contains($"Invalid VAT rate {STANDARD_HUNGARIAN_VAT_RATE}%", exception.Message);
        Assert.Contains($"Supported rates: {CUSTOM_TEST_VAT_RATE_LOW}, {CUSTOM_TEST_VAT_RATE_HIGH}", exception.Message);
    }

    private static CsvConfiguration CreateCsvConfiguration() =>
        new(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim
        };

    private static CsvParsingSettings CreateCsvParsingSettings() =>
        new()
        {
            MaxRowsToProcess = 10000,
            MaxInvoiceNumberLength = 100,
            MaxFieldLength = 500,
            MaxErrorsToDisplay = 5
        };

    private static VatRateSettings CreateDefaultVatRateSettings() =>
        new()
        {
            SupportedRates = [REDUCED_HUNGARIAN_VAT_RATE, INTERMEDIATE_HUNGARIAN_VAT_RATE, STANDARD_HUNGARIAN_VAT_RATE]
        };

    private static VatRateSettings CreateCustomVatRateSettings() =>
        new()
        {
            SupportedRates = [CUSTOM_TEST_VAT_RATE_LOW, CUSTOM_TEST_VAT_RATE_HIGH]
        };

    private static MemoryStream CreateStream(string content)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        return new MemoryStream(bytes);
    }
}
