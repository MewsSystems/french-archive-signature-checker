namespace Mews.Fiscalization.SignatureChecker.Dto
{
    internal class Archive
    {
        internal Archive(ArchiveEntry metadata, ArchiveEntry signature, CsvData totals, CsvData taxTotals, CsvData invoiceFooter)
        {
            Metadata = metadata;
            Signature = signature;
            Totals = totals;
            TaxTotals = taxTotals;
            InvoiceFooter = invoiceFooter;
        }

        public ArchiveEntry Metadata { get; }

        public ArchiveEntry Signature { get; }

        public CsvData Totals { get; }

        public CsvData TaxTotals { get; }

        public CsvData InvoiceFooter { get; }
    }
}