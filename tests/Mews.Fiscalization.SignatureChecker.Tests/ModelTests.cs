using Mews.Fiscalization.SignatureChecker.Model;

namespace Mews.Fiscalization.SignatureChecker.Tests;

public sealed class AmountTests
{
    [Theory]
    [InlineData("€")]
    [InlineData("EUR")]
    public void Create_AcceptsEuro(string currency)
    {
        var result = Amount.Create(12.5m, currency);

        Assert.True(result.IsSuccess);
        Assert.Equal(12.5m, result.Success.Get().Value);
    }

    [Theory]
    [InlineData("eur")]
    [InlineData("USD")]
    [InlineData("")]
    public void Create_RejectsOtherCurrencies(string currency)
    {
        var result = Amount.Create(12.5m, currency);

        Assert.True(result.IsError);
        Assert.Equal(["Currency not found."], result.Error.Get());
    }

    [Fact]
    public void Sum_AddsValues()
    {
        var amounts = new[] { 1.10m, 2.20m, -0.30m }.Select(v => Amount.Create(v, "EUR").Success.Get());

        Assert.Equal(3.00m, Amount.Sum(amounts).Value);
    }

    [Fact]
    public void Sum_OfNothingIsZero()
    {
        Assert.Equal(0m, Amount.Sum([]).Value);
    }

    [Fact]
    public void ToString_UsesInvariantCulture()
    {
        Assert.Equal("1234.56", Amount.Create(1234.56m, "EUR").Success.Get().ToString());
    }
}

public sealed class TaxRateTests
{
    [Fact]
    public void EqualRatesWithDifferentScaleAreEqual()
    {
        var a = new TaxRate(0.2m);
        var b = new TaxRate(0.2000m);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void CompareTo_OrdersByValue()
    {
        Assert.True(new TaxRate(0.2m).CompareTo(new TaxRate(0.1m)) > 0);
        Assert.True(new TaxRate(0.055m).CompareTo(new TaxRate(0.1m)) < 0);
        Assert.Equal(0, new TaxRate(0.1m).CompareTo(new TaxRate(0.10m)));
    }

    [Fact]
    public void ToString_ShowsPercentage()
    {
        Assert.Equal($"{0.2m * 100} %", new TaxRate(0.2m).ToString());
    }
}

public sealed class SignatureTests
{
    [Theory]
    [InlineData("AQIDBA", new byte[] { 1, 2, 3, 4 })]
    [InlineData("AQID", new byte[] { 1, 2, 3 })]
    [InlineData("AQI", new byte[] { 1, 2 })]
    [InlineData("-_8", new byte[] { 0xfb, 0xff })]
    [InlineData("", new byte[0])]
    public void Create_DecodesBase64Url(string input, byte[] expected)
    {
        var result = Signature.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Success.Get().Value);
        Assert.Equal(input, result.Success.Get().Base64UrlString);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("not base64!")]
    public void Create_RejectsInvalidInput(string input)
    {
        var result = Signature.Create(input);

        Assert.True(result.IsError);
        Assert.StartsWith("Failed to read signature: ", Assert.Single(result.Error.Get()));
    }

    [Fact]
    public void Create_RejectsNull()
    {
        var result = Signature.Create(null);

        Assert.True(result.IsError);
    }
}
