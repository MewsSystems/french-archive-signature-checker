namespace Mews.Fiscalization.SignatureChecker.Model;

internal sealed record Archive(ArchiveMetadata Metadata, Signature Signature, TaxSummary TaxSummary, ReportedValue ReportedValue)
{
    public static Try<Archive, IReadOnlyList<string>> Create(IReadOnlyList<Dto.File> files)
    {
        var archive = Dto.ArchiveReader.CompileArchive(files);
        return archive.FlatMap(c => Parse(c));
    }

    private static Try<Archive, IReadOnlyList<string>> Parse(Dto.Archive archive)
    {
        return ArchiveMetadata.Create(archive).FlatMap(metadata =>
        {
            return Try.Aggregate(
                TaxSummary.Create(archive, metadata.Version),
                ReportedValue.Create(archive, metadata.Version),
                Signature.Create(archive.Signature.Content),
                (taxSummary, reportedValue, signature) => new Archive(metadata, signature, taxSummary, reportedValue)
            );
        });
    }
}