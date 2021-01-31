using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FuncSharp;

namespace Mews.SignatureChecker
{
    internal static class ArchiveParser
    {
        public static ITry<TaxSummary, string> GetTaxSummary(Archive archive)
        {
            return archive.Metadata.Version.Match(
                "1.0", _ => GetV1TaxSummary(archive),
                "4.0", _ => GetV4TaxSummary(archive)
            );
        }

        public static ITry<CurrencyValue, string> GetReportedValue(Archive archive)
        {
            return archive.Metadata.Version.Match(
                "1.0", _ => GetV1ReportedValue(archive),
                "4.0", _ => GetV4ReportedValue(archive)
            );
        }

        private static ITry<TaxSummary, string> GetV1TaxSummary(Archive archive)
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

        private static ITry<CurrencyValue, string> GetV1ReportedValue(Archive archive)
        {
            return archive.ProcessEntry("TOTALS", e =>
            {
                var data = GetCsvData(e.Content, v => Decimal.Parse(v[3], CultureInfo.InvariantCulture));
                return new CurrencyValue(Currencies.Euro, data.Sum());
            });
        }

        private static ITry<TaxSummary, string> GetV4TaxSummary(Archive archive)
        {
            return archive.ProcessEntry("INVOICE_FOOTER", e =>
            {
                var taxBreakdownNet = GetCsvData(e.Content, v => ParseLineTaxSummary(v[1]));
                var taxBreakdownTax = GetCsvData(e.Content, v => ParseLineTaxSummary(v[2]));
                return TaxSummary.Sum(taxBreakdownNet.Concat(taxBreakdownTax));
            });
        }

        private static ITry<CurrencyValue, string> GetV4ReportedValue(Archive archive)
        {
            return archive.ProcessEntry("INVOICE_FOOTER", e =>
            {
                var values = GetCsvData(e.Content, v => CurrencyValue.Parse(v[18]));
                return CurrencyValue.Sum(values.ToArray());
            });
        }

        private static TaxSummary ParseLineTaxSummary(string value)
        {
            var values = value.Split('|');
            var data = values.Select(v =>
            {
                var parts = v.Split(':');
                var percentage = Decimal.Parse(parts[0].TrimEnd('%').Trim()) / 100;
                var currencyValue = CurrencyValue.Parse(parts[1]);
                return (percentage, currencyValue);
            }).ToArray();
            return new TaxSummary(data.GroupBy(d => d.percentage).ToDictionary(g => new TaxRate(g.Key), g => CurrencyValue.Sum(g.Select(i => i.currencyValue).ToArray())));
        }

        private static IReadOnlyList<T> GetCsvData<T>(string source, Func<string[], T> converter)
        {
            var lines = source.Split('\n').Skip(1).Where(l => !String.IsNullOrWhiteSpace(l));
            return lines.Select(l => converter(l.Split(';'))).ToList();
        }
    }
}