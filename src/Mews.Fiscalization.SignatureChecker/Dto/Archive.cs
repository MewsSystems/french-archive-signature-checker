namespace Mews.Fiscalization.SignatureChecker.Dto;

internal sealed class Archive(File metadata, File signature, Option<CsvData> totals, Option<CsvData> taxTotals, IEnumerable<CsvData> invoiceFooters)
{
    public File Metadata { get; } = metadata;

    public File Signature { get; } = signature;

    public Option<CsvData> Totals { get; } = totals;

    public Option<CsvData> TaxTotals { get; } = taxTotals;

    public IReadOnlyList<CsvData> InvoiceFooters { get; } = invoiceFooters.AsReadOnlyList();
}
