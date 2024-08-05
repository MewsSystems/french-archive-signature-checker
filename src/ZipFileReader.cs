using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using FuncSharp;

namespace Mews.Fiscalization.SignatureChecker;

internal static class ZipFileReader
{
    public static ITry<IReadOnlyList<Dto.File>, IEnumerable<string>> Read(string path)
    {
        var validPath = path.ToOption().Where(p => File.Exists(p)).ToTry(_ => "File does not exist.".ToEnumerable());
        return validPath.FlatMap(p =>
        {
            var entries = Try.Create<IReadOnlyList<Dto.File>, Exception>(_ =>
            {
                using (var stream = System.IO.File.OpenRead(p))
                using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    return zip.Entries.Select(e => ReadEntry(e)).ToList();
                }
            });
            return entries.MapError(e => "Cannot read archive.".ToEnumerable());
        });
    }

    private static Dto.File ReadEntry(ZipArchiveEntry zipEntry)
    {
        using (var stream = zipEntry.Open())
        {
            var content = Encoding.UTF8.GetString(ReadFully(stream));
            return new Dto.File(zipEntry.Name, content);
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