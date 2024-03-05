using System.Collections.Generic;

namespace Mews.Fiscalization.SignatureChecker.Dto;

internal class CsvRow
{
    internal CsvRow(IReadOnlyList<string> values)
    {
        Values = values;
    }

    public IReadOnlyList<string> Values { get; }
}