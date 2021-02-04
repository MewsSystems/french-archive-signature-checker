using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FuncSharp;
using Mews.Fiscalization.SignatureChecker.Dto;

namespace Mews.Fiscalization.SignatureChecker.Model
{
    internal static class ArchiveParser
    {
        private static readonly CultureInfo FrenchCulture = new CultureInfo("fr-FR");

        public static ITry<TaxSummary, string> GetTaxSummary(Archive archive)
        {
            return archive.Metadata.Version.Match(
                "1.0", _ => GetV1TaxSummary(archive),
                "4.0", _ => GetV4TaxSummary(archive)
            );
        }

        public static ITry<Amount, string> GetReportedValue(Archive archive)
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
                    TaxRate = ParseDecimal(l[4]),
                    TaxValue = ParseDecimal(l[10])
                });
                var lines = data.GroupBy(l => l.TaxRate).ToDictionary(
                    g => new TaxRate(g.Key),
                    g => new Model.Amount(Currencies.Euro, g.Sum(v => v.TaxValue))
                );
                return new TaxSummary(lines);
            });
        }

        private static ITry<Amount, string> GetV1ReportedValue(Archive archive)
        {
            return archive.ProcessEntry("TOTALS", e =>
            {
                var data = GetCsvData(e.Content, v => Decimal.Parse(v[3], CultureInfo.InvariantCulture));
                return new Amount(Currencies.Euro, data.Sum());
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

        private static ITry<Amount, string> GetV4ReportedValue(Archive archive)
        {
            return archive.ProcessEntry("INVOICE_FOOTER", e =>
            {
                var values = GetCsvData(e.Content, v => ParseAmount(v[18]));
                return Amount.Sum(values.ToArray());
            });
        }

        private static TaxSummary ParseLineTaxSummary(string value)
        {
            var values = value.Split('|');
            var data = values.Select(v =>
            {
                var parts = v.Split(':');
                var percentage = ParseDecimal(parts[0].TrimEnd('%').Trim()) / 100;
                var currencyValue = ParseAmount(parts[1]);
                return (percentage, currencyValue);
            });
            var valuesByTaxRatePercentage = data.GroupBy(d => d.percentage);
            var valueByTaxRate = valuesByTaxRatePercentage.ToDictionary(
                g => new TaxRate(g.Key),
                g => Model.Amount.Sum(g.Select(i => i.currencyValue))
            );
            return new TaxSummary(valueByTaxRate);
        }

        private static IReadOnlyList<T> GetCsvData<T>(string source, Func<string[], T> converter)
        {
            var lines = source.Split('\n').Skip(1).Where(l => !String.IsNullOrWhiteSpace(l));
            return lines.Select(l => converter(l.Split(';'))).ToList();
        }

        public static Amount ParseAmount(string stringValue)
        {
            var tokens = stringValue.Split('\u00A0', ' ').Where(t => !String.IsNullOrWhiteSpace(t)).ToList();
            return tokens.Count.Match(
                2, _ =>
                {
                    var value = ParseDecimal(tokens[0]);
                    var currency = Currencies.GetBySymbolOrCode(tokens[1].Trim()).Get(e => new Exception(e));
                    return new Amount(currency, value);
                },
                _ => throw new ArgumentException($"Invalid {nameof(Amount)}.", nameof(stringValue))
            );
        }

        public static decimal ParseDecimal(string value)
        {
            return Decimal.Parse(value.Replace('.', ',').Trim(), FrenchCulture);
        }
    }
}