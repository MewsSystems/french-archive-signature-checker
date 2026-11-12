using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Mews.Fiscalization.SignatureChecker.Tests.Support;

namespace Mews.Fiscalization.SignatureChecker.Tests;

/// <summary>
/// Runs the real executable, the way an auditor or a script uses it.
/// </summary>
public sealed class ExecutableTests : IDisposable
{
    private readonly string directory = Directory.CreateTempSubdirectory("signature-checker-tests").FullName;

    [Fact]
    public async Task Interactive_VerifiesArchivesUntilUserStops()
    {
        var path = WriteArchive();
        var missing = Path.Combine(directory, "missing.zip");

        var (exitCode, output) = await RunAsync([], "", missing, $"\"{path}\" --develop", "yes", path, "no");

        Assert.Equal(0, exitCode);
        Assert.Equal(
        [
            $"Mews French fiscal archive (NF525) signature checker {ConsoleApplication.ToolVersion}",
            ConsoleApplication.InputPrompt,
            "Invalid input. Please try again.",
            ConsoleApplication.InputPrompt,
            $"File {missing} does not exist.",
            ConsoleApplication.InputPrompt,
            $"Checking {path} with the Mews develop key.",
            "Archive version 4.1.1, type DAY, created 2024-01-31 23:59:59, terminal TERMINAL-1, chained to previous archive: no.",
            "Archive signature is not valid. The archive contents do not match its signature for the Mews develop key.",
            "If the archive comes from Mews production, check it without --develop.",
            ConsoleApplication.RepeatPrompt,
            ConsoleApplication.InputPrompt,
            $"Checking {path} with the Mews production key.",
            "Archive version 4.1.1, type DAY, created 2024-01-31 23:59:59, terminal TERMINAL-1, chained to previous archive: no.",
            "Archive signature is not valid. The archive contents do not match its signature for the Mews production key.",
            "If the archive comes from a Mews test environment, check it with --develop.",
            ConsoleApplication.RepeatPrompt,
        ], output);
    }

    [Fact]
    public async Task Interactive_ExitsWhenInputEndsAfterError()
    {
        var (exitCode, output) = await RunAsync([], Path.Combine(directory, "missing.zip"));

        Assert.Equal(0, exitCode);
        Assert.Equal(ConsoleApplication.InputPrompt, output[^1]);
    }

    [Fact]
    public async Task Arguments_ReturnExitCodes()
    {
        var path = WriteArchive();

        Assert.Equal(ExitCodes.NotValid, (await RunAsync([path, "--develop"])).ExitCode);
        Assert.Equal(ExitCodes.InvalidUsage, (await RunAsync([Path.Combine(directory, "missing.zip")])).ExitCode);
        Assert.Equal(ExitCodes.InvalidUsage, (await RunAsync([path, "--develp"])).ExitCode);
        Assert.Equal(ExitCodes.NotValid, (await RunAsync([TestArchives.WriteCorruptZip(directory)])).ExitCode);
        Assert.Equal(ExitCodes.Valid, (await RunAsync(["--help"])).ExitCode);
    }

    [Fact]
    public async Task Arguments_PathWithSpaces()
    {
        var subdirectory = Directory.CreateDirectory(Path.Combine(directory, "My Archives")).FullName;
        var path = WriteArchive(subdirectory);

        var (exitCode, output) = await RunAsync([path]);

        Assert.Equal(ExitCodes.NotValid, exitCode);
        Assert.Equal($"Checking {path} with the Mews production key.", output[0]);
    }

    [Fact]
    public async Task Arguments_Version()
    {
        var (exitCode, output) = await RunAsync(["--version"]);

        Assert.Equal(0, exitCode);
        Assert.Equal(["2.1.0"], output);
    }

    private string WriteArchive(string targetDirectory = null)
    {
        var files = TestArchives.V4Files("4.1.1");
        return TestArchives.WriteZip(files.WithSignature(TestArchives.Sign("payload", HashAlgorithmName.SHA256)), targetDirectory ?? directory);
    }

    private static async Task<(int ExitCode, string[] Output)> RunAsync(string[] arguments, params string[] inputs)
    {
        var executable = typeof(ConsoleApplication).Assembly.Location;
        var startInfo = new ProcessStartInfo("dotnet", [executable, .. arguments])
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(startInfo)!;
        foreach (var input in inputs)
        {
            await process.StandardInput.WriteLineAsync(input);
        }
        process.StandardInput.Close();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var output = await process.StandardOutput.ReadToEndAsync(timeout.Token);
        await process.WaitForExitAsync(timeout.Token);

        return (process.ExitCode, output.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries));
    }

    public void Dispose()
    {
        Directory.Delete(directory, recursive: true);
    }
}
