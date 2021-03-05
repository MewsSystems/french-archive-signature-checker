using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using FuncSharp;

namespace Mews.Fiscalization.SignatureChecker.Model
{
    internal static class Parser
    {
        private static readonly CultureInfo FrenchCulture = new CultureInfo("fr-FR");
        private static readonly Regex AmountRegex = new Regex(@"([-−]?[\d\s\\.]*[\d\\.])\s*([^\d\s\.]+)$");

        public static ITry<decimal, IEnumerable<string>> ParseDecimal(string value)
        {
            var isSuccess = Decimal.TryParse(value.Replace('.', ',').Replace('−', '-').Trim(), NumberStyles.Number, FrenchCulture, out var result);
            return isSuccess.ToTry(_ => result, _ => "Invalid number.".ToEnumerable());
        }

        public static ITry<Amount, IEnumerable<string>> ParseAmount(string stringValue)
        {
            var amountParts = AmountRegex.Match(stringValue.Replace(',', '.'));
            var value = amountParts.Groups[1].Value;
            var currencyCodeOrSymbol = amountParts.Groups[2].Value;
            return ParseDecimal(value).FlatMap(v => Amount.Create(value: v, currencyCodeOrSymbol: currencyCodeOrSymbol));
        }
    }
}