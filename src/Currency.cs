namespace Mews.SignatureChecker
{
    internal sealed class Currency
    {
        public Currency(string code, int normalizationConstant)
        {
            Code = code;
            NormalizationConstant = normalizationConstant;
        }

        public string Code { get; }

        public int NormalizationConstant { get; }
    }

    internal sealed class Currencies
    {
        static Currencies()
        {
            Euro = new Currency(code: "EUR", normalizationConstant: 100);
        }

        public static Currency Euro { get; }
    }
}