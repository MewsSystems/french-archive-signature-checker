using System;
using System.Collections.Generic;
using System.Linq;
using FuncSharp;

namespace Mews.Fiscalization.SignatureChecker.Dto
{
    internal static class ArchiveReader
    {
        public static ITry<Archive, IEnumerable<string>> CompileArchive(IReadOnlyList<File> files)
        {
            var metadata = GetFile(files, "METADATA.json");
            var signature = GetFile(files, "SIGNATURE.txt");
            var taxTotals = GetOptionalEntry(files, "TAX_TOTALS").Map(e => GetCsvData(e.Content));
            var totals = GetOptionalEntry(files, "TOTALS").Map(e => GetCsvData(e.Content));
            var invoiceFooters = GetFiles(files, "INVOICE_FOOTER").Select(f => GetCsvData(f.Content));

            return Try.Aggregate(
                metadata,
                signature,
                (m, s) => new Archive(metadata: m, signature: s, totals: totals, taxTotals: taxTotals, invoiceFooters: invoiceFooters)
            );
        }

        private static ITry<File, IEnumerable<string>> GetFile(IReadOnlyList<File> files, string namePrefix)
        {
            return GetOptionalEntry(files, namePrefix).ToTry(_ => $"No unique file found {namePrefix}*.".ToEnumerable());
        }

        private static IEnumerable<File> GetFiles(IEnumerable<File> files, string namePrefix)
        {
            return files.Where(f => f.Name.StartsWith(namePrefix));
        }

        private static IOption<File> GetOptionalEntry(IReadOnlyList<File> files, string namePrefix)
        {
            return files.SingleOption(e => e.Name.StartsWith(namePrefix));
        }

        private static CsvData GetCsvData(string source)
        {
            var lines = source.Split('\n').Skip(1).Where(l => !String.IsNullOrWhiteSpace(l));
            return new CsvData(lines.Select(l => new CsvRow(l.Split(';'))));
        }
    }
}