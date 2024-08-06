using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using FuncSharp;

namespace Mews.Fiscalization.SignatureChecker.Model;

internal static class Parser
{
    private static readonly CultureInfo FrenchCulture = new("fr-FR");
    private static readonly Regex AmountRegex = new(@"([-−]?[\d\s\\.,]*[\d\\.,])\s*([^\d\s\.]+)?");

    public static Try<decimal, IReadOnlyList<string>> ParseDecimal(string value)
    {
        var isSuccess = decimal.TryParse(Regex.Replace(value, @"\s+", "").Replace('.', ',').Replace('−', '-'), NumberStyles.Number, FrenchCulture, out var result);
        return isSuccess.ToTry(_ => result, _ => "Invalid number.".ToReadOnlyList());
    }

    public static Try<Amount, IReadOnlyList<string>> ParseAmount(string stringValue)
    {
        var amountParts = AmountRegex.Match(stringValue);
        var value = amountParts.Groups[1].Value;
        var currencyCodeOrSymbol = amountParts.Groups[2].Value;
        return ParseDecimal(value).FlatMap(v => Amount.Create(value: v, currencyCodeOrSymbol: currencyCodeOrSymbol));
    }
}