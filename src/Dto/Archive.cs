using System.Collections.Generic;
using FuncSharp;

namespace Mews.Fiscalization.SignatureChecker.Dto;

internal sealed class Archive
{
    internal Archive(File metadata, File signature, Option<CsvData> totals, Option<CsvData> taxTotals, IEnumerable<CsvData> invoiceFooters)
    {
        Metadata = metadata;
        Signature = signature;
        Totals = totals;
        TaxTotals = taxTotals;
        InvoiceFooters = invoiceFooters.AsReadOnlyList();
    }

    public File Metadata { get; }

    public File Signature { get; }

    public Option<CsvData> Totals { get; }

    public Option<CsvData> TaxTotals { get; }

    public IReadOnlyList<CsvData> InvoiceFooters { get; }
}