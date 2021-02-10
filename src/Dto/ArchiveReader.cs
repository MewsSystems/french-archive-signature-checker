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
            var invoiceFooter = GetOptionalEntry(files, "INVOICE_FOOTER").Map(e => GetCsvData(e.Content));

            return Try.Aggregate(
                metadata,
                signature,
                (m, s) => new Archive(metadata: m, signature: s, totals: totals, taxTotals: taxTotals, invoiceFooter: invoiceFooter)
            );
        }

        private static ITry<File, IEnumerable<string>> GetFile(IReadOnlyList<File> files, string namePrefix)
        {
            return GetOptionalEntry(files, namePrefix).ToTry(_ => $"No unique file found {namePrefix}*.".ToEnumerable());
        }

        private static IOption<File> GetOptionalEntry(IReadOnlyList<File> files, string namePrefix, bool isOptional = false)
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