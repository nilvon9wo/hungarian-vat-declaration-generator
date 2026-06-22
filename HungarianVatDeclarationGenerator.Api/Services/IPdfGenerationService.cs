using HungarianVatDeclarationGenerator.Api.Models;

namespace HungarianVatDeclarationGenerator.Api.Services;

public interface IPdfGenerationService
{
    /// <summary>
    /// Generates a PDF report from VAT declaration results.
    /// Returns the PDF as a byte array.
    /// </summary>
    byte[] GeneratePdf(VatDeclarationResult result);
}
