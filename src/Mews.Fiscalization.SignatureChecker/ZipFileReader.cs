using System.IO;
using System.IO.Compression;
using System.Text;

namespace Mews.Fiscalization.SignatureChecker;

/// <summary>
/// Why an archive file could not be read. <see cref="FileNotFound"/> separates "nothing to verify was given"
/// from a file that exists but cannot be read as a ZIP archive.
/// </summary>
internal sealed record ZipReadError(bool FileNotFound, string Message);

internal static class ZipFileReader
{
    public static Try<IReadOnlyList<Dto.File>, ZipReadError> Read(string path)
    {
        // Open the file instead of checking File.Exists first: File.Exists also returns false when the file exists
        // but cannot be accessed (for example inside a folder the user may not open), which is not a missing file.
        Stream stream;
        try
        {
            if (string.IsNullOrEmpty(path) || Directory.Exists(path))
            {
                return NotFound(path);
            }
            stream = File.OpenRead(path);
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException or PathTooLongException or ArgumentException or NotSupportedException)
        {
            return NotFound(path);
        }
        catch (Exception e)
        {
            return CannotRead(e);
        }

        using (stream)
        {
            var entries = Try.Catch<IReadOnlyList<Dto.File>, Exception>(_ =>
            {
                using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
                return zip.Entries.Select(e => ReadEntry(e)).AsReadOnlyList();
            });
            return entries.MapError(e => CannotReadError(e));
        }
    }

    private static Try<IReadOnlyList<Dto.File>, ZipReadError> NotFound(string path)
    {
        return Try.Error<IReadOnlyList<Dto.File>, ZipReadError>(new ZipReadError(FileNotFound: true, $"File {path} does not exist."));
    }

    private static Try<IReadOnlyList<Dto.File>, ZipReadError> CannotRead(Exception e)
    {
        return Try.Error<IReadOnlyList<Dto.File>, ZipReadError>(CannotReadError(e));
    }

    private static ZipReadError CannotReadError(Exception e)
    {
        return new ZipReadError(FileNotFound: false, $"Cannot read archive: {e.Message}.");
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
