using System.Collections.Generic;
using FuncSharp;

namespace Mews.Fiscalization.SignatureChecker.Model;

internal class Archive
{
    public Archive(ArchiveMetadata metadata, Signature signature, TaxSummary taxSummary, ReportedValue reportedValue)
    {
        Metadata = metadata;
        Signature = signature;
        TaxSummary = taxSummary;
        ReportedValue = reportedValue;
    }

    public ArchiveMetadata Metadata { get; }

    public Signature Signature { get; }

    public TaxSummary TaxSummary { get; }

    public ReportedValue ReportedValue { get; }

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