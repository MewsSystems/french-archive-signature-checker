using System.Collections.Generic;
using FuncSharp;

namespace Mews.Fiscalization.SignatureChecker.Dto;

internal class CsvData
{
    internal CsvData(IEnumerable<CsvRow> rows)
    {
        Rows = rows.ToReadOnlyList();
    }

    public IReadOnlyList<CsvRow> Rows { get; }


}