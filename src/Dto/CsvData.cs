namespace Mews.Fiscalization.SignatureChecker.Dto;

internal sealed record CsvData(IReadOnlyList<CsvRow> Rows);