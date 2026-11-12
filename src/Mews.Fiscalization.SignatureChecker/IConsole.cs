namespace Mews.Fiscalization.SignatureChecker;

/// <summary>
/// The console operations used by <see cref="ConsoleApplication"/>, so the interactive flow can be tested.
/// </summary>
internal interface IConsole
{
    string ReadLine();

    void WriteLine(string message);

    void SetColors(ConsoleColor foreground, ConsoleColor background);

    void ResetColor();
}

internal sealed class SystemConsole : IConsole
{
    public string ReadLine() => Console.ReadLine();

    public void WriteLine(string message) => Console.WriteLine(message);

    public void SetColors(ConsoleColor foreground, ConsoleColor background)
    {
        Console.ForegroundColor = foreground;
        Console.BackgroundColor = background;
    }

    public void ResetColor() => Console.ResetColor();
}
