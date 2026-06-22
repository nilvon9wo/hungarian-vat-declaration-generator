namespace HungarianVatDeclarationGenerator.Api.Constants;

public static class VatRates
{
    public static readonly int[] Supported = [5, 18, 27];

    public static bool IsValid(int rate)
        => Supported.Contains(rate);
}
