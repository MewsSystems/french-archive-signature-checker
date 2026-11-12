using System.IO;
using Mews.Fiscalization.SignatureChecker.Tests.Support;

namespace Mews.Fiscalization.SignatureChecker.Tests;

public sealed class ZipFileReaderTests : IDisposable
{
    private readonly string directory = Directory.CreateTempSubdirectory("signature-checker-tests").FullName;

    [Fact]
    public void Read_ReturnsEntriesInOrderWithUtf8Content()
    {
        var path = TestArchives.WriteZip([new Dto.File("b.csv", "é;1"), new Dto.File("a.json", "{}")], directory);

        var files = ZipFileReader.Read(path).Success.Get();

        Assert.Equal(["b.csv", "a.json"], files.Select(f => f.Name));
        Assert.Equal(["é;1", "{}"], files.Select(f => f.Content));
    }

    [Fact]
    public void Read_UsesFileNameWithoutFolder()
    {
        var path = TestArchives.WriteZip([new Dto.File("folder/METADATA.json", "{}")], directory);

        var file = Assert.Single(ZipFileReader.Read(path).Success.Get());

        Assert.Equal("METADATA.json", file.Name);
    }

    [Fact]
    public void Read_KeepsByteOrderMark()
    {
        var path = Path.Combine(directory, "bom.zip");
        using (var zip = System.IO.Compression.ZipFile.Open(path, System.IO.Compression.ZipArchiveMode.Create))
        using (var stream = zip.CreateEntry("x.csv").Open())
        {
            stream.Write([0xEF, 0xBB, 0xBF, (byte)'a']);
        }

        var file = Assert.Single(ZipFileReader.Read(path).Success.Get());

        Assert.Equal("﻿a", file.Content);
    }

    [Fact]
    public void Read_ReportsMissingFile()
    {
        var path = Path.Combine(directory, "missing.zip");

        Assert.Equal([$"File {path} does not exist."], ZipFileReader.Read(path).Error.Get());
    }

    [Fact]
    public void Read_ReportsNullPath()
    {
        Assert.Equal(["File  does not exist."], ZipFileReader.Read(null).Error.Get());
    }

    [Fact]
    public void Read_ReportsFileThatIsNotAZip()
    {
        var path = Path.Combine(directory, "not-a.zip");
        File.WriteAllText(path, "plain text");

        var error = Assert.Single(ZipFileReader.Read(path).Error.Get());

        Assert.StartsWith("Cannot read archive: ", error);
    }

    public void Dispose()
    {
        Directory.Delete(directory, recursive: true);
    }
}
