namespace Mews.Fiscalization.SignatureChecker.Tests;

public sealed class CommandLineTests
{
    [Theory]
    [InlineData(new[] { "2025.zip" }, "Production")]
    [InlineData(new[] { "2025.zip", "--production" }, "Production")]
    [InlineData(new[] { "--PRODUCTION", "2025.zip" }, "Production")]
    [InlineData(new[] { "2025.zip", "--develop" }, "Develop")]
    [InlineData(new[] { "2025.zip", "--Develop", "--develop" }, "Develop")]
    public void Parse_ReadsPathAndEnvironment(string[] arguments, string environment)
    {
        var command = Assert.IsType<VerifyCommand>(CommandLine.Parse(arguments).Success.Get());

        Assert.Equal("2025.zip", command.Path);
        Assert.Equal(Enum.Parse<SigningEnvironment>(environment), command.Environment);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("-?")]
    [InlineData("/?")]
    [InlineData("--HELP")]
    public void Parse_RecognizesHelp(string option)
    {
        Assert.IsType<HelpCommand>(CommandLine.Parse(["2025.zip", option, "--unknown"]).Success.Get());
    }

    [Fact]
    public void Parse_RecognizesVersion()
    {
        Assert.IsType<VersionCommand>(CommandLine.Parse(["--version"]).Success.Get());
    }

    [Theory]
    [InlineData("")]
    [InlineData("--develop")]
    [InlineData("a.zip b.zip")]
    public void Parse_RequiresExactlyOnePath(string arguments)
    {
        var tokens = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(["Exactly one archive path is expected. If the path contains spaces, put it in quotes."], CommandLine.Parse(tokens).Error.Get());
    }

    [Fact]
    public void Parse_IgnoresEmptyArguments()
    {
        Assert.IsType<VerifyCommand>(CommandLine.Parse(["", "2025.zip", ""]).Success.Get());
    }

    [Theory]
    [InlineData("--develp")]
    [InlineData("--prod")]
    [InlineData("--dev")]
    public void Parse_RejectsUnknownOptions(string option)
    {
        // A mistyped option used to silently fall back to the production key.
        Assert.Equal([$"Unknown option {option}. Use --develop or --production."], CommandLine.Parse(["2025.zip", option]).Error.Get());
    }

    [Fact]
    public void Parse_RejectsBothEnvironments()
    {
        Assert.Equal(["Use either --develop or --production, not both."], CommandLine.Parse(["2025.zip", "--develop", "--production"]).Error.Get());
    }

    [Fact]
    public void Parse_ReportsAllErrors()
    {
        Assert.Equal(3, CommandLine.Parse(["--x", "--develop", "--production"]).Error.Get().Count);
    }

    [Theory]
    [InlineData("2025.zip", new[] { "2025.zip" })]
    [InlineData("  2025.zip   --develop  ", new[] { "2025.zip", "--develop" })]
    [InlineData("2025.zip\t--develop", new[] { "2025.zip", "--develop" })]
    [InlineData("\"C:\\My Archives\\2025.zip\" --develop", new[] { "C:\\My Archives\\2025.zip", "--develop" })]
    [InlineData("'/tmp/My Archives/2025.zip'", new[] { "/tmp/My Archives/2025.zip" })]
    [InlineData("a\"b c\"d", new[] { "ab cd" })]
    [InlineData("\"\"", new[] { "" })]
    [InlineData("\"it's.zip\"", new[] { "it's.zip" })]
    public void Tokenize_SplitsOnWhitespaceAndHonoursQuotes(string line, string[] expected)
    {
        Assert.Equal(expected, CommandLine.Tokenize(line, backslashEscapes: false).Success.Get());
    }

    [Fact]
    public void Tokenize_KeepsWindowsBackslashes()
    {
        Assert.Equal(["C:\\Archives\\2025.zip"], CommandLine.Tokenize("C:\\Archives\\2025.zip", backslashEscapes: false).Success.Get());
    }

    [Theory]
    [InlineData("/Users/me/My\\ Archives/2025\\ \\(1\\).zip ", "/Users/me/My Archives/2025 (1).zip")]
    [InlineData("/Users/me/it\\'s.zip", "/Users/me/it's.zip")]
    [InlineData("trailing\\", "trailing\\")]
    public void Tokenize_UnescapesDraggedMacPaths(string line, string expected)
    {
        Assert.Equal([expected], CommandLine.Tokenize(line, backslashEscapes: true).Success.Get());
    }

    [Fact]
    public void Tokenize_ReportsMissingClosingQuote()
    {
        Assert.Equal(["Missing closing quote (\")."], CommandLine.Tokenize("\"2025.zip --develop", backslashEscapes: false).Error.Get());
    }
}
