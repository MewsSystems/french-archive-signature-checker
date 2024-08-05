using System.Collections.Generic;
using System.Linq;
using FuncSharp;

namespace Mews.Fiscalization.SignatureChecker.Model;

internal sealed class TaxSummary
{
    private TaxSummary(IReadOnlyDictionary<TaxRate, Amount> data)
    {
        Data = data;
    }

    public IReadOnlyDictionary<TaxRate, Amount> Data { get; }

    public static Try<TaxSummary, IReadOnlyList<string>> Create(Dto.Archive archive, ArchiveVersion version)
    {
        return version.Match(
            ArchiveVersion.v100, _ => GetV1TaxSummary(archive),
            ArchiveVersion.v400, _ => GetV4TaxSummary(archive),
            ArchiveVersion.v410, _ => GetV4TaxSummary(archive),
            ArchiveVersion.v411, _ => GetV4TaxSummary(archive)
        );
    }

    private static TaxSummary Sum(IEnumerable<TaxSummary> summaries)
    {
        var summaryData = summaries.SelectMany(s => s.Data);
        var valuesByTaxRate = summaryData.GroupBy(d => d.Key);
        return new TaxSummary(valuesByTaxRate.ToDictionary(
            g => g.Key,
            g => Amount.Sum(g.Select(i => i.Value))
        ));
    }

    private static Try<TaxSummary, IReadOnlyList<string>> GetV1TaxSummary(Dto.Archive archive)
    {
        var taxTotals = archive.TaxTotals.ToTry(_ => "Tax totals file not found.".ToReadOnlyList());
        var data = taxTotals.FlatMap(t => Try.Aggregate(t.Rows.Select(row =>
        {
            return Try.Aggregate(
                Parser.ParseDecimal(row.Values[4]),
                Parser.ParseDecimal(row.Values[10]).FlatMap(v => Amount.Create(v, "EUR")),
                (r, v) => (TaxRate: new TaxRate(r), TaxValue: v)
            );
        })));

        return data.Map(lines => new TaxSummary(lines.GroupBy(l => l.TaxRate).ToDictionary(
            g => g.Key,
            g => Amount.Sum(g.Select(value => value.TaxValue))
        )));
    }

    private static Try<TaxSummary, IReadOnlyList<string>> GetV4TaxSummary(Dto.Archive archive)
    {
        return archive.InvoiceFooters.AsNonEmpty().Match(
            a =>
            {
                var taxBreakdownNet = Try.Aggregate(a.SelectMany(f => f.Rows.Select(row => ParseLineTaxSummary(row.Values[1]))));
                var taxBreakdownTax = Try.Aggregate(a.SelectMany(f => f.Rows.Select(row => ParseLineTaxSummary(row.Values[2]))));
                return Try.Aggregate(
                    taxBreakdownNet,
                    taxBreakdownTax,
                    (net, tax) => Sum(net.Concat(tax))
                );
            },
            _ => Try.Error<TaxSummary, IReadOnlyList<string>>("Invoice footer file/s not found.".ToReadOnlyList())
        );
    }

    private static Try<TaxSummary, IReadOnlyList<string>> ParseLineTaxSummary(string value)
    {
        var rawValues = value.Split('|').Where(v => !string.IsNullOrEmpty(v));
        var parsedValues = rawValues.Select(v =>
        {
            var parts = v.Split(':');
            var percentage = Parser.ParseDecimal(parts[0].TrimEnd('%').Trim());
            var amount = Parser.ParseAmount(parts[1]);
            return percentage.FlatMap(p => amount.Map(a => (Percentage: p / 100, Amount: a)));
        });

        return Try.Aggregate(parsedValues).Map(values =>
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