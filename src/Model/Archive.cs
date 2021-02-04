using System.Collections.Generic;
using Mews.Fiscalization.SignatureChecker.Dto;

namespace Mews.Fiscalization.SignatureChecker.Model
{
    internal class Archive
    {
        public Archive(IReadOnlyList<ArchiveEntry> entries, ArchiveMetadata metadata, byte[] signature, TaxSummary taxSummary, Amount reportedValue)
        {
            Entries = entries;
            Metadata = metadata;
            Signature = signature;
            TaxSummary = taxSummary;
            ReportedValue = reportedValue;
        }

        public IReadOnlyList<ArchiveEntry> Entries { get; }

        public ArchiveMetadata Metadata { get; }

        public byte[] Signature { get; }

        public TaxSummary TaxSummary { get; }

        public Amount ReportedValue { get; }


    }
}