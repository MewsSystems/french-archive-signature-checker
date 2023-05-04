using System.Collections.Generic;
using System.Linq;
using FuncSharp;

namespace Mews.Fiscalization.SignatureChecker.Model
{
    internal sealed class ReportedValue
    {
        private ReportedValue(Amount value)
        {
            Value = value;
        }

        public Amount Value { get; }

        public static ITry<ReportedValue, IEnumerable<string>> Create(Dto.Archive archive, ArchiveVersion version)
        {
            var reportedValue = version.Match(
                ArchiveVersion.v100, _ => GetReportedValueV1(archive),
                ArchiveVersion.v400, _ => GetReportedValueV4(archive),
                ArchiveVersion.v410, _ => GetReportedValueV4(archive),
                ArchiveVersion.v411, _ => GetReportedValueV4(archive)
            );

            return reportedValue.Map(value => new ReportedValue(value));
        }

        private static ITry<Amount, IEnumerable<string>> GetReportedValueV1(Dto.Archive archive)
        {
            var totals = archive.Totals.ToTry(_ => "Totals file not found.".ToEnumerable());
            var data = totals.FlatMap(t => Try.Aggregate(t.Rows.Select(row => Parser.ParseDecimal(row.Values[3]))));
            return data.FlatMap(values => Amount.Create(values.Sum(), "EUR"));
        }

        private static ITry<Amount, IEnumerable<string>> GetReportedValueV4(Dto.Archive archive)
        {
            var invoiceFooter = archive.InvoiceFooter.ToTry(_ => "Invoice footer file not found.".ToEnumerable());
            var values = invoiceFooter.FlatMap(i => Try.Aggregate(i.Rows.Select(row => Parser.ParseAmount(row.Values[18]))));
            return values.Map(v => Amount.Sum(v));
        }
    }
}