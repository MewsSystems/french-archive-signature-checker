using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FuncSharp;

namespace Mews.Fiscalization.SignatureChecker.Model;

internal sealed class Amount
{
    private Amount(decimal value)
    {
        Value = value;
    }

    public decimal Value { get; }

    public static ITry<Amount, IEnumerable<string>> Create(decimal value, string currencyCodeOrSymbol)
    {
        return currencyCodeOrSymbol.Match(
            "€", _ => Try.Success<Amount, IEnumerable<string>>(new Amount(value)),
            "EUR", _ => Try.Success<Amount, IEnumerable<string>>(new Amount(value)),
            _ => Try.Error<Amount, IEnumerable<string>>("Currency not found.".ToEnumerable())
        );
    }

    public static Amount Sum(IEnumerable<Amount> values)
    {
        return new Amount(values.Sum(v => v.Value));
    }

    public override string ToString()
    {
        return Value.ToString(CultureInfo.InvariantCulture);
    }
}