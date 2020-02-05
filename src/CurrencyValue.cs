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
    }
}