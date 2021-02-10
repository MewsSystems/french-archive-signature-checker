using System;
using System.Collections.Generic;
using System.Linq;
using FuncSharp;

namespace Mews.Fiscalization.SignatureChecker
{
    internal static class ArchiveReader
    {
        public static ITry<Dto.Archive, IEnumerable<string>> ReadArchive(IReadOnlyList<Dto.File> entries)
        {
            var metadata = ReadEntry(entries, "METADATA.json");
            var signature = ReadEntry(entries, "SIGNATURE.txt");
            var taxTotals = ReadOptionalEntry(entries, "TAX_TOTALS").Map(e => GetCsvData(e.Content));
            var totals = ReadOptionalEntry(entries, "TOTALS").Map(e => GetCsvData(e.Content));
            var invoiceFooter = ReadOptionalEntry(entries, "INVOICE_FOOTER").Map(e => GetCsvData(e.Content));

            return Try.Aggregate(
                metadata,
                signature,
                (m, s) => new Dto.Archive(metadata: m, signature: s, totals: totals, taxTotals: taxTotals, invoiceFooter: invoiceFooter)
            );
        }

        private static ITry<Dto.File, IEnumerable<string>> ReadEntry(IReadOnlyList<Dto.File> archiveEntries, string namePrefix)
        {
            return ReadOptionalEntry(archiveEntries, namePrefix).ToTry(_ => $"No unique file found {namePrefix}*.".ToEnumerable());
        }

        private static IOption<Dto.File> ReadOptionalEntry(IReadOnlyList<Dto.File> archiveEntries, string namePrefix, bool isOptional = false)
        {
            return archiveEntries.SingleOption(e => e.Name.StartsWith(namePrefix));
        }

        private static Dto.CsvData GetCsvData(string source)
        {
            var lines = source.Split('\n').Skip(1).Where(l => !String.IsNullOrWhiteSpace(l));
            return new Dto.CsvData(lines.Select(l => new Dto.CsvRow(l.Split(';'))));
        }
    }
}