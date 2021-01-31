using System;
using System.Globalization;

namespace Mews.SignatureChecker
{
    public static class DecimalParser
    {
        private static readonly CultureInfo FrenchCulture = new CultureInfo("fr-FR");

        public static decimal Parse(string value)
        {
            return Decimal.Parse(value.Replace('.', ',').Trim(), FrenchCulture);
        }
    }}