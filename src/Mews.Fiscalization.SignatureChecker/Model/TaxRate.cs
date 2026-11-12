namespace Mews.Fiscalization.SignatureChecker.Model;

internal sealed class TaxRate : Product1<decimal>, IComparable<TaxRate>
{
    public TaxRate(decimal value)
        : base(value)
    {
    }

    public decimal Value => ProductValue1;

    public int CompareTo(TaxRate other)
    {
        return Value.CompareTo(other.Value);
    }

    public override string ToString()
    {
        return $"{Value * 100} %";
    }
}