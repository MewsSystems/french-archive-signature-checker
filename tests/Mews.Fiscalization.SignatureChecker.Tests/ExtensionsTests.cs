using Mews.Fiscalization.SignatureChecker.Dto;
using Mews.Fiscalization.SignatureChecker.Model;
using Mews.Fiscalization.SignatureChecker.Tests.Support;

namespace Mews.Fiscalization.SignatureChecker.Tests;

public sealed class ExtensionsTests
{
    [Fact]
    public void ToReadOnlyList_WrapsSingleValue()
    {
        Assert.Equal(["a"], "a".ToReadOnlyList());
    }

    [Fact]
    public void MkLines_JoinsWithNewLine()
    {
        Assert.Equal($"a{Environment.NewLine}b", new[] { "a", "b" }.MkLines());
        Assert.Equal("", Array.Empty<string>().MkLines());
    }

    [Fact]
    public void DateTimeSignatureString_IsCompactTimestamp()
    {
        Assert.Equal("20240131235959", new DateTime(2024, 1, 31, 23, 59, 59, 999).ToSignatureString());
    }

    [Theory]
    [InlineData("175.00", "17500")]
    [InlineData("0.55", "55")]
    [InlineData("0", "0")]
    [InlineData("1.239", "123")] // truncated, not rounded
    [InlineData("-1.239", "-123")]
    public void AmountSignatureString_IsCentsTruncated(string value, string expected)
    {
        var amount = Amount.Create(decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture), "EUR").Success.Get();

        Assert.Equal(expected, amount.ToSignatureString());
    }

    [Fact]
    public void TaxSummarySignatureString_OrdersRatesDescendingAndPadsThem()
    {
        var files = new List<File>
        {
            new("INVOICE_FOOTER.csv", TestArchives.InvoiceFooter(("5,50 %:1,00 €|20,00 %:2,00 €|0,00 %:3,00 €|10,00 %:4,00 €", "", "10,00 €"))),
            new("METADATA.json", TestArchives.Metadata("4.0")),
        }.WithSignature("AQID");
        var archive = Model.Archive.Create(files).Success.Get();

        Assert.Equal("2000:200|1000:400|0550:100|0000:300", archive.TaxSummary.ToSignatureString());
    }
}
