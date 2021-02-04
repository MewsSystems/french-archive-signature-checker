namespace Mews.Fiscalization.SignatureChecker.Dto
{
    internal sealed class TaxSummaryLine
    {
        public TaxSummaryLine(decimal taxRate, decimal taxValue)
        {
            TaxRate = taxRate;
            TaxValue = taxValue;
        }

        public decimal TaxRate { get; }

        public decimal TaxValue { get; }
    }
}