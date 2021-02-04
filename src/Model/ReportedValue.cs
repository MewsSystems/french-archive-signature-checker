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
                ArchiveVersion.v120, _ => GetReportedValueV1(archive),
                ArchiveVersion.v400, _ => GetReportedValueV4(archive)
            );

            return reportedValue.Map(value => new ReportedValue(value));
        }

        private static ITry<Amount, IEnumerable<string>> GetReportedValueV1(Dto.Archive archive)
        {
            var data = archive.Totals.Rows.Select(row => Parser.ParseDecimal(row.Values[3]));
            return Try.Aggregate(data).FlatMap(values => Amount.Create(values.Sum(), "EUR"));
        }

        private static ITry<Amount, IEnumerable<string>> GetReportedValueV4(Dto.Archive archive)
        {
            var values = archive.InvoiceFooter.Rows.Select(row => Parser.ParseAmount(row.Values[18]));
            return Try.Aggregate(values).Map(v => Amount.Sum(v));
        }
    }
}