using HungarianVatDeclarationGenerator.Api.Configuration;
using HungarianVatDeclarationGenerator.Api.Models;
using HungarianVatDeclarationGenerator.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HungarianVatDeclarationGenerator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class VatDeclarationController(
    ICsvParserService csvParser,
    IVatCalculationService vatCalculator,
    IPdfGenerationService pdfGenerator,
    FileUploadSettings fileUploadSettings,
    FileValidationSettings fileValidationSettings
) : ControllerBase
{
    private readonly ICsvParserService _csvParser = csvParser
        ?? throw new ArgumentNullException(nameof(csvParser));
    private readonly IVatCalculationService _vatCalculator = vatCalculator
        ?? throw new ArgumentNullException(nameof(vatCalculator));
    private readonly IPdfGenerationService _pdfGenerator = pdfGenerator
        ?? throw new ArgumentNullException(nameof(pdfGenerator));
    private readonly FileUploadSettings _fileUploadSettings = fileUploadSettings
        ?? throw new ArgumentNullException(nameof(fileUploadSettings));
    private readonly FileValidationSettings _fileValidationSettings = fileValidationSettings
        ?? throw new ArgumentNullException(nameof(fileValidationSettings));

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
        ValidateFile(file);

        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_fileUploadSettings.ProcessingTimeoutSeconds));

        await using Stream stream = file!.OpenReadStream();
        IReadOnlyList<Invoice> invoices = await _csvParser.Parse(stream, cts.Token);

        VatDeclarationResult result = _vatCalculator.Calculate(invoices);
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
        ValidateFile(file);

        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_fileUploadSettings.ProcessingTimeoutSeconds));

        await using Stream stream = file!.OpenReadStream();
        IReadOnlyList<Invoice> invoices = await _csvParser.Parse(stream, cts.Token);

        VatDeclarationResult result = _vatCalculator.Calculate(invoices);
        byte[] pdfBytes = _pdfGenerator.GeneratePdf(result);
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

        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_fileUploadSettings.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only CSV files are allowed");
        }

        ValidateFileContent(file);
    }

    private void ValidateFileContent(IFormFile file)
    {
        using Stream stream = file.OpenReadStream();
        Span<byte> buffer = stackalloc byte[8];
        int bytesRead = stream.Read(buffer);
        stream.Position = 0;

        if (bytesRead == 0)
        {
            throw new InvalidOperationException("Uploaded file is empty");
        }

        if (_fileValidationSettings.RejectBinaryFormats)
        {
            CheckForBinaryFormats(buffer[..bytesRead]);
        }

        bool isValidCsv = (buffer[0] == 0xEF && bytesRead >= 3 && buffer[1] == 0xBB && buffer[2] == 0xBF) ||
                          (buffer[0] < 0x80);

        if (!isValidCsv)
        {
            throw new InvalidOperationException("Uploaded file does not appear to be a valid CSV file");
        }
    }

    private void CheckForBinaryFormats(ReadOnlySpan<byte> fileHeader)
    {
        foreach (RejectedFormat format in _fileValidationSettings.RejectedFormats)
        {
            byte[] magicBytes = ConvertHexToBytes(format.MagicNumberHex);
            if (fileHeader.Length >= magicBytes.Length && fileHeader[..magicBytes.Length].SequenceEqual(magicBytes))
            {
                throw new InvalidOperationException(format.ErrorMessage);
            }
        }
    }

    private static byte[] ConvertHexToBytes(string hex)
    {
        int numberChars = hex.Length;
        byte[] bytes = new byte[numberChars / 2];
        for (int i = 0; i < numberChars; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }
        return bytes;
    }
}
