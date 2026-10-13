namespace IstpetDev.Application.Imports;

public static class ImportLimits
{
    public const long FileBytes = 10 * 1024 * 1024;
    public const long RequestBytes = 50 * 1024 * 1024;
    public const long ExpandedBytes = 100 * 1024 * 1024;
    public const int ProfileBytes = 64 * 1024;
    public const int Rows = 100_000;
    public const int Columns = 128;
    public const int CellCharacters = 4096;
    public const decimal MaximumQuantity = 999_999_999_999_999.999m;
}
