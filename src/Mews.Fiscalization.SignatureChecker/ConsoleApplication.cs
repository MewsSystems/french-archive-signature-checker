using System.Globalization;
using System.Reflection;
using Mews.Fiscalization.SignatureChecker.Model;

namespace Mews.Fiscalization.SignatureChecker;

internal static class ExitCodes
{
    public const int Valid = 0;
    public const int NotValid = 1;
    public const int InvalidUsage = 2;
}

/// <summary>
/// Runs the tool. With arguments it verifies one archive and exits with a code that tells the result;
/// without arguments it asks for archive paths interactively.
/// </summary>
internal sealed class ConsoleApplication(
    IConsole console,
    Func<IReadOnlyList<Dto.File>, SigningEnvironment, Try<VerificationResult, IReadOnlyList<string>>> verify = null,
    bool? backslashEscapes = null)
{
    internal const string InputPrompt = "Enter the archive zip file path and options (e.g. 2025.zip --develop or 2025.zip --production), or drag the file into this window:";
    internal const string RepeatPrompt = "Do you want to verify another file? (yes/no):";
    internal const string InvalidInputMessage = "Invalid input. Please try again.";
    internal const string ValidMessage = "Archive signature is valid.";
    internal const string InvalidMessagePrefix = "Archive signature is not valid. ";

    private readonly Func<IReadOnlyList<Dto.File>, SigningEnvironment, Try<VerificationResult, IReadOnlyList<string>>> verify = verify ?? SignatureVerifier.Verify;
    private readonly bool backslashEscapes = backslashEscapes ?? !OperatingSystem.IsWindows();

    internal static string ToolVersion
    {
        get
        {
            var version = typeof(ConsoleApplication).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
            return version.Split('+')[0];
        }
    }

    internal static string Usage =>
        $"""
        Mews French fiscal archive (NF525) signature checker {ToolVersion}

        Usage:
          Mews.Fiscalization.SignatureChecker                            Start interactive mode.
          Mews.Fiscalization.SignatureChecker <archive.zip> [option]     Verify one archive and exit.

        Options:
          {CommandLine.ProductionOption}   Verify with the Mews production key. This is the default.
          {CommandLine.DevelopOption}      Verify with the Mews develop key, for archives from a Mews test environment.
          --help         Show this help.
          --version      Show the tool version.

        Exit codes:
          {ExitCodes.Valid}  The archive signature is valid.
          {ExitCodes.NotValid}  The archive signature is not valid, or the archive could not be read.
          {ExitCodes.InvalidUsage}  The arguments are invalid, or the file does not exist or is not a ZIP file.
        """;

    public int Run(IReadOnlyList<string> arguments)
    {
        return arguments.Count > 0 ? RunOnce(arguments) : RunInteractive();
    }

    private int RunOnce(IReadOnlyList<string> arguments)
    {
        return CommandLine.Parse(arguments).Match(
            command => Execute(command),
            errors =>
            {
                LogMessage(errors.MkLines(), isError: true);
                console.WriteLine("Run with --help to see the usage.");
                return ExitCodes.InvalidUsage;
            }
        );
    }

    private int RunInteractive()
    {
        console.WriteLine($"Mews French fiscal archive (NF525) signature checker {ToolVersion}");
        while (true)
        {
            console.WriteLine(InputPrompt);

            var input = console.ReadLine();
            if (input is null)
            {
                // The input was closed (end of file or Ctrl+Z / Ctrl+D), so there is nothing more to verify.
                return ExitCodes.Valid;
            }
            if (string.IsNullOrWhiteSpace(input))
            {
                LogMessage(InvalidInputMessage, isError: true);
                continue;
            }

            var command = CommandLine.Tokenize(input, backslashEscapes).FlatMap(tokens => CommandLine.Parse(tokens));
            if (command.IsError)
            {
                LogMessage(command.Error.Get().MkLines(), isError: true);
                continue;
            }

            var exitCode = Execute(command.Success.Get());
            if (exitCode == ExitCodes.InvalidUsage || command.Success.Get() is not VerifyCommand)
            {
                // Nothing was verified (wrong path, help or version), so ask for a path again.
                continue;
            }

            console.WriteLine(RepeatPrompt);

            var response = console.ReadLine();
            if (!string.Equals(response?.Trim(), "yes", StringComparison.InvariantCultureIgnoreCase))
            {
                return ExitCodes.Valid;
            }
        }
    }

    private int Execute(Command command)
    {
        switch (command)
        {
            case HelpCommand:
                console.WriteLine(Usage);
                return ExitCodes.Valid;
            case VersionCommand:
                console.WriteLine(ToolVersion);
                return ExitCodes.Valid;
            case VerifyCommand verifyCommand:
                return Verify(verifyCommand);
            default:
                throw new ArgumentOutOfRangeException(nameof(command), command, "Unknown command.");
        }
    }

    private int Verify(VerifyCommand command)
    {
        var archiveFiles = ZipFileReader.Read(command.Path);
        if (archiveFiles.IsError)
        {
            LogMessage(archiveFiles.Error.Get().MkLines(), isError: true);
            return ExitCodes.InvalidUsage;
        }

        var keyName = GetKeyName(command.Environment);
        console.WriteLine($"Checking {command.Path} with the {keyName}.");

        Try<VerificationResult, IReadOnlyList<string>> result;
        try
        {
            result = verify(archiveFiles.Success.Get(), command.Environment);
        }
        catch (Exception e)
        {
            // Never let an unexpected problem end the session or look like a result: report the archive as not verified.
            result = Try.Error<VerificationResult, IReadOnlyList<string>>($"Unexpected error while verifying the archive: {e.Message}".ToReadOnlyList());
        }

        return result.Match(
            r =>
            {
                console.WriteLine(Describe(r.Archive));
                if (r.IsValid)
                {
                    LogMessage(ValidMessage);
                    return ExitCodes.Valid;
                }
                LogMessage($"{InvalidMessagePrefix}The archive contents do not match its signature for the {keyName}.", isError: true);
                console.WriteLine(GetOtherKeyHint(command.Environment));
                return ExitCodes.NotValid;
            },
            errors =>
            {
                LogMessage($"{InvalidMessagePrefix}{errors.MkLines()}", isError: true);
                return ExitCodes.NotValid;
            }
        );
    }

    internal static string Describe(Archive archive)
    {
        var metadata = archive.Metadata;
        var version = metadata.Version switch
        {
            ArchiveVersion.v100 => "1.0",
            ArchiveVersion.v400 => "4.0",
            ArchiveVersion.v410 => "4.1",
            ArchiveVersion.v411 => "4.1.1",
            _ => metadata.Version.ToString()
        };
        var created = metadata.Created.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        var previous = metadata.PreviousRecordSignature.Match(_ => "yes", _ => "no");
        return $"Archive version {version}, type {metadata.ArchiveType.ToString().ToUpperInvariant()}, created {created}, terminal {metadata.TerminalIdentification}, chained to previous archive: {previous}.";
    }

    private static string GetKeyName(SigningEnvironment environment)
    {
        return environment switch
        {
            SigningEnvironment.Develop => "Mews develop key",
            _ => "Mews production key"
        };
    }

    private static string GetOtherKeyHint(SigningEnvironment environment)
    {
        return environment switch
        {
            SigningEnvironment.Develop => "If the archive comes from Mews production, check it without --develop.",
            _ => "If the archive comes from a Mews test environment, check it with --develop."
        };
    }

    private void LogMessage(string message, bool isError = false)
    {
        console.SetColors(ConsoleColor.White, isError ? ConsoleColor.Red : ConsoleColor.Green);
        console.WriteLine(message);
        console.ResetColor();
    }
}
