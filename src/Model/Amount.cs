using System;
using System.Collections.Generic;
using System.Linq;
using FuncSharp;

namespace Mews.Fiscalization.SignatureChecker.Model
{
    internal sealed class Amount
    {
        public Amount(Currency currency, decimal value)
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

        public static Amount Sum(params Amount[] values)
        {
            var currency = values.Select(v => v.Currency).Distinct().SingleOption().Get(_ => new ArgumentException("All values need to be in the same currency."));
            return new Amount(currency, values.Sum(v => v.Value));
        }

        public static Amount Sum(IEnumerable<Amount> values)
        {
            return Sum(values.ToArray());
        }
    }
}