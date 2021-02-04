using FuncSharp;

namespace Mews.Fiscalization.SignatureChecker.Dto
{
    internal class Archive
    {
        internal Archive(ArchiveEntry metadata, ArchiveEntry signature, IOption<CsvData> totals, IOption<CsvData> taxTotals, IOption<CsvData> invoiceFooter)
        {
            Metadata = metadata;
            Signature = signature;
            Totals = totals;
            TaxTotals = taxTotals;
            InvoiceFooter = invoiceFooter;
        }

        public ArchiveEntry Metadata { get; }

        public ArchiveEntry Signature { get; }

        public IOption<CsvData> Totals { get; }

        public IOption<CsvData> TaxTotals { get; }

        public IOption<CsvData> InvoiceFooter { get; }
    }
}