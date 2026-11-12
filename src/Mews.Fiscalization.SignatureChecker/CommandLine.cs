using System.Text;

namespace Mews.Fiscalization.SignatureChecker;

internal enum SigningEnvironment
{
    Production,
    Develop
}

internal abstract record Command;

internal sealed record VerifyCommand(string Path, SigningEnvironment Environment) : Command;

internal sealed record HelpCommand : Command;

internal sealed record VersionCommand : Command;

/// <summary>
/// Parses the archive path and options, given either as process arguments or as a line typed in interactive mode.
/// </summary>
internal static class CommandLine
{
    internal const string DevelopOption = "--develop";
    internal const string ProductionOption = "--production";

    private static readonly string[] HelpOptions = ["--help", "-h", "-?", "/?"];
    private const string VersionOption = "--version";

    public static Try<Command, IReadOnlyList<string>> Parse(IReadOnlyList<string> arguments)
    {
        if (arguments.Any(a => HelpOptions.Contains(a, StringComparer.OrdinalIgnoreCase)))
        {
            return Success(new HelpCommand());
        }
        if (arguments.Any(a => string.Equals(a, VersionOption, StringComparison.OrdinalIgnoreCase)))
        {
            return Success(new VersionCommand());
        }

        var (options, paths) = arguments.Where(a => a.Length > 0).Partition(a => a.StartsWith("--"));
        var errors = new List<string>();

        var unknownOptions = options.Where(o => !IsOption(o, DevelopOption) && !IsOption(o, ProductionOption));
        errors.AddRange(unknownOptions.Select(o => $"Unknown option {o}. Use {DevelopOption} or {ProductionOption}."));

        var develop = options.Any(o => IsOption(o, DevelopOption));
        var production = options.Any(o => IsOption(o, ProductionOption));
        if (develop && production)
        {
            errors.Add($"Use either {DevelopOption} or {ProductionOption}, not both.");
        }

        if (paths.Count != 1)
        {
            errors.Add("Exactly one archive path is expected. If the path contains spaces, put it in quotes.");
        }

        if (errors.Count > 0)
        {
            return Try.Error<Command, IReadOnlyList<string>>(errors);
        }

        var environment = develop ? SigningEnvironment.Develop : SigningEnvironment.Production;
        return Success(new VerifyCommand(paths.Single(), environment));
    }

    /// <summary>
    /// Splits a typed line into arguments. Whitespace separates arguments; single or double quotes group text with spaces.
    /// When <paramref name="backslashEscapes"/> is set (macOS and Linux shells), a backslash escapes the next character,
    /// which is how a file dragged into a terminal arrives (<c>My\ Archives/2025.zip</c>). On Windows a backslash is a path separator.
    /// </summary>
    public static Try<IReadOnlyList<string>, IReadOnlyList<string>> Tokenize(string line, bool backslashEscapes)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        var inToken = false;
        char? quote = null;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (quote is not null)
            {
                if (c == quote)
                {
                    quote = null;
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c is '"' or '\'')
            {
                quote = c;
                inToken = true;
            }
            else if (backslashEscapes && c == '\\' && i + 1 < line.Length)
            {
                current.Append(line[++i]);
                inToken = true;
            }
            else if (char.IsWhiteSpace(c))
            {
                if (inToken)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                    inToken = false;
                }
            }
            else
            {
                current.Append(c);
                inToken = true;
            }
        }

        if (quote is not null)
        {
            return Try.Error<IReadOnlyList<string>, IReadOnlyList<string>>($"Missing closing quote ({quote}).".ToReadOnlyList());
        }
        if (inToken)
        {
            tokens.Add(current.ToString());
        }
        return Try.Success<IReadOnlyList<string>, IReadOnlyList<string>>(tokens);
    }

    private static bool IsOption(string value, string option)
    {
        return string.Equals(value, option, StringComparison.InvariantCultureIgnoreCase);
    }

    private static Try<Command, IReadOnlyList<string>> Success(Command command)
    {
        return Try.Success<Command, IReadOnlyList<string>>(command);
    }
}
