using HungarianVatDeclarationGenerator.Api.Configuration;
using HungarianVatDeclarationGenerator.Api.Models;

namespace HungarianVatDeclarationGenerator.Api.Services;

public sealed class VatCalculationService(VatCalculationSettings vatCalculationSettings) : IVatCalculationService
{
    private readonly VatCalculationSettings _vatCalculationSettings = vatCalculationSettings
        ?? throw new ArgumentNullException(nameof(vatCalculationSettings));

    public VatDeclarationResult Calculate(IReadOnlyList<Invoice> invoices)
    {
        if (invoices.Count == 0)
        {
            return VatDeclarationResult.Empty;
        }

        List<VatSummary> summariesByRate = [.. invoices
            .GroupBy(inv => inv.VatRate)
            .Select(group => new VatSummary
            {
                VatRate = group.Key,
                TotalNetAmount = RoundDecimal(group.Sum(inv => inv.NetAmount)),
                TotalVatAmount = RoundDecimal(group.Sum(inv => inv.VatAmount)),
                TotalGrossAmount = RoundDecimal(group.Sum(inv => inv.GrossAmount)),
                InvoiceCount = group.Count()
            })
            .OrderBy(summary => summary.VatRate)];

        return new VatDeclarationResult
        {
            SummariesByVatRate = summariesByRate,
            GrandTotalNet = RoundDecimal(summariesByRate.Sum(s => s.TotalNetAmount)),
            GrandTotalVat = RoundDecimal(summariesByRate.Sum(s => s.TotalVatAmount)),
            GrandTotalGross = RoundDecimal(summariesByRate.Sum(s => s.TotalGrossAmount)),
            TotalInvoiceCount = summariesByRate.Sum(s => s.InvoiceCount)
        };
    }

    private decimal RoundDecimal(decimal value)
        => Math.Round(value, _vatCalculationSettings.DecimalPlaces);
}
