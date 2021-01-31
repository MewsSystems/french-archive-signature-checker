using System;
using System.Collections.Generic;
using System.Linq;

namespace Mews.SignatureChecker
{
    internal sealed class TaxSummary
    {
        public IReadOnlyDictionary<TaxRate, CurrencyValue> Data { get; }

        public TaxSummary(IReadOnlyDictionary<TaxRate, CurrencyValue> data)
        {
            Data = data;
        }

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
                g => CurrencyValue.Sum(g.Select(i => i.Value))
            ));
        }
    }
}