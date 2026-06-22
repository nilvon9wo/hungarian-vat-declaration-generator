using HungarianVatDeclarationGenerator.Api.Models;

namespace HungarianVatDeclarationGenerator.Api.Services;

public sealed class VatCalculationService : IVatCalculationService
{
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
                TotalNetAmount = group.Sum(inv => inv.NetAmount),
                TotalVatAmount = group.Sum(inv => inv.VatAmount),
                TotalGrossAmount = group.Sum(inv => inv.GrossAmount),
                InvoiceCount = group.Count()
            })
            .OrderBy(summary => summary.VatRate)];

        return new VatDeclarationResult
        {
            SummariesByVatRate = summariesByRate,
            GrandTotalNet = summariesByRate.Sum(s => s.TotalNetAmount),
            GrandTotalVat = summariesByRate.Sum(s => s.TotalVatAmount),
            GrandTotalGross = summariesByRate.Sum(s => s.TotalGrossAmount),
            TotalInvoiceCount = summariesByRate.Sum(s => s.InvoiceCount)
        };
    }
}
