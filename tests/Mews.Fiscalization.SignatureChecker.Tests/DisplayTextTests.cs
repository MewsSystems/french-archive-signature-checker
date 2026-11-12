namespace Mews.Fiscalization.SignatureChecker.Tests;

public sealed class DisplayTextTests
{
    [Theory]
    [InlineData("TERMINAL-1")]
    [InlineData("Caisse n°3 — Hôtel €")]
    [InlineData("C:\\Archives\\2025.zip")]
    [InlineData("")]
    public void Escape_LeavesNormalTextUnchanged(string value)
    {
        Assert.Same(value, DisplayText.Escape(value));
    }

    [Fact]
    public void Escape_KeepsNull()
    {
        Assert.Null(DisplayText.Escape(null));
    }

    [Theory]
    [InlineData("a\nb", "a\\u000Ab")]
    [InlineData("a\r\nb", "a\\u000D\\u000Ab")]
    [InlineData("a\tb", "a\\u0009b")]
    [InlineData("\u001b[8mhidden", "\\u001B[8mhidden")]
    [InlineData("\u001b]8;;https://example.com\u0007link", "\\u001B]8;;https://example.com\\u0007link")]
    [InlineData("\u009b31m", "\\u009B31m")]
    [InlineData("abc\u202Edcba", "abc\\u202Edcba")]
    [InlineData("a\u2066b\u2069", "a\\u2066b\\u2069")]
    [InlineData("zero\u200Bwidth", "zero\\u200Bwidth")]
    [InlineData("line\u2028break", "line\\u2028break")]
    [InlineData("para\u2029break", "para\\u2029break")]
    [InlineData("\0", "\\u0000")]
    public void Escape_EscapesControlAndInvisibleFormattingCharacters(string value, string expected)
    {
        Assert.Equal(expected, DisplayText.Escape(value));
    }

    [Fact]
    public void EscapeLines_KeepsSeparateMessagesOnSeparateLines()
    {
        Assert.Equal($"a\\u000Ab{Environment.NewLine}c", DisplayText.EscapeLines(["a\nb", "c"]));
    }
}
