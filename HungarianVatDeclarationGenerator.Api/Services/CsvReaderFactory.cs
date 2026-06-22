using CsvHelper;
using CsvHelper.Configuration;

namespace HungarianVatDeclarationGenerator.Api.Services;

public sealed class CsvReaderFactory(CsvConfiguration configuration) : ICsvReaderFactory
{
    private readonly CsvConfiguration _configuration = configuration
        ?? throw new ArgumentNullException(nameof(configuration));

    public CsvReader Create(StreamReader streamReader)
    {
        ArgumentNullException.ThrowIfNull(streamReader);
        return new CsvReader(streamReader, _configuration);
    }
}
