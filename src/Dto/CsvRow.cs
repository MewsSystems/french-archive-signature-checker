using System.Collections.Generic;

namespace Mews.Fiscalization.SignatureChecker.Dto;

internal sealed class CsvRow
{
    internal CsvRow(string[] values)
    {
        Values = values;
    }

    public IReadOnlyList<string> Values { get; }
}