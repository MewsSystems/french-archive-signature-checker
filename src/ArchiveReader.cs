using System;
using System.Collections.Generic;
using System.Linq;
using FuncSharp;
using Mews.Fiscalization.SignatureChecker.Dto;

namespace Mews.Fiscalization.SignatureChecker
{
    internal static class ArchiveReader
    {
        public static ITry<Archive, IEnumerable<string>> ReadArchive(IReadOnlyList<ArchiveEntry> entries)
        {
            var metadata = ReadEntry(entries, "METADATA.json");
            var signature = ReadEntry(entries, "SIGNATURE.txt");
            var taxTotals = ReadOptionalEntry(entries, "TAX_TOTALS").Map(e => GetCsvData(e.Content));
            var totals = ReadOptionalEntry(entries, "TOTALS").Map(e => GetCsvData(e.Content));
            var invoiceFooter = ReadOptionalEntry(entries, "INVOICE_FOOTER").Map(e => GetCsvData(e.Content));

            return Try.Aggregate(
                metadata,
                signature,
                (m, s) => new Archive(metadata: m, signature: s, totals: totals, taxTotals: taxTotals, invoiceFooter: invoiceFooter)
            );
        }

        private static ITry<ArchiveEntry, IEnumerable<string>> ReadEntry(IReadOnlyList<ArchiveEntry> archiveEntries, string namePrefix)
        {
            return ReadOptionalEntry(archiveEntries, namePrefix).ToTry(_ => $"No unique file found {namePrefix}*.".ToEnumerable());
        }

        private static IOption<ArchiveEntry> ReadOptionalEntry(IReadOnlyList<ArchiveEntry> archiveEntries, string namePrefix, bool isOptional = false)
        {
            return archiveEntries.SingleOption(e => e.Name.StartsWith(namePrefix));
        }

        private static CsvData GetCsvData(string source)
        {
            var lines = source.Split('\n').Skip(1).Where(l => !String.IsNullOrWhiteSpace(l));
            return new CsvData(lines.Select(l => new CsvRow(l.Split(';'))));
        }
    }
}