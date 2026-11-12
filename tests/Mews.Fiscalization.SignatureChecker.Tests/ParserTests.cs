using Mews.Fiscalization.SignatureChecker.Model;

namespace Mews.Fiscalization.SignatureChecker.Tests;

public sealed class ParserTests
{
    [Theory]
    [InlineData("0", "0")]
    [InlineData("12", "12")]
    [InlineData("12,50", "12.50")]
    [InlineData("12.50", "12.50")]
    [InlineData("1 234,56", "1234.56")]
    [InlineData("1 234,56", "1234.56")]
    [InlineData("1 234,56", "1234.56")]
    [InlineData(" 7,5 ", "7.5")]
    [InlineData("-3,25", "-3.25")]
    [InlineData("−3,25", "-3.25")]
    [InlineData("0,055", "0.055")]
    public void ParseDecimal_ParsesFrenchAndInvariantFormats(string input, string expected)
    {
        var result = Parser.ParseDecimal(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), result.Success.Get());
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("1,2,3")]
    [InlineData("12 €")]
    public void ParseDecimal_RejectsInvalidNumbers(string input)
    {
        var result = Parser.ParseDecimal(input);

        Assert.True(result.IsError);
        Assert.Equal(["Invalid number."], result.Error.Get());
    }

    [Theory]
    [InlineData("175,00 €", "175.00")]
    [InlineData("175,00€", "175.00")]
    [InlineData("175.00 EUR", "175.00")]
    [InlineData("1 234,56 €", "1234.56")]
    [InlineData("-10,55 €", "-10.55")]
    [InlineData("−10,55 €", "-10.55")]
    [InlineData("0,55 €", "0.55")]
    public void ParseAmount_ParsesEuroAmounts(string input, string expected)
    {
        var result = Parser.ParseAmount(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), result.Success.Get().Value);
    }

    [Theory]
    [InlineData("10,00 USD")]
    [InlineData("10,00 $")]
    [InlineData("10,00")]
    public void ParseAmount_RejectsOtherOrMissingCurrency(string input)
    {
        var result = Parser.ParseAmount(input);

        Assert.True(result.IsError);
        Assert.Equal(["Currency not found."], result.Error.Get());
    }

    [Fact]
    public void ParseAmount_RejectsMissingNumber()
    {
        var result = Parser.ParseAmount("€");

        Assert.True(result.IsError);
        Assert.Equal(["Invalid number."], result.Error.Get());
    }
}
