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

        Assert.Equal(new ZipReadError(FileNotFound: true, $"File {path} does not exist."), ZipFileReader.Read(path).Error.Get());
    }

    [Fact]
    public void Read_ReportsNullPath()
    {
        Assert.Equal(new ZipReadError(FileNotFound: true, "File  does not exist."), ZipFileReader.Read(null).Error.Get());
    }

    [Fact]
    public void Read_ReportsFileThatIsNotAZip()
    {
        var path = Path.Combine(directory, "not-a.zip");
        File.WriteAllText(path, "plain text");

        var error = ZipFileReader.Read(path).Error.Get();

        Assert.False(error.FileNotFound);
        Assert.StartsWith("Cannot read archive: ", error.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("bad\0path.zip")]
    public void Read_ReportsUnusablePathAsMissing(string path)
    {
        Assert.Equal(new ZipReadError(FileNotFound: true, $"File {path} does not exist."), ZipFileReader.Read(path).Error.Get());
    }

    [Fact]
    public void Read_ReportsFolderAsMissing()
    {
        Assert.Equal(new ZipReadError(FileNotFound: true, $"File {directory} does not exist."), ZipFileReader.Read(directory).Error.Get());
    }

    [Fact]
    public void Read_ReportsMissingFolderAsMissing()
    {
        var path = Path.Combine(directory, "no-such-folder", "2025.zip");

        Assert.True(ZipFileReader.Read(path).Error.Get().FileNotFound);
    }

    [Fact]
    public void Read_InaccessibleExistingFileIsNotReportedAsMissing()
    {
        Assert.SkipWhen(Environment.IsPrivilegedProcess, "Root and administrators bypass file permissions.");
        var folder = Directory.CreateDirectory(Path.Combine(directory, "locked")).FullName;
        var path = TestArchives.WriteZip(TestArchives.V1Files().WithSignature("AQID"), folder);

        ZipReadError error;
        using (TestArchives.MakeInaccessible(path))
        {
            error = ZipFileReader.Read(path).Error.Get();
        }

        Assert.False(error.FileNotFound);
        Assert.StartsWith("Cannot read archive: ", error.Message);
    }

    [Fact]
    public void Read_ReportsZipWithCorruptEntry()
    {
        var path = TestArchives.WriteCorruptZip(directory);

        var error = ZipFileReader.Read(path).Error.Get();

        Assert.False(error.FileNotFound);
        Assert.StartsWith("Cannot read archive: ", error.Message);
    }

    public void Dispose()
    {
        Directory.Delete(directory, recursive: true);
    }
}
