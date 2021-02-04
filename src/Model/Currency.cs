using FuncSharp;

namespace Mews.Fiscalization.SignatureChecker
{
    internal sealed class Currency
    {
        public Currency(string code, string symbol, int normalizationConstant)
        {
            Code = code;
            Symbol = symbol;
            NormalizationConstant = normalizationConstant;
        }

        public string Code { get; }

        public string Symbol { get; }

        public int NormalizationConstant { get; }
    }

    internal static class Currencies
    {
        static Currencies()
        {
            Euro = new Currency(code: "EUR", symbol: "€", normalizationConstant: 100);
        }

        public static Currency Euro { get; }

        public static ITry<Currency, string> GetBySymbolOrCode(string symbolOrCode)
        {
            return symbolOrCode.Match(
                Euro.Symbol, _ => Try.Success<Currency, string>(Euro),
                Euro.Code, _ => Try.Success<Currency, string>(Euro),
                _ => Try.Error<Currency, string>("Currency not found.")
            );
        }
    }
}