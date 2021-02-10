using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FuncSharp;

namespace Mews.Fiscalization.SignatureChecker.Model
{
    internal static class Parser
    {
        private static readonly CultureInfo FrenchCulture = new CultureInfo("fr-FR");

        public static ITry<decimal, IEnumerable<string>> ParseDecimal(string value)
        {
            var isSuccess = Decimal.TryParse(value.Replace('.', ',').Trim(), NumberStyles.Number, FrenchCulture, out var result);
            return isSuccess.ToTry(_ => result, _ => "Invalid number.".ToEnumerable());
        }

        public static ITry<Amount, IEnumerable<string>> ParseAmount(string stringValue)
        {
            var tokens = stringValue.Split('\u00A0', ' ').Where(t => !String.IsNullOrWhiteSpace(t)).ToList();
            return tokens.Count.Match(
                2, _ => ParseDecimal(tokens[0]).FlatMap(value => Amount.Create(value: value, currencyCodeOrSymbol: tokens[1].Trim())),
                _ => Try.Error<Amount, IEnumerable<string>>($"Invalid {nameof(Amount)}.".ToEnumerable())
            );
        }
    }
}