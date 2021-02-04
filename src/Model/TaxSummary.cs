using System;
using System.Collections.Generic;
using System.Linq;
using FuncSharp;

namespace Mews.Fiscalization.SignatureChecker.Model
{
    internal sealed class TaxSummary
    {
        public TaxSummary(IReadOnlyDictionary<TaxRate, Amount> data)
        {
            Data = data;
        }

        public IReadOnlyDictionary<TaxRate, Amount> Data { get; }

        public string ToSignatureString()
        {
            var parts = Data.OrderByDescending(d => d.Key).Select(d => $"{d.Key.ToSignatureString()}:{d.Value.ToSignatureString()}");
            return String.Join("|", parts);
        }

        public static TaxSummary Sum(IEnumerable<TaxSummary> summaries)
        {
            var summaryData = summaries.SelectMany(s => s.Data);
            var valuesByTaxRate = summaryData.GroupBy(d => d.Key);
            return new TaxSummary(valuesByTaxRate.ToDictionary(
                g => g.Key,
                g => Amount.Sum(g.Select(i => i.Value))
            ));
        }

        public static ITry<TaxSummary, IEnumerable<string>> Create(Dto.ArchiveEntry summary)
        {
            return archive.Metadata.Version.Match(
                "1.0", _ => GetV1TaxSummary(archive),
                "4.0", _ => GetV4TaxSummary(archive)
            );
        }



        private static ITry<TaxSummary, string> GetV1TaxSummary(Dto.ArchiveEntry entry)
        {
            var data = entry.Content.GetCsvData(l => new
            {
                TaxRate = ParseDecimal(l[4]),
                TaxValue = ParseDecimal(l[10])
            });
            var lines = data.GroupBy(l => l.TaxRate).ToDictionary(
                g => new TaxRate(g.Key),
                g => new Amount(Currencies.Euro, g.Sum(v => v.TaxValue))
            );
            return new TaxSummary(lines);
        }

        private static ITry<TaxSummary, string> GetV4TaxSummary(Dto.ArchiveEntry entry)
        {
            var taxBreakdownNet = entry.Content.GetCsvData(v => ParseLineTaxSummary(v[1]));
            var taxBreakdownTax = entry.Content.GetCsvData(v => ParseLineTaxSummary(v[2]));
            return TaxSummary.Sum(taxBreakdownNet.Concat(taxBreakdownTax));
        }

        private static ITry<TaxSummary, string> ParseLineTaxSummary(string value)
        {
            var parsedValues = value.Split('|').Select(v =>
            {
                var parts = v.Split(':');
                var percentage = ParseDecimal(parts[0].TrimEnd('%').Trim());
                var amount = ParseAmount(parts[1]);
                return percentage.FlatMap(p => amount.Map(a => (Percentage: p / 100, Amount: a)));
            });

            return Try.Aggregate(parsedValues).MapError(e => e.First()).Map(values =>
            {
                var valuesByTaxRatePercentage = values.GroupBy(v => v.Percentage);
                var valuesByTaxRate = valuesByTaxRatePercentage.ToDictionary(
                    g => new TaxRate(g.Key),
                    g => Amount.Sum(g.Select(v => v.Amount))
                );
                return new TaxSummary(valuesByTaxRate);
            });
        }
    }
}