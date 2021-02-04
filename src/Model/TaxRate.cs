using System;
using FuncSharp;

namespace Mews.Fiscalization.SignatureChecker.Model
{
    internal sealed class TaxRate : Product1<decimal>, IComparable<TaxRate>
    {
        public TaxRate(decimal value)
            : base(value)
        {
        }

        private decimal Value
        {
            get { return ProductValue1; }
        }

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