using System;
using System.Collections.Generic;
using System.Linq;
using Mews.Fiscalization.SignatureChecker.Model;

namespace Mews.Fiscalization.SignatureChecker
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
    }
}