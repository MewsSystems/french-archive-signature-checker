namespace Mews.Fiscalization.SignatureChecker.Dto
{
    internal class Archive
    {
        private Archive(ArchiveMetadata metadata, byte[] signature, ArchiveEntry totals, ArchiveEntry taxTotals, ArchiveEntry invoiceFooter)
        {
            Metadata = metadata;
            Signature = signature;
            Totals = totals;
            TaxTotals = taxTotals;
            InvoiceFooter = invoiceFooter;
        }

        public ArchiveMetadata Metadata { get; }

        public byte[] Signature { get; }

        public ArchiveEntry Totals { get; }

        public ArchiveEntry TaxTotals { get; }

        public ArchiveEntry InvoiceFooter { get; }
    }
}