using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FuncSharp;

namespace Mews.SignatureChecker
{
    internal static class ArchiveParser
    {
        public static Try<TaxSummary, string> GetTaxSummary(Archive archive)
        {
            return archive.Metadata.Version.Match(
                "1.0", _ => GetV1TaxSummary(archive),
                "4.0", _ => GetV4TaxSummary(archive)
            );
        }

        public static Try<CurrencyValue, string> GetReportedValue(Archive archive)
        {
            return archive.Metadata.Version.Match(
                "1.0", _ => GetV1ReportedValue(archive),
                "4.0", _ => GetV4ReportedValue(archive)
            );
        }

        private static Try<TaxSummary, string> GetV1TaxSummary(Archive archive)
        {
            return archive.ProcessEntry("TAX_TOTALS", e =>
            {
                var data = GetCsvData(e.Content, l => new
                {
                    TaxRate = Decimal.Parse(l[4], CultureInfo.InvariantCulture),
                    TaxValue = Decimal.Parse(l[10], CultureInfo.InvariantCulture)
                });
                var lines = data.GroupBy(l => l.TaxRate).ToDictionary(
                    g => new TaxRate(g.Key),
                    g => new CurrencyValue(Currencies.Euro, g.Sum(v => v.TaxValue))
                );
                return new TaxSummary(lines);
            });
        }

        private static Try<CurrencyValue, string> GetV1ReportedValue(Archive archive)
        {
            return archive.ProcessEntry("TOTALS", e =>
            {
                var data = GetCsvData(e.Content, v => Decimal.Parse(v[3], CultureInfo.InvariantCulture));
                return new CurrencyValue(Currencies.Euro, data.Sum());
            });
        }

        private static Try<TaxSummary, string> GetV4TaxSummary(Archive archive)
        {
            return archive.ProcessEntry("INVOICE_FOOTER", e =>
            {
                var taxBreakdownNet = GetCsvData(e.Content, v => ParseLineTaxSummary(v[1]));
                var taxBreakdownTax = GetCsvData(e.Content, v => ParseLineTaxSummary(v[2]));
                return TaxSummary.Sum(taxBreakdownNet.Concat(taxBreakdownTax));
            });
        }

        private static Try<CurrencyValue, string> GetV4ReportedValue(Archive archive)
        {
            return archive.ProcessEntry("INVOICE_FOOTER", e =>
            {
                var values = GetCsvData(e.Content, v => CurrencyValue.Parse(v[18]));
                return CurrencyValue.Sum(values);
            });
        }

        private static TaxSummary ParseLineTaxSummary(string value)
        {
            throw new NotImplementedException();
        }

        private static IReadOnlyList<T> GetCsvData<T>(string source, Func<string[], T> converter)
        {
            var lines = source.Split('\n').Skip(1);
            return lines.Select(l => converter(l.Split(';'))).ToList();
        }
    }
}