using System.Collections.Generic;
using FuncSharp;

namespace Mews.Fiscalization.SignatureChecker.Model
{
    internal class Archive
    {
        public Archive(ArchiveMetadata metadata, byte[] signature, TaxSummary taxSummary, ReportedValue reportedValue)
        {
            Metadata = metadata;
            Signature = signature;
            TaxSummary = taxSummary;
            ReportedValue = reportedValue;
        }

        public ArchiveMetadata Metadata { get; }

        public byte[] Signature { get; }

        public TaxSummary TaxSummary { get; }

        public ReportedValue ReportedValue { get; }

        public static ITry<Archive, IEnumerable<string>> Create(Dto.Archive archive)
        {
            return ArchiveMetadata.Create(archive).FlatMap(metadata =>
            {
                return Try.Aggregate(
                    TaxSummary.Create(archive, metadata.Version),
                    ReportedValue.Create(archive, metadata.Version),
                    (taxSummary, reportedValue) => new Archive(metadata, null, taxSummary, reportedValue)
                );
            });
        }
    }
}