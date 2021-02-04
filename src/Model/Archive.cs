using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using FuncSharp;
using Newtonsoft.Json;

namespace Mews.Fiscalization.SignatureChecker.Model
{
    internal class Archive
    {
        private Archive(IReadOnlyList<ArchiveEntry> entries, ArchiveMetadata metadata, byte[] signature, TaxSummary taxSummary, Amount reportedValue)
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

        public static ITry<Archive, string> Load(string path)
        {
             return File.Exists(path).Match(
                t => ReadArchive(path),
                f => Try.Error<Archive, string>("File does not exist.")
             );
        }

        public ITry<T, string> ProcessEntry<T>(string namePrefix, Func<ArchiveEntry, T> parser)
        {
            return ProcessEntry(Entries, namePrefix, parser);
        }





        private static ITry<ArchiveMetadata, string> GetMetadata(IReadOnlyList<ArchiveEntry> archiveEntries)
        {
            return ProcessEntry(archiveEntries, "METADATA.json", e => JsonConvert.DeserializeObject<ArchiveMetadata>(e.Content)).FlatMap(m =>
            {
                var isVersionSupported = m.Version == "1.0" || m.Version == "4.0";
                return isVersionSupported.Match(
                    t => Try.Success<ArchiveMetadata, string>(m),
                    f => Try.Error<ArchiveMetadata, string>("Archive version is not supported.")
                );
            });
        }

        private static ITry<byte[], string> GetSignature(IReadOnlyList<ArchiveEntry> archiveEntries)
        {
            return ProcessEntry(archiveEntries, "SIGNATURE.txt", e => Base64Url.GetBytes(e.Content));
        }

        private static ITry<T, string> ProcessEntry<T>(IReadOnlyList<ArchiveEntry> archiveEntries, string namePrefix, Func<ArchiveEntry, T> parser)
        {
            var entry = archiveEntries.SingleOption(e => e.Name.StartsWith(namePrefix)).Match(
                e => Try.Success<ArchiveEntry, string>(e),
                _ => Try.Error<ArchiveEntry, string>($"No unique file found {namePrefix}*.")
            );
            return entry.FlatMap(e =>
            {
                var result = Try.Create<T, Exception>(_ => parser(e));
                return result.MapError(_ => $"Invalid data ({e.Name}).");
            });
        }


    }
}