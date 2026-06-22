using HungarianVatDeclarationGenerator.Api.Configuration;
using HungarianVatDeclarationGenerator.Api.Models;
using HungarianVatDeclarationGenerator.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HungarianVatDeclarationGenerator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class VatDeclarationController(
    ICsvParserService csvParser,
    IVatCalculationService vatCalculator,
    IPdfGenerationService pdfGenerator,
    ILogger<VatDeclarationController> logger,
    FileUploadSettings fileUploadSettings
) : ControllerBase
{
    private readonly ICsvParserService _csvParser = csvParser
        ?? throw new ArgumentNullException(nameof(csvParser));
    private readonly IVatCalculationService _vatCalculator = vatCalculator
        ?? throw new ArgumentNullException(nameof(vatCalculator));
    private readonly IPdfGenerationService _pdfGenerator = pdfGenerator
        ?? throw new ArgumentNullException(nameof(pdfGenerator));
    private readonly ILogger<VatDeclarationController> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));
    private readonly FileUploadSettings _fileUploadSettings = fileUploadSettings
        ?? throw new ArgumentNullException(nameof(fileUploadSettings));

    /// <summary>
    /// Upload a CSV file containing invoice data and receive a VAT declaration summary.
    /// </summary>
    /// <param name="file">CSV file with columns: InvoiceNumber, NetAmount, VatRate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>VAT declaration result with summaries grouped by VAT rate</returns>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(Models.VatDeclarationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
    public async Task<IActionResult> UploadCsv(
            IFormFile file,
            CancellationToken cancellationToken
        )
    {
        string sanitizedFilename = SanitizeFilename(file?.FileName);
        _logger.LogInformation("Processing CSV upload: {FileName}", sanitizedFilename);

        ValidateFile(file);

        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_fileUploadSettings.ProcessingTimeoutSeconds));

        await using Stream stream = file!.OpenReadStream();
        IReadOnlyList<Invoice> invoices = await _csvParser.Parse(stream, cts.Token);

        VatDeclarationResult result = _vatCalculator.Calculate(invoices);

        _logger.LogInformation(
            "Successfully processed {InvoiceCount} invoices with total gross amount {GrossAmount:C}",
            result.TotalInvoiceCount,
            result.GrandTotalGross);

        return Ok(result);
    }

    /// <summary>
    /// Upload a CSV file and generate a PDF report of the VAT declaration.
    /// </summary>
    /// <param name="file">CSV file with columns: InvoiceNumber, NetAmount, VatRate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF file</returns>
    [HttpPost("upload-and-generate-pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
    public async Task<IActionResult> UploadCsvAndGeneratePdf(
            IFormFile file,
            CancellationToken cancellationToken
        )
    {
        string sanitizedFilename = SanitizeFilename(file?.FileName);
        _logger.LogInformation("Processing CSV upload for PDF generation: {FileName}", sanitizedFilename);

        ValidateFile(file);

        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_fileUploadSettings.ProcessingTimeoutSeconds));

        await using Stream stream = file!.OpenReadStream();
        IReadOnlyList<Invoice> invoices = await _csvParser.Parse(stream, cts.Token);

        VatDeclarationResult result = _vatCalculator.Calculate(invoices);
        byte[] pdfBytes = _pdfGenerator.GeneratePdf(result);

        _logger.LogInformation("Generated PDF report for {InvoiceCount} invoices", result.TotalInvoiceCount);

        return File(pdfBytes, "application/pdf", "vat-declaration.pdf");
    }

    private void ValidateFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            throw new InvalidOperationException("No file uploaded or file is empty");
        }

        if (file.Length > _fileUploadSettings.MaxFileSizeBytes)
        {
            throw new InvalidOperationException(
                $"File size exceeds maximum allowed size of {_fileUploadSettings.MaxFileSizeBytes / 1024 / 1024} MB");
        }

        if (!_fileUploadSettings.AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase) &&
            !_fileUploadSettings.AllowedExtensions.Any(ext => file.FileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Only CSV files are allowed");
        }
    }

    private static string SanitizeFilename(string? filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            return "unknown";
        }

        return new string([.. filename.Where(c => !char.IsControl(c))]);
    }
}
