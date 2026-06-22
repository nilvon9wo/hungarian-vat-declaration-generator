using CsvHelper;
using CsvHelper.Configuration;

namespace HungarianVatDeclarationGenerator.Api.Services;

public sealed class CsvReaderFactory(CsvConfiguration configuration) : ICsvReaderFactory
{
    private readonly CsvConfiguration _configuration = configuration;

    public CsvReader Create(StreamReader streamReader)
        => new(streamReader, _configuration);
}
