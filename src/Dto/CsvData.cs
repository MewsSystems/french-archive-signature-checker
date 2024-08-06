namespace Mews.Fiscalization.SignatureChecker.Dto;

internal sealed class CsvData
{
    internal CsvData(IEnumerable<CsvRow> rows)
    {
        Rows = rows.AsReadOnlyList();
    }

    public IReadOnlyList<CsvRow> Rows { get; }
}