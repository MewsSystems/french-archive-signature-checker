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

    public static Try<Amount, IReadOnlyList<string>> Create(decimal value, string currencyCodeOrSymbol)
    {
        return currencyCodeOrSymbol.Match(
            "€", _ => Try.Success<Amount, IReadOnlyList<string>>(new Amount(value)),
            "EUR", _ => Try.Success<Amount, IReadOnlyList<string>>(new Amount(value)),
            e => Try.Error<Amount, IReadOnlyList<string>>($"The provided ({e}) currency is not supported.".ToReadOnlyList())
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