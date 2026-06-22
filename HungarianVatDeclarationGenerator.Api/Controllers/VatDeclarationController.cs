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
    ILogger<VatDeclarationController> logger
) : ControllerBase
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly string[] AllowedContentTypes = ["text/csv", "application/vnd.ms-excel"];

    private readonly ICsvParserService _csvParser = csvParser;
    private readonly IVatCalculationService _vatCalculator = vatCalculator;
    private readonly IPdfGenerationService _pdfGenerator = pdfGenerator;
    private readonly ILogger<VatDeclarationController> _logger = logger;

    /// <summary>
    /// Upload a CSV file containing invoice data and receive a VAT declaration summary.
    /// </summary>
    /// <param name="file">CSV file with columns: InvoiceNumber, NetAmount, VatRate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>VAT declaration result with summaries grouped by VAT rate</returns>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(Models.VatDeclarationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadCsv(
            IFormFile file,
            CancellationToken cancellationToken
        )
    {
        _logger.LogInformation("Processing CSV upload: {FileName}", file?.FileName);

        ValidateFile(file);

        await using Stream stream = file!.OpenReadStream();
        IReadOnlyList<Invoice> invoices = await _csvParser.Parse(stream, cancellationToken);

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
    public async Task<IActionResult> UploadCsvAndGeneratePdf(
            IFormFile file,
            CancellationToken cancellationToken
        )
    {
        _logger.LogInformation("Processing CSV upload for PDF generation: {FileName}", file?.FileName);

        ValidateFile(file);

        await using Stream stream = file!.OpenReadStream();
        IReadOnlyList<Invoice> invoices = await _csvParser.Parse(stream, cancellationToken);

        VatDeclarationResult result = _vatCalculator.Calculate(invoices);
        byte[] pdfBytes = _pdfGenerator.GeneratePdf(result);

        _logger.LogInformation("Generated PDF report for {InvoiceCount} invoices", result.TotalInvoiceCount);

        return File(pdfBytes, "application/pdf", "vat-declaration.pdf");
    }

    private static void ValidateFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            throw new InvalidOperationException("No file uploaded or file is empty");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException(
                $"File size exceeds maximum allowed size of {MaxFileSizeBytes / 1024 / 1024} MB");
        }

        if (!AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase) &&
            !file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only CSV files are allowed");
        }
    }
}
