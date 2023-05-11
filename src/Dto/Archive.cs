using System.Collections.Generic;
using System.Linq;
using FuncSharp;

namespace Mews.Fiscalization.SignatureChecker.Dto
{
    internal class Archive
    {
        internal Archive(File metadata, File signature, IOption<CsvData> totals, IOption<CsvData> taxTotals, IEnumerable<CsvData> invoiceFooters)
        {
            Metadata = metadata;
            Signature = signature;
            Totals = totals;
            TaxTotals = taxTotals;
            InvoiceFooters = invoiceFooters.ToList();
        }

        public File Metadata { get; }

        public File Signature { get; }

        public IOption<CsvData> Totals { get; }

        public IOption<CsvData> TaxTotals { get; }

        public IReadOnlyList<CsvData> InvoiceFooters { get; }
    }
}