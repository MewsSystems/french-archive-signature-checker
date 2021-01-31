using System;
using System.Collections.Generic;
using System.Linq;
using FuncSharp;

namespace Mews.SignatureChecker
{
    internal sealed class CurrencyValue
    {
        public CurrencyValue(Currency currency, decimal value)
        {
            Currency = currency;
            Value = value;
        }

        public Currency Currency { get; }

        public decimal Value { get; }

        public string ToSignatureString()
        {
            return ((int)(Value * Currency.NormalizationConstant)).ToString();
        }

        public static CurrencyValue Parse(string stringValue)
        {
            var tokens = stringValue.Split('\u00A0', ' ').Where(t => !String.IsNullOrWhiteSpace(t)).ToList();
            return tokens.Count.Match(
                2, _ =>
                {
                    var value = DecimalParser.Parse(tokens[0]);
                    var currency = Currencies.GetBySymbolOrCode(tokens[1].Trim()).Get(e => new Exception(e));
                    return new CurrencyValue(currency, value);
                },
                _ => throw new ArgumentException($"Invalid {nameof(CurrencyValue)}.", nameof(stringValue))
            );
        }

        public static CurrencyValue Sum(params CurrencyValue[] values)
        {
            var currency = values.Select(v => v.Currency).Distinct().SingleOption().Get(_ => new ArgumentException("All values need to be in the same currency."));
            return new CurrencyValue(currency, values.Sum(v => v.Value));
        }

        public static CurrencyValue Sum(IEnumerable<CurrencyValue> values)
        {
            return Sum(values.ToArray());
        }
    }
}