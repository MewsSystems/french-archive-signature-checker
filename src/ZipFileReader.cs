using System.IO;
using System.IO.Compression;
using System.Text;

namespace Mews.Fiscalization.SignatureChecker;

internal static class ZipFileReader
{
    public static Try<IReadOnlyList<Dto.File>, IReadOnlyList<string>> Read(string path)
    {
        var validPath = path.ToOption().Where(p => File.Exists(p)).ToTry(_ => $"File {path} does not exist.".ToReadOnlyList());
        return validPath.FlatMap(p =>
        {
            var entries = Try.Catch<IReadOnlyList<Dto.File>, Exception>(_ =>
            {
                using var stream = File.OpenRead(p);
                using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
                return zip.Entries.Select(e => ReadEntry(e)).AsReadOnlyList();
            });
            return entries.MapError(e => $"Cannot read archive: {e.Message}.".ToReadOnlyList());
        });
    }

    private static Dto.File ReadEntry(ZipArchiveEntry zipEntry)
    {
        using var stream = zipEntry.Open();
        var content = Encoding.UTF8.GetString(ReadFully(stream));
        return new Dto.File(zipEntry.Name, content);
    }

    private static byte[] ReadFully(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}