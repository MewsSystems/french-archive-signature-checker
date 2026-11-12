namespace Mews.Fiscalization.SignatureChecker.Dto;

internal sealed class CsvRow(int lineNumber, string[] values)
{
    /// <summary>
    /// The 1-based line number in the source file, so errors can point at the exact line.
    /// </summary>
    public int LineNumber { get; } = lineNumber;

    public IReadOnlyList<string> Values { get; } = values;
}
