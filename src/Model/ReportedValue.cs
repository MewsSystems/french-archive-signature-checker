namespace Mews.Fiscalization.SignatureChecker.Model;

internal sealed class ReportedValue
{
    private ReportedValue(Amount value)
    {
        Value = value;
    }

    public Amount Value { get; }

    public static Try<ReportedValue, IReadOnlyList<string>> Create(Dto.Archive archive, ArchiveVersion version)
    {
        var reportedValue = version switch
        {
            ArchiveVersion.v100 => GetReportedValueV1(archive),
            ArchiveVersion.v400 => GetReportedValueV4(archive),
            ArchiveVersion.v410 => GetReportedValueV4(archive),
            ArchiveVersion.v411 => GetReportedValueV4(archive),
            _ => Try.Error<Amount, IReadOnlyList<string>>($"Unsupported archive version {version}.".ToReadOnlyList())
        };

        return reportedValue.Map(value => new ReportedValue(value));
    }

    private static Try<Amount, IReadOnlyList<string>> GetReportedValueV1(Dto.Archive archive)
    {
        var totals = archive.Totals.ToTry(_ => "Totals file not found.".ToReadOnlyList());
        var data = totals.FlatMap(t => Try.Aggregate(t.Rows.Select(row => Parser.ParseDecimal(row.Values[3]))));
        return data.FlatMap(values => Amount.Create(values.Sum(), "EUR"));
    }

    private static Try<Amount, IReadOnlyList<string>> GetReportedValueV4(Dto.Archive archive)
    {
        return archive.InvoiceFooters.AsNonEmpty().Match(
            footers =>
            {
                var values = Try.Aggregate(footers.SelectMany(f => f.Rows.Select(row => Parser.ParseAmount(row.Values[18]))));
                return values.Map(v => Amount.Sum(v));
            },
            _ => Try.Error<Amount, IReadOnlyList<string>>("Invoice footer file/s not found.".ToReadOnlyList())
        );
    }
}