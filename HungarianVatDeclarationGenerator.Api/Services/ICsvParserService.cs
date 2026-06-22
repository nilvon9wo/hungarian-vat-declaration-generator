using HungarianVatDeclarationGenerator.Api.Models;

namespace HungarianVatDeclarationGenerator.Api.Services;

public interface ICsvParserService
{
    /// <summary>
    /// Parses CSV content and returns a list of valid invoices.
    /// Throws InvalidOperationException if CSV format is invalid or contains invalid data.
    /// </summary>
    Task<IReadOnlyList<Invoice>> Parse(Stream csvStream, CancellationToken cancellationToken = default);
}
