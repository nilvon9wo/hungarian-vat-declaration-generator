using System.Globalization;
using System.Text;
using CsvHelper.Configuration;
using HungarianVatDeclarationGenerator.Api.Configuration;
using HungarianVatDeclarationGenerator.Api.Models;
using HungarianVatDeclarationGenerator.Api.Services;

namespace HungarianVatDeclarationGenerator.Api.Tests.Services;

public sealed class CsvParserServiceTests
{
    private readonly CsvParserService _service;

    public CsvParserServiceTests()
    {
        CsvConfiguration csvConfig = new(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim
        };
        CsvReaderFactory csvReaderFactory = new(csvConfig);

        CsvParsingSettings settings = new()
        {
            MaxRowsToProcess = 10000,
            MaxInvoiceNumberLength = 100,
            MaxFieldLength = 500,
            MaxErrorsToDisplay = 5
        };

        VatRateSettings vatRateSettings = new()
        {
            SupportedRates = [5, 18, 27]
        };

        _service = new CsvParserService(csvReaderFactory, settings, vatRateSettings);
    }

    [Fact]
    public async Task Parse_WithValidCsv_ReturnsInvoices()
    {
        // Arrange
        string csv = """
            InvoiceNumber,NetAmount,VatRate
            INV-001,10000,27
            INV-002,5000,18
            INV-003,2500,5
            """;
        MemoryStream stream = CreateStream(csv);

        // Act
        IReadOnlyList<Invoice> invoices = await _service.Parse(stream);

        // Assert
        Assert.Equal(3, invoices.Count);

        Assert.Equal("INV-001", invoices[0].InvoiceNumber);
        Assert.Equal(10000m, invoices[0].NetAmount);
        Assert.Equal(27, invoices[0].VatRate);

        Assert.Equal("INV-002", invoices[1].InvoiceNumber);
        Assert.Equal(5000m, invoices[1].NetAmount);
        Assert.Equal(18, invoices[1].VatRate);

        Assert.Equal("INV-003", invoices[2].InvoiceNumber);
        Assert.Equal(2500m, invoices[2].NetAmount);
        Assert.Equal(5, invoices[2].VatRate);
    }

    [Fact]
    public async Task Parse_WithEmptyFile_ThrowsException()
    {
        // Arrange
        MemoryStream stream = CreateStream("");

        // Act & Assert
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.Parse(stream));
        Assert.Contains("empty", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Parse_WithMissingHeader_ThrowsException()
    {
        // Arrange
        string csv = """
            INV-001,10000,27
            """;
        MemoryStream stream = CreateStream(csv);

        // Act & Assert
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.Parse(stream));
        Assert.Contains("Invalid CSV header", ex.Message);
    }

    [Fact]
    public async Task Parse_WithInvalidHeader_ThrowsException()
    {
        // Arrange
        string csv = """
            Invoice,Amount,Rate
            INV-001,10000,27
            """;
        MemoryStream stream = CreateStream(csv);

        // Act & Assert
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.Parse(stream));
        Assert.Contains("Invalid CSV header", ex.Message);
    }

    [Fact]
    public async Task Parse_WithNoDataRows_ThrowsException()
    {
        // Arrange
        string csv = "InvoiceNumber,NetAmount,VatRate";
        MemoryStream stream = CreateStream(csv);

        // Act & Assert
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.Parse(stream));
        Assert.Contains("no valid invoice data", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Parse_WithInvalidVatRate_ThrowsException()
    {
        // Arrange
        string csv = """
            InvoiceNumber,NetAmount,VatRate
            INV-001,10000,20
            """;
        MemoryStream stream = CreateStream(csv);

        // Act & Assert
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.Parse(stream));
        Assert.Contains("Invalid VAT rate", ex.Message);
        Assert.Contains("20", ex.Message);
    }

    [Fact]
    public async Task Parse_WithNegativeNetAmount_ThrowsException()
    {
        // Arrange
        string csv = """
            InvoiceNumber,NetAmount,VatRate
            INV-001,-10000,27
            """;
        MemoryStream stream = CreateStream(csv);

        // Act & Assert
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.Parse(stream));
        Assert.Contains("must be positive", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Parse_WithZeroNetAmount_ThrowsException()
    {
        // Arrange
        string csv = """
            InvoiceNumber,NetAmount,VatRate
            INV-001,0,27
            """;
        MemoryStream stream = CreateStream(csv);

        // Act & Assert
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.Parse(stream));
        Assert.Contains("must be positive", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Parse_WithEmptyInvoiceNumber_ThrowsException()
    {
        // Arrange
        string csv = """
            InvoiceNumber,NetAmount,VatRate
            ,10000,27
            """;
        MemoryStream stream = CreateStream(csv);

        // Act & Assert
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.Parse(stream));
        Assert.Contains("Invoice number cannot be empty", ex.Message);
    }

    [Fact]
    public async Task Parse_WithInvalidNetAmount_ThrowsException()
    {
        // Arrange
        string csv = """
            InvoiceNumber,NetAmount,VatRate
            INV-001,invalid,27
            """;
        MemoryStream stream = CreateStream(csv);

        // Act & Assert
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.Parse(stream));
        Assert.Contains("Invalid data format", ex.Message);
    }

    [Fact]
    public async Task Parse_WithWrongColumnCount_ThrowsException()
    {
        // Arrange
        string csv = """
            InvoiceNumber,NetAmount,VatRate
            INV-001,10000
            """;
        MemoryStream stream = CreateStream(csv);

        // Act & Assert
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.Parse(stream));
        Assert.Contains("Invalid data format", ex.Message);
    }

    [Fact]
    public async Task Parse_SkipsEmptyLines()
    {
        // Arrange
        string csv = """
            InvoiceNumber,NetAmount,VatRate
            INV-001,10000,27

            INV-002,5000,18
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
        string csv = """
            InvoiceNumber,NetAmount,VatRate
             INV-001 , 10000 , 27 
            """;
        MemoryStream stream = CreateStream(csv);

        // Act
        IReadOnlyList<Invoice> invoices = await _service.Parse(stream);

        // Assert
        Assert.Single(invoices);
        Assert.Equal("INV-001", invoices[0].InvoiceNumber);
        Assert.Equal(10000m, invoices[0].NetAmount);
        Assert.Equal(27, invoices[0].VatRate);
    }

    [Fact]
    public async Task Parse_WithCustomVatRates_RespectsConfiguredRates()
    {
        // Arrange
        CsvConfiguration csvConfig = new(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim
        };
        CsvReaderFactory csvReaderFactory = new(csvConfig);
        CsvParsingSettings settings = new()
        {
            MaxRowsToProcess = 10000,
            MaxInvoiceNumberLength = 100,
            MaxFieldLength = 500,
            MaxErrorsToDisplay = 5
        };
        VatRateSettings customVatRateSettings = new()
        {
            SupportedRates = [10, 20]
        };
        CsvParserService customService = new(csvReaderFactory, settings, customVatRateSettings);

        string csv = """
            InvoiceNumber,NetAmount,VatRate
            INV-001,10000,27
            INV-002,5000,18
            """;
        MemoryStream stream = CreateStream(csv);

        // Act
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => customService.Parse(stream));

        // Assert
        Assert.Contains("no valid invoice data", exception.Message);
        Assert.Contains("Invalid VAT rate 27%", exception.Message);
        Assert.Contains("Supported rates: 10, 20", exception.Message);
    }

    private static MemoryStream CreateStream(string content)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        return new MemoryStream(bytes);
    }
}
