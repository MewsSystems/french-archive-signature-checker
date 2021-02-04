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
    internal static class ZipArchiveReader
    {
        public static ITry<IReadOnlyList<ArchiveEntry>, string> ReadArchive(string path)
        {
            var entries = Try.Create<IReadOnlyList<ArchiveEntry>, Exception>(_ =>
            {
                using (var stream = File.OpenRead(path))
                using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    return zip.Entries.Select(e => ReadEntry(e)).ToList();
                }
            });
            return entries.MapError(e => "Cannot read archive.");
        }

        private static ArchiveEntry ReadEntry(ZipArchiveEntry zipEntry)
        {
            using (var stream = zipEntry.Open())
            {
                var content = Encoding.UTF8.GetString(ReadFully(stream));
                return new ArchiveEntry(zipEntry.Name, content);
            }
        }

        private static byte[] ReadFully(Stream stream, bool seekToBeginning = false)
        {
            if (seekToBeginning)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            var buffer = new byte[32768];
            using (var ms = new MemoryStream())
            {
                while (true)
                {
                    var read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                    {
                        return ms.ToArray();
                    }
                    ms.Write(buffer, 0, read);
                }
            }
        }
    }
}