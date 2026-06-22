using System.Runtime.CompilerServices;
using CsvHelper;
using HungarianVatDeclarationGenerator.Api.Constants;
using HungarianVatDeclarationGenerator.Api.Models;

namespace HungarianVatDeclarationGenerator.Api.Services;

/// <summary>
/// Parses CSV files containing invoice data using CsvHelper.
/// Strategy: Lenient parsing - skips invalid rows and collects errors.
/// Only fails if the file is empty, unreadable, or contains zero valid invoices.
/// </summary>
public sealed class CsvParserService(ICsvReaderFactory csvReaderFactory) : ICsvParserService
{
    private const int MaxRowsToProcess = 10000;
    private readonly ICsvReaderFactory _csvReaderFactory = csvReaderFactory;

    public async Task<IReadOnlyList<Invoice>> Parse(Stream csvStream, CancellationToken cancellationToken = default)
    {
        try
        {
            return await ParseCsvStream(csvStream, cancellationToken);
        }
        catch (CsvHelper.HeaderValidationException ex)
        {
            throw new InvalidOperationException($"Invalid CSV header: {ex.Message}", ex);
        }
        catch (CsvHelper.TypeConversion.TypeConverterException ex)
        {
            throw new InvalidOperationException($"Invalid data format: {ex.Message}", ex);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException("Failed to parse CSV file.", ex);
        }
    }

    private async Task<IReadOnlyList<Invoice>> ParseCsvStream(Stream csvStream, CancellationToken cancellationToken)
    {
        using StreamReader reader = new(csvStream);
        using CsvReader csv = _csvReaderFactory.Create(reader);

        (List<Invoice> validInvoices, List<string> errors) = await CollectValidInvoices(csv, cancellationToken);
        ThrowIfNoValidInvoices(validInvoices, errors);
        return validInvoices;
    }

    private static async Task<(List<Invoice> validInvoices, List<string> errors)> CollectValidInvoices(
        CsvReader csv,
        CancellationToken cancellationToken)
    {
        List<Invoice> validInvoices = [];
        List<string> errors = [];

        await foreach (InvoiceCsvRecord? record in ReadRecords(csv, cancellationToken))
        {
            ProcessRecord(record, validInvoices, errors);
            if (validInvoices.Count >= MaxRowsToProcess) break;
        }

        return (validInvoices, errors);
    }

    private static void ProcessRecord(InvoiceCsvRecord? record, List<Invoice> validInvoices, List<string> errors)
    {
        if (record == null) return;

        string? validationError = ValidateRecord(record);
        if (validationError != null)
        {
            errors.Add(validationError);
            return;
        }

        validInvoices.Add(MapToInvoice(record));
    }

    private static async IAsyncEnumerable<InvoiceCsvRecord?> ReadRecords(
        CsvReader csv,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IAsyncEnumerable<InvoiceCsvRecord> records = csv.GetRecordsAsync<InvoiceCsvRecord>(cancellationToken);

        await foreach (InvoiceCsvRecord record in records.WithCancellation(cancellationToken))
        {
            yield return record;
        }
    }

    private static string? ValidateRecord(InvoiceCsvRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.InvoiceNumber))
            return $"Row {GetRowContext(record)}: Invoice number cannot be empty";

        if (record.NetAmount <= 0)
            return $"Row {GetRowContext(record)}: Net amount must be positive, got {record.NetAmount}";

        if (!VatRates.IsValid(record.VatRate))
            return $"Row {GetRowContext(record)}: Invalid VAT rate {record.VatRate}%. Supported rates: {string.Join(", ", VatRates.Supported)}%";

        return null;
    }

    private static string GetRowContext(InvoiceCsvRecord record)
        => string.IsNullOrWhiteSpace(record.InvoiceNumber)
            ? "(unknown)"
            : record.InvoiceNumber;

    private static Invoice MapToInvoice(InvoiceCsvRecord record)
        => new()
        {
            InvoiceNumber = record.InvoiceNumber,
            NetAmount = record.NetAmount,
            VatRate = record.VatRate
        };

    private static void ThrowIfNoValidInvoices(List<Invoice> validInvoices, List<string> errors)
    {
        if (validInvoices.Count == 0)
        {
            string errorMessage = errors.Count > 0
                ? $"CSV file contains no valid invoice data. Errors found:\n{string.Join("\n", errors.Take(10))}"
                : "CSV file is empty or contains no valid invoice data.";

            throw new InvalidOperationException(errorMessage);
        }
    }
}
