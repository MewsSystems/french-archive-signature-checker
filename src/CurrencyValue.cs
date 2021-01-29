using System;
using System.Globalization;
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

        public static CurrencyValue Parse(string value)
        {
            var frenchCulture = new CultureInfo("fr-FR");
            var sanitizedVal = value.Replace('\u00A0', ' ');
            var val = Decimal.Parse(sanitizedVal, NumberStyles.Currency | NumberStyles.AllowDecimalPoint, frenchCulture);
            return new CurrencyValue(Currencies.Euro, val);
        }

        public static CurrencyValue Sum(params CurrencyValue[] values)
        {
            var currency = values.Select(v => v.Currency).Distinct().SingleOption().Get(_ => new ArgumentException("All values need to be in the same currency."));
            return new CurrencyValue(currency, values.Sum(v => v.Value));
        }
    }
}