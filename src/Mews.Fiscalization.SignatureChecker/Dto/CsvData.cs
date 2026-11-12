namespace Mews.Fiscalization.SignatureChecker.Dto;

internal sealed class CsvData(string name, IEnumerable<CsvRow> rows)
{
    public string Name { get; } = name;

    public IReadOnlyList<CsvRow> Rows { get; } = rows.AsReadOnlyList();
}
