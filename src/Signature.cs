using System;

namespace Mews.SignatureChecker
{
    internal sealed class TaxRate : IComparable<TaxRate>
    {
        public TaxRate(decimal value)
        {
            Value = value;
        }

        public decimal Value { get; }

        public string ToSignatureString()
        {
            var rateNormalizationConstant = 100 * 100;
            return ((int)(Value * rateNormalizationConstant)).ToString().PadLeft(4, '0');
        }

        public int CompareTo(TaxRate other)
        {
            return Value.CompareTo(other.Value);
        }
    }
}