namespace Mews.Fiscalization.SignatureChecker.Dto;

internal static class ArchiveReader
{
    public static Try<Archive, IReadOnlyList<string>> CompileArchive(IReadOnlyList<File> files)
    {
        var metadata = GetFile(files, "METADATA.json");
        var signature = GetFile(files, "SIGNATURE.txt");
        var taxTotals = GetOptionalEntry(files, "TAX_TOTALS").Map(e => GetCsvData(e.Content));
        var totals = GetOptionalEntry(files, "TOTALS").Map(e => GetCsvData(e.Content));
        var invoiceFooters = GetFiles(files, "INVOICE_FOOTER").Select(f => GetCsvData(f.Content));

        return Try.Aggregate(
            metadata,
            signature,
            (m, s) => new Archive(Metadata: m, Signature: s, Totals: totals, TaxTotals: taxTotals, InvoiceFooters: invoiceFooters.ToReadOnlyList())
        );
    }

    private static Try<File, IReadOnlyList<string>> GetFile(IEnumerable<File> files, string namePrefix)
    {
        return GetOptionalEntry(files, namePrefix).ToTry(_ => $"No unique file found {namePrefix}*.".ToReadOnlyList());
    }

    private static IEnumerable<File> GetFiles(IEnumerable<File> files, string namePrefix)
    {
        return files.Where(f => f.Name.StartsWith(namePrefix));
    }

    private static Option<File> GetOptionalEntry(IEnumerable<File> files, string namePrefix)
    {
        return files.SingleOption(e => e.Name.StartsWith(namePrefix));
    }

    private static CsvData GetCsvData(string source)
    {
        var lines = source.Split('\n').Skip(1).Where(l => !string.IsNullOrWhiteSpace(l));
        return new CsvData(lines.Select(l => new CsvRow(l.Split(';'))).ToReadOnlyList());
    }
}