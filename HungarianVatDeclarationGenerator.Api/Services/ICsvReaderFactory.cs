using CsvHelper;

namespace HungarianVatDeclarationGenerator.Api.Services;

public interface ICsvReaderFactory
{
    CsvReader Create(StreamReader streamReader);
}
