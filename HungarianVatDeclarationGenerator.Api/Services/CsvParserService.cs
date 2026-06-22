using System.Globalization;
using HungarianVatDeclarationGenerator.Api.Constants;
using HungarianVatDeclarationGenerator.Api.Models;

namespace HungarianVatDeclarationGenerator.Api.Services;

public sealed class CsvParserService : ICsvParserService
{
    private const int MaxRowsToProcess = 10000;
    private static readonly string[] ExpectedHeaders = ["InvoiceNumber", "NetAmount", "VatRate"];

    public async Task<IReadOnlyList<Invoice>> Parse(Stream csvStream, CancellationToken cancellationToken = default)
    {
        using StreamReader reader = new(csvStream);

        string headerLine = await ReadAndValidateHeader(reader, cancellationToken);
        IEnumerable<string> dataLines = await ReadDataLines(reader, cancellationToken);
        IReadOnlyList<Invoice> invoices = ParseInvoices(dataLines);

        ValidateHasInvoices(invoices);
        return invoices;
    }

    private static async Task<string> ReadAndValidateHeader(StreamReader reader, CancellationToken cancellationToken)
    {
        string? headerLine = await reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(headerLine))
            throw new InvalidOperationException("CSV file is empty or missing header row.");

        ValidateHeaderColumns(headerLine);
        return headerLine;
    }

    private static async Task<IEnumerable<string>> ReadDataLines(StreamReader reader, CancellationToken cancellationToken)
    {
        List<string> lines = [];
        int lineNumber = 0;

        while (lineNumber < MaxRowsToProcess)
        {
            string? line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) break;
            if (!string.IsNullOrWhiteSpace(line))
                lines.Add(line);
            lineNumber++;
        }

        return lines;
    }

    private static IReadOnlyList<Invoice> ParseInvoices(IEnumerable<string> dataLines)
        => [.. dataLines.Select((line, index) => ParseInvoiceLine(line, index + 2))];

    private static void ValidateHeaderColumns(string headerLine)
    {
        string[] actualHeaders = [.. headerLine.Split(',').Select(h => h.Trim())];

        if (actualHeaders.Length != ExpectedHeaders.Length)
            throw new InvalidOperationException(
                $"Invalid CSV header. Expected {ExpectedHeaders.Length} columns: {string.Join(", ", ExpectedHeaders)}");

        var invalidColumn = actualHeaders
            .Select((header, index) => new { header, expected = ExpectedHeaders[index], index })
            .FirstOrDefault(x => !string.Equals(x.header, x.expected, StringComparison.OrdinalIgnoreCase));

        if (invalidColumn != null)
            throw new InvalidOperationException(
                $"Invalid CSV header at column {invalidColumn.index + 1}. Expected '{invalidColumn.expected}', found '{invalidColumn.header}'");
    }

    private static Invoice ParseInvoiceLine(string line, int lineNumber)
    {
        string[] parts = SplitAndValidateColumns(line, lineNumber);
        return new Invoice
        {
            InvoiceNumber = ParseInvoiceNumber(parts[0], lineNumber),
            NetAmount = ParseNetAmount(parts[1], lineNumber),
            VatRate = ParseVatRate(parts[2], lineNumber)
        };
    }

    private static string[] SplitAndValidateColumns(string line, int lineNumber)
    {
        string[] parts = line.Split(',');
        return parts.Length != 3
            ? throw new InvalidOperationException($"Line {lineNumber}: Expected 3 columns, found {parts.Length}")
            : parts;
    }

    private static string ParseInvoiceNumber(string value, int lineNumber)
    {
        string invoiceNumber = value.Trim();
        return string.IsNullOrWhiteSpace(invoiceNumber)
            ? throw new InvalidOperationException($"Line {lineNumber}: Invoice number cannot be empty")
            : invoiceNumber;
    }

    private static decimal ParseNetAmount(string value, int lineNumber)
    {
        if (!decimal.TryParse(value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal netAmount))
            throw new InvalidOperationException($"Line {lineNumber}: Invalid net amount '{value}'");

        return netAmount <= 0
            ? throw new InvalidOperationException($"Line {lineNumber}: Net amount must be positive, got {netAmount}")
            : netAmount;
    }

    private static int ParseVatRate(string value, int lineNumber)
    {
        if (!int.TryParse(value.Trim(), out int vatRate))
            throw new InvalidOperationException($"Line {lineNumber}: Invalid VAT rate '{value}'");

        return !VatRates.IsValid(vatRate)
            ? throw new InvalidOperationException(
                $"Line {lineNumber}: Invalid VAT rate {vatRate}%. Supported rates: {string.Join(", ", VatRates.Supported)}%")
            : vatRate;
    }

    private static void ValidateHasInvoices(IReadOnlyList<Invoice> invoices)
    {
        if (invoices.Count == 0)
            throw new InvalidOperationException("CSV file contains no valid invoice data.");
    }
}
