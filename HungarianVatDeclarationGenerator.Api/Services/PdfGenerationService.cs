using HungarianVatDeclarationGenerator.Api.Models;

namespace HungarianVatDeclarationGenerator.Api.Services;

public sealed class PdfGenerationService : IPdfGenerationService
{
    public byte[] GeneratePdf(VatDeclarationResult result) =>
        // TODO: Implement PDF generation using QuestPDF
        // This is a placeholder that will be implemented in the next step
        throw new NotImplementedException("PDF generation will be implemented in the next step");
}
