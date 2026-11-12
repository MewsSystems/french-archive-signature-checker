namespace Mews.Fiscalization.SignatureChecker.Model;

/// <summary>
/// Checks that the files the signature is computed from exist and have the columns that are read from them,
/// so a malformed archive is reported as an error instead of failing while it is parsed.
/// </summary>
internal static class ArchiveStructure
{
    // Highest column index read from each file, plus one.
    internal const int TaxTotalsColumns = 11;
    internal const int TotalsColumns = 4;
    internal const int InvoiceFooterColumns = 19;

    public static Try<Dto.Archive, IReadOnlyList<string>> Validate(Dto.Archive archive, ArchiveVersion version)
    {
        var errors = version switch
        {
            ArchiveVersion.v100 => GetVersion1Errors(archive),
            ArchiveVersion.v400 or ArchiveVersion.v410 or ArchiveVersion.v411 => GetVersion4Errors(archive),
            _ => [$"Unsupported archive version {version}."]
        };
        return errors.Count == 0
            ? Try.Success<Dto.Archive, IReadOnlyList<string>>(archive)
            : Try.Error<Dto.Archive, IReadOnlyList<string>>(errors);
    }

    private static List<string> GetVersion1Errors(Dto.Archive archive)
    {
        var taxTotalsErrors = archive.TaxTotals.Match(t => GetColumnErrors(t, TaxTotalsColumns), _ => ["Tax totals file not found."]);
        var totalsErrors = archive.Totals.Match(t => GetColumnErrors(t, TotalsColumns), _ => ["Totals file not found."]);
        return [.. taxTotalsErrors, .. totalsErrors];
    }

    private static List<string> GetVersion4Errors(Dto.Archive archive)
    {
        if (archive.InvoiceFooters.Count == 0)
        {
            return ["Invoice footer file/s not found."];
        }
        return archive.InvoiceFooters.SelectMany(f => GetColumnErrors(f, InvoiceFooterColumns)).ToList();
    }

    private static List<string> GetColumnErrors(Dto.CsvData data, int requiredColumns)
    {
        return data.Rows
            .Where(r => r.Values.Count < requiredColumns)
            .Select(r => $"Invalid data ({data.Name}, line {r.LineNumber}): expected at least {requiredColumns} columns, found {r.Values.Count}.")
            .ToList();
    }
}
