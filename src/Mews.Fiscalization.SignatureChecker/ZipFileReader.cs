using System.IO;
using System.IO.Compression;
using System.Text;

namespace Mews.Fiscalization.SignatureChecker;

/// <summary>
/// Why an archive file could not be read. <see cref="FileNotFound"/> separates "nothing to verify was given"
/// from a file that exists but is not a readable ZIP archive.
/// </summary>
internal sealed record ZipReadError(bool FileNotFound, string Message);

internal static class ZipFileReader
{
    public static Try<IReadOnlyList<Dto.File>, ZipReadError> Read(string path)
    {
        var validPath = path.ToOption().Where(p => File.Exists(p)).ToTry(_ => new ZipReadError(FileNotFound: true, $"File {path} does not exist."));
        return validPath.FlatMap(p =>
        {
            var entries = Try.Catch<IReadOnlyList<Dto.File>, Exception>(_ =>
            {
                using var stream = File.OpenRead(p);
                using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
                return zip.Entries.Select(e => ReadEntry(e)).AsReadOnlyList();
            });
            return entries.MapError(e => new ZipReadError(FileNotFound: false, $"Cannot read archive: {e.Message}."));
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
