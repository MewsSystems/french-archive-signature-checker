using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using FuncSharp;

namespace Mews.SignatureChecker
{
    internal class Archive
    {
        public Archive(IReadOnlyList<ArchiveEntry> entries)
        {
            Entries = entries;
        }

        public IReadOnlyList<ArchiveEntry> Entries { get; }

        public Try<ArchiveEntry, string> GetEntry(string namePrefix)
        {
            return Entries.SingleOption(e => e.Name.StartsWith(namePrefix)).Match(
                e => Try.Success<ArchiveEntry, string>(e),
                _ => Try.Error($"No unique file found {namePrefix}*.")
            );
        }

        public static Try<Archive, string> Load(string path)
        {
             return File.Exists(path).Match(
                t => ReadArchive(path),
                f => Try.Error("File does not exist.")
             );
        }

        private static Try<Archive, string> ReadArchive(string path)
        {
            var archive = Try.Create(_ =>
            {
                using (var stream = File.OpenRead(path))
                using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    var entries = zip.Entries.Select(e => ReadArchiveEntry(e)).ToList();
                    return new Archive(entries);
                }
            });
            return archive.MapError(e => "Invalid archive.");
        }

        private static ArchiveEntry ReadArchiveEntry(ZipArchiveEntry zipEntry)
        {
            using (var stream = zipEntry.Open())
            {
                var content = Encoding.UTF8.GetString(stream.ReadFully());
                return new ArchiveEntry(zipEntry.Name, content);
            }
        }
    }

    internal sealed class ArchiveEntry
    {
        public ArchiveEntry(string name, string content)
        {
            Name = name;
            Content = content;
        }

        public string Name { get; }

        public string Content { get; }
    }
}