namespace Mews.Fiscalization.SignatureChecker.Model;

internal sealed class TaxRate(decimal value) : Product1<decimal>(value), IComparable<TaxRate>
{
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