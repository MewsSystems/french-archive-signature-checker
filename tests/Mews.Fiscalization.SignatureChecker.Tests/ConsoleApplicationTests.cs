using System.IO;
using System.Security.Cryptography;
using Mews.Fiscalization.SignatureChecker.Tests.Support;

namespace Mews.Fiscalization.SignatureChecker.Tests;

public sealed class ConsoleApplicationTests : IDisposable
{
    private static readonly string Banner = $"Mews French fiscal archive (NF525) signature checker {ConsoleApplication.ToolVersion}";
    private const string Prompt = ConsoleApplication.InputPrompt;
    private const string Repeat = ConsoleApplication.RepeatPrompt;
    private const string ArchiveDescription = "Archive version 4.1.1, type DAY, created 2024-01-31 23:59:59, terminal TERMINAL-1, chained to previous archive: no.";
    private const string NotValidForProduction = "[Red] Archive signature is not valid. The archive contents do not match its signature for the Mews production key.";
    private const string DevelopHint = "If the archive comes from a Mews test environment, check it with --develop.";

    private readonly string directory = Directory.CreateTempSubdirectory("signature-checker-tests").FullName;

    // Interactive mode.

    [Fact]
    public void Interactive_EmptyInputAsksAgain()
    {
        var path = WriteArchive();

        var (exitCode, output) = Run([], "", "   ", path, "no");

        Assert.Equal(0, exitCode);
        Assert.Equal(
        [
            Banner,
            Prompt,
            "[Red] Invalid input. Please try again.",
            Prompt,
            "[Red] Invalid input. Please try again.",
            Prompt,
            $"Checking {path} with the Mews production key.",
            ArchiveDescription,
            NotValidForProduction,
            DevelopHint,
            Repeat,
        ], output);
    }

    [Fact]
    public void Interactive_EndOfInputExits()
    {
        // Used to loop forever printing "Invalid input" when standard input was closed.
        var (exitCode, output) = Run([]);

        Assert.Equal(0, exitCode);
        Assert.Equal([Banner, Prompt], output);
    }

    [Fact]
    public void Interactive_EndOfInputAfterErrorExits()
    {
        var (exitCode, output) = Run([], "missing.zip");

        Assert.Equal(0, exitCode);
        Assert.Equal([Banner, Prompt, "[Red] File missing.zip does not exist.", Prompt], output);
    }

    [Fact]
    public void Interactive_EndOfInputAtRepeatQuestionExits()
    {
        var (exitCode, output) = Run([], WriteArchive());

        Assert.Equal(0, exitCode);
        Assert.Equal(Repeat, output[^1]);
    }

    [Theory]
    [InlineData("--develop")]
    [InlineData("a.zip b.zip")]
    [InlineData("a.zip --develp")]
    [InlineData("\"a.zip")]
    public void Interactive_InvalidArgumentsAskAgain(string input)
    {
        var (_, output) = Run([], input);

        Assert.Equal(4, output.Count);
        Assert.StartsWith("[Red] ", output[2]);
        Assert.Equal(Prompt, output[3]);
    }

    [Fact]
    public void Interactive_AcceptsQuotedPathWithSpaces()
    {
        var subdirectory = Directory.CreateDirectory(Path.Combine(directory, "My Archives")).FullName;
        var path = WriteArchive(subdirectory);

        var (_, output) = Run([], $"\"{path}\" --develop", "no");

        Assert.Contains($"Checking {path} with the Mews develop key.", output);
    }

    [Fact]
    public void Interactive_AcceptsEscapedSpacesWhenBackslashEscapesAreOn()
    {
        var subdirectory = Directory.CreateDirectory(Path.Combine(directory, "My Archives")).FullName;
        var path = WriteArchive(subdirectory).Replace('\\', '/');
        var console = new FakeConsole(path.Replace(" ", "\\ ") + " ", "no");

        new ConsoleApplication(console, backslashEscapes: true).Run([]);

        Assert.Contains($"Checking {path} with the Mews production key.", console.Lines);
    }

    [Fact]
    public void Interactive_HelpAndVersionAskAgain()
    {
        var (_, output) = Run([], "--help", "--version");

        Assert.Equal([Banner, Prompt, ConsoleApplication.Usage, Prompt, ConsoleApplication.ToolVersion, Prompt], output);
    }

    [Fact]
    public void Interactive_MissingFileAsksAgainWithoutRepeatQuestion()
    {
        var missing = Path.Combine(directory, "missing.zip");

        var (_, output) = Run([], missing, WriteArchive(), "no");

        Assert.Equal([Banner, Prompt, $"[Red] File {missing} does not exist.", Prompt], output.Take(4));
        Assert.Equal(Repeat, output[^1]);
    }

    [Theory]
    [InlineData("yes")]
    [InlineData(" YES ")]
    [InlineData("Yes")]
    public void Interactive_YesVerifiesAnotherFile(string answer)
    {
        var path = WriteArchive();

        var (_, output) = Run([], path, answer, path, "no");

        Assert.Equal(2, output.Count(l => l == Repeat));
    }

    [Theory]
    [InlineData("no")]
    [InlineData("y")]
    [InlineData("")]
    public void Interactive_AnythingButYesExits(string answer)
    {
        var (exitCode, output) = Run([], WriteArchive(), answer);

        Assert.Equal(0, exitCode);
        Assert.Single(output, l => l == Repeat);
    }

    // Single run with arguments.

    [Fact]
    public void Arguments_NotValidSignatureExitsWithOne()
    {
        var path = WriteArchive();

        var (exitCode, output) = Run([path]);

        Assert.Equal(ExitCodes.NotValid, exitCode);
        Assert.Equal([$"Checking {path} with the Mews production key.", ArchiveDescription, NotValidForProduction, DevelopHint], output);
    }

    [Fact]
    public void Arguments_DevelopKeyHintPointsToProduction()
    {
        var path = WriteArchive();

        var (_, output) = Run([path, "--develop"]);

        Assert.Equal(
        [
            $"Checking {path} with the Mews develop key.",
            ArchiveDescription,
            "[Red] Archive signature is not valid. The archive contents do not match its signature for the Mews develop key.",
            "If the archive comes from Mews production, check it without --develop.",
        ], output);
    }

    [Fact]
    public void Arguments_ValidSignatureExitsWithZeroAndPrintsGreen()
    {
        var path = WriteArchive();
        var console = new FakeConsole();
        SigningEnvironment? receivedEnvironment = null;

        var exitCode = new ConsoleApplication(console, (files, environment) =>
        {
            receivedEnvironment = environment;
            var archive = Model.Archive.Create(files).Success.Get();
            return Try.Success<VerificationResult, IReadOnlyList<string>>(new VerificationResult(archive, IsValid: true));
        }).Run([path, "--develop"]);

        Assert.Equal(ExitCodes.Valid, exitCode);
        Assert.Equal(SigningEnvironment.Develop, receivedEnvironment);
        Assert.Equal([$"Checking {path} with the Mews develop key.", ArchiveDescription, "[Green] Archive signature is valid."], console.Lines);
        Assert.Equal(["colors White/Green", "line Archive signature is valid.", "reset"], console.Log.TakeLast(3));
    }

    [Fact]
    public void Arguments_UnreadableArchiveExitsWithOne()
    {
        var path = TestArchives.WriteZip([new Dto.File("x.csv", "")], directory);

        var (exitCode, output) = Run([path]);

        Assert.Equal(ExitCodes.NotValid, exitCode);
        Assert.Equal(
        [
            $"Checking {path} with the Mews production key.",
            $"[Red] Archive signature is not valid. No unique file found METADATA.json*.{Environment.NewLine}No unique file found SIGNATURE.txt*.",
        ], output);
    }

    [Fact]
    public void Arguments_UnexpectedErrorIsReportedAsNotValid()
    {
        var console = new FakeConsole();

        var exitCode = new ConsoleApplication(console, (_, _) => throw new InvalidOperationException("Boom")).Run([WriteArchive()]);

        Assert.Equal(ExitCodes.NotValid, exitCode);
        Assert.Equal("[Red] Archive signature is not valid. Unexpected error while verifying the archive: Boom", console.Lines[^1]);
    }

    [Fact]
    public void Arguments_MissingFileExitsWithTwo()
    {
        var (exitCode, output) = Run(["missing.zip"]);

        Assert.Equal(ExitCodes.InvalidUsage, exitCode);
        Assert.Equal(["[Red] File missing.zip does not exist."], output);
    }

    [Fact]
    public void Arguments_InvalidArgumentsExitWithTwo()
    {
        var (exitCode, output) = Run(["a.zip", "--develp"]);

        Assert.Equal(ExitCodes.InvalidUsage, exitCode);
        Assert.Equal(["[Red] Unknown option --develp. Use --develop or --production.", "Run with --help to see the usage."], output);
    }

    [Fact]
    public void Arguments_HelpAndVersion()
    {
        var help = Run(["--help"]);
        var version = Run(["--version"]);

        Assert.Equal(0, help.ExitCode);
        Assert.Equal([ConsoleApplication.Usage], help.Output);
        Assert.Equal(0, version.ExitCode);
        Assert.Equal(["2.1.0"], version.Output);
    }

    [Fact]
    public void Usage_DocumentsOptionsAndExitCodes()
    {
        Assert.Contains("--develop", ConsoleApplication.Usage);
        Assert.Contains("--production", ConsoleApplication.Usage);
        Assert.Contains("Exit codes:", ConsoleApplication.Usage);
    }

    [Fact]
    public void Describe_ShowsPreviousSignatureAndUpperCaseType()
    {
        var files = TestArchives.V4Files("4.0", "FISCALYEAR", previousRecordSignature: "AQID").WithSignature("AQID");
        var archive = Model.Archive.Create(files).Success.Get();

        Assert.Equal("Archive version 4.0, type FISCALYEAR, created 2024-01-31 23:59:59, terminal TERMINAL-1, chained to previous archive: yes.", ConsoleApplication.Describe(archive));
    }

    [Fact]
    public void Describe_Version1()
    {
        var archive = Model.Archive.Create(TestArchives.V1Files().WithSignature("AQID")).Success.Get();

        Assert.StartsWith("Archive version 1.0, type ARCHIVING,", ConsoleApplication.Describe(archive));
    }

    private string WriteArchive(string targetDirectory = null)
    {
        var files = TestArchives.V4Files("4.1.1");
        return TestArchives.WriteZip(files.WithSignature(TestArchives.Sign("payload", HashAlgorithmName.SHA256)), targetDirectory ?? directory);
    }

    private static (int ExitCode, List<string> Output) Run(string[] arguments, params string[] inputs)
    {
        var console = new FakeConsole(inputs);
        var exitCode = new ConsoleApplication(console, backslashEscapes: false).Run(arguments);
        return (exitCode, console.Lines);
    }

    public void Dispose()
    {
        Directory.Delete(directory, recursive: true);
    }

    private sealed class FakeConsole(params string[] inputs) : IConsole
    {
        private readonly Queue<string> inputs = new(inputs);
        private ConsoleColor? background;

        public List<string> Lines { get; } = [];

        public List<string> Log { get; } = [];

        // Returns null once the scripted input runs out, like a closed standard input.
        public string ReadLine() => inputs.Count == 0 ? null : inputs.Dequeue();

        public void WriteLine(string message)
        {
            Log.Add($"line {message}");
            Lines.Add(background switch
            {
                ConsoleColor.Red => $"[Red] {message}",
                ConsoleColor.Green => $"[Green] {message}",
                _ => message
            });
        }

        public void SetColors(ConsoleColor foreground, ConsoleColor background)
        {
            Log.Add($"colors {foreground}/{background}");
            this.background = background;
        }

        public void ResetColor()
        {
            Log.Add("reset");
            background = null;
        }
    }
}
