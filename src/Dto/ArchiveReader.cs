using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using FuncSharp;
using Mews.Fiscalization.SignatureChecker.Dto;

namespace Mews.Fiscalization.SignatureChecker.Dto
{
    internal static class ArchiveReader
    {
        public static ITry<Archive, string> ReadEntries(IReadOnlyList<ArchiveEntry> entries)
        {
            var metadata = GetMetadata(entries);
            var signature = GetSignature(entries);
            return metadata.FlatMap(m => signature.Map(s => new Archive(entries, m, s)));
        }

        private static IReadOnlyList<T> GetCsvData<T>(string source, Func<string[], T> converter)
        {
            var lines = source.Split('\n').Skip(1).Where(l => !String.IsNullOrWhiteSpace(l));
            return lines.Select(l => converter(l.Split(';'))).ToList();
        }

        private static ITry<ArchiveEntry, string> ReadFile<T>(IReadOnlyList<ArchiveEntry> archiveEntries, string filePrefix)
        {
            return archiveEntries.SingleOption(e => e.Name.StartsWith(filePrefix)).Match(
                e => Try.Success<ArchiveEntry, string>(e),
                _ => Try.Error<ArchiveEntry, string>($"No unique file found {filePrefix}*.")
            );
        }
    }
}