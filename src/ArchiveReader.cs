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
            var totals = ReadEntry(entries, "TOTALS").Map(e => GetCsvData(e.Content));
            var taxTotals = ReadEntry(entries, "TAX_TOTALS").Map(e => GetCsvData(e.Content));
            var invoiceFooter = ReadEntry(entries, "INVOICE_FOOTER").Map(e => GetCsvData(e.Content));

            return metadata.FlatMap(m => signature.FlatMap(s => totals.FlatMap(t => taxTotals.FlatMap(tt => invoiceFooter.Map(f => new Archive(
                metadata: m,
                signature: s,
                totals: t,
                taxTotals: tt,
                invoiceFooter: f
            ))))));
        }

        private static ITry<ArchiveEntry, IEnumerable<string>> ReadEntry(IReadOnlyList<ArchiveEntry> archiveEntries, string namePrefix)
        {
            return archiveEntries.SingleOption(e => e.Name.StartsWith(namePrefix)).Match(
                e => Try.Success<ArchiveEntry, IEnumerable<string>>(e),
                _ => Try.Error<ArchiveEntry, IEnumerable<string>>($"No unique file found {namePrefix}*.".ToEnumerable())
            );
        }

        private static CsvData GetCsvData(string source)
        {
            var lines = source.Split('\n').Skip(1).Where(l => !String.IsNullOrWhiteSpace(l));
            return new CsvData(lines.Select(l => new CsvRow(l.Split(';'))));
        }
    }
}