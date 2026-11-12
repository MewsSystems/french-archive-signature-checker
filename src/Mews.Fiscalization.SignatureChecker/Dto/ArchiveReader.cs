namespace Mews.Fiscalization.SignatureChecker.Dto;

internal static class ArchiveReader
{
    public static Try<Archive, IReadOnlyList<string>> CompileArchive(IReadOnlyList<File> files)
    {
        var metadata = GetFile(files, "METADATA.json");
        var signature = GetFile(files, "SIGNATURE.txt");
        var taxTotals = GetOptionalEntry(files, "TAX_TOTALS").Map(e => GetCsvData(e));
        var totals = GetOptionalEntry(files, "TOTALS").Map(e => GetCsvData(e));
        var invoiceFooters = GetFiles(files, "INVOICE_FOOTER").Select(f => GetCsvData(f));

        return Try.Aggregate(
            metadata,
            signature,
            (m, s) => new Archive(metadata: m, signature: s, totals: totals, taxTotals: taxTotals, invoiceFooters: invoiceFooters)
        );
    }

    private static Try<File, IReadOnlyList<string>> GetFile(IReadOnlyList<File> files, string namePrefix)
    {
        return GetOptionalEntry(files, namePrefix).ToTry(_ => $"No unique file found {namePrefix}*.".ToReadOnlyList());
    }

    private static IEnumerable<File> GetFiles(IEnumerable<File> files, string namePrefix)
    {
        return files.Where(f => f.Name.StartsWith(namePrefix));
    }

    private static Option<File> GetOptionalEntry(IReadOnlyList<File> files, string namePrefix)
    {
        return files.SingleOption(e => e.Name.StartsWith(namePrefix));
    }

    private static CsvData GetCsvData(File file)
    {
        var lines = file.Content.Split('\n').Select((line, index) => (Line: line, Number: index + 1)).Skip(1).Where(l => !string.IsNullOrWhiteSpace(l.Line));
        return new CsvData(file.Name, lines.Select(l => new CsvRow(l.Number, l.Line.Split(';'))));
    }
}