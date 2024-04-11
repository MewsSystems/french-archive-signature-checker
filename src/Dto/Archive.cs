namespace Mews.Fiscalization.SignatureChecker.Dto;

internal sealed record Archive(File Metadata, File Signature, Option<CsvData> Totals, Option<CsvData> TaxTotals, IReadOnlyList<CsvData> InvoiceFooters);