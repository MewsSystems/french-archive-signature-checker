using System;
using System.Collections.Generic;
using System.Linq;
using Mews.Fiscalization.SignatureChecker.Model;

namespace Mews.Fiscalization.SignatureChecker
{
    internal static class Extensions
    {
        internal static IEnumerable<T> ToEnumerable<T>(this T value)
        {
            return new List<T>{value};
        }

        internal static string MkLines(this IEnumerable<string> values)
        {
            return $"{String.Join(Environment.NewLine, values)}";
        }

        internal static string ToSignatureString(this DateTime dateTime)
        {
            return dateTime.ToString("yyyyMMddHHmmss");
        }

        internal static string ToSignatureString(this Amount amount)
        {
            return ((int)(amount.Value * 100)).ToString();
        }

        internal static string ToSignatureString(this TaxSummary taxSummary)
        {
            var parts = taxSummary.Data.OrderByDescending(d => d.Key).Select(d => $"{d.Key.ToSignatureString()}:{d.Value.ToSignatureString()}");
            return String.Join("|", parts);
        }

        internal static string ToSignatureString(this TaxRate taxRate)
        {
            var rateNormalizationConstant = 100 * 100;
            return ((int)(taxRate.Value * rateNormalizationConstant)).ToString().PadLeft(4, '0');
        }

        public static Tuple<IReadOnlyCollection<T>, IReadOnlyCollection<T>> Partition<T>(this IEnumerable<T> e, Func<T, bool> predicate)
        {
            var passing = new List<T>();
            var violating = new List<T>();
            foreach (var i in e)
            {
                (predicate(i) ? passing : violating).Add(i);
            }
            return Tuple.Create<IReadOnlyCollection<T>, IReadOnlyCollection<T>>(passing, violating);
        }

    }
}