using System.Collections.Generic;
using System.Linq;

namespace Mews.Fiscalization.SignatureChecker.Dto;

internal sealed class CsvData
{
    internal CsvData(IEnumerable<CsvRow> rows)
    {
        Rows = rows.ToList();
    }

    public IReadOnlyList<CsvRow> Rows { get; }
}