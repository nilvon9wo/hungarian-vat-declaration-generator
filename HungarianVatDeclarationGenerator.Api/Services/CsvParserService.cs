using System.Runtime.CompilerServices;
using CsvHelper;
using HungarianVatDeclarationGenerator.Api.Configuration;
using HungarianVatDeclarationGenerator.Api.Models;

namespace HungarianVatDeclarationGenerator.Api.Services;

/// <summary>
/// Parses CSV files containing invoice data using CsvHelper.
/// Strategy: Lenient parsing - skips invalid rows and collects errors.
/// Only fails if the file is empty, unreadable, or contains zero valid invoices.
/// </summary>
public sealed class CsvParserService(
    ICsvReaderFactory csvReaderFactory,
    CsvParsingSettings csvParsingSettings,
    VatRateSettings vatRateSettings
) : ICsvParserService
{
    private readonly ICsvReaderFactory _csvReaderFactory = csvReaderFactory
        ?? throw new ArgumentNullException(nameof(csvReaderFactory));
    private readonly CsvParsingSettings _settings = csvParsingSettings
        ?? throw new ArgumentNullException(nameof(csvParsingSettings));
    private readonly VatRateSettings _vatRateSettings = vatRateSettings
        ?? throw new ArgumentNullException(nameof(vatRateSettings));

    public async Task<IReadOnlyList<Invoice>> Parse(Stream csvStream, CancellationToken cancellationToken = default)
    {
        try
        {
            return await ParseCsvStream(csvStream, cancellationToken);
        }
        catch (CsvHelper.HeaderValidationException ex)
        {
            throw new InvalidOperationException("Invalid CSV header format. Expected columns: InvoiceNumber, NetAmount, VatRate", ex);
        }
        catch (CsvHelper.TypeConversion.TypeConverterException ex)
        {
            throw new InvalidOperationException("Invalid data format in CSV file. Please check numeric values.", ex);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException("CSV processing was cancelled due to timeout or request cancellation.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException("Failed to parse CSV file. Please verify the file format.", ex);
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

    private async Task<(List<Invoice> validInvoices, List<string> errors)> CollectValidInvoices(
        CsvReader csv,
        CancellationToken cancellationToken)
    {
        List<Invoice> validInvoices = [];
        List<string> errors = [];

        await foreach (InvoiceCsvRecord? record in ReadRecords(csv, cancellationToken))
        {
            ProcessRecord(record, validInvoices, errors);
            if (validInvoices.Count >= _settings.MaxRowsToProcess) break;
        }

        return (validInvoices, errors);
    }

    private void ProcessRecord(InvoiceCsvRecord? record, List<Invoice> validInvoices, List<string> errors)
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

    private string? ValidateRecord(InvoiceCsvRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.InvoiceNumber))
            return $"Row {GetRowContext(record)}: Invoice number cannot be empty";

        if (record.InvoiceNumber.Length > _settings.MaxInvoiceNumberLength)
            return $"Row {GetRowContext(record)}: Invoice number too long (max {_settings.MaxInvoiceNumberLength} characters)";

        if (record.NetAmount <= 0)
            return $"Row {GetRowContext(record)}: Net amount must be positive, got {record.NetAmount}";

        if (record.NetAmount > decimal.MaxValue / 2)
            return $"Row {GetRowContext(record)}: Net amount exceeds maximum allowed value";

        if (!_vatRateSettings.IsValid(record.VatRate))
            return $"Row {GetRowContext(record)}: Invalid VAT rate {record.VatRate}%. Supported rates: {_vatRateSettings.GetSupportedRatesDisplay()}%";

        return null;
    }

    private static string GetRowContext(InvoiceCsvRecord record)
        => string.IsNullOrWhiteSpace(record.InvoiceNumber)
            ? "(unknown)"
            : TruncateForDisplay(record.InvoiceNumber, 50);

    private static string TruncateForDisplay(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength] + "...";

    private static Invoice MapToInvoice(InvoiceCsvRecord record)
        => new()
        {
            InvoiceNumber = record.InvoiceNumber.Trim(),
            NetAmount = record.NetAmount,
            VatRate = record.VatRate
        };

    private void ThrowIfNoValidInvoices(List<Invoice> validInvoices, List<string> errors)
    {
        if (validInvoices.Count == 0)
        {
            string errorMessage = errors.Count > 0
                ? $"CSV file contains no valid invoice data. First {Math.Min(errors.Count, _settings.MaxErrorsToDisplay)} errors:\n{string.Join("\n", errors.Take(_settings.MaxErrorsToDisplay))}"
                : "CSV file is empty or contains no valid invoice data.";

            throw new InvalidOperationException(errorMessage);
        }
    }
}
