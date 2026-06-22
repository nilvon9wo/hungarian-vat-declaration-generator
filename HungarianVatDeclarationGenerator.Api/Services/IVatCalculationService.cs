using HungarianVatDeclarationGenerator.Api.Models;

namespace HungarianVatDeclarationGenerator.Api.Services;

public interface IVatCalculationService
{
    /// <summary>
    /// Calculates VAT summaries grouped by VAT rate from a list of invoices.
    /// </summary>
    VatDeclarationResult Calculate(IReadOnlyList<Invoice> invoices);
}
