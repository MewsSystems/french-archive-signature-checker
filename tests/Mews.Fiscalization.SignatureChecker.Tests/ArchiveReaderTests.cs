using Mews.Fiscalization.SignatureChecker.Dto;

namespace Mews.Fiscalization.SignatureChecker.Tests;

public sealed class ArchiveReaderTests
{
    private static readonly File Metadata = new("METADATA.json", "{}");
    private static readonly File Signature = new("SIGNATURE.txt", "AQID");

    [Fact]
    public void CompileArchive_ReadsAllKnownFiles()
    {
        var files = new List<File>
        {
            Metadata,
            Signature,
            new("TOTALS-2015.csv", "header\n1;2\n"),
            new("TAX_TOTALS-2015.csv", "header\n3;4\n"),
            new("INVOICE_FOOTER-1.csv", "header\na;b\n"),
            new("INVOICE_FOOTER-2.csv", "header\nc;d\n"),
            new("INVOICE_HEADER.csv", "header\nx\n"),
        };

        var archive = ArchiveReader.CompileArchive(files).Success.Get();

        Assert.Same(Metadata, archive.Metadata);
        Assert.Same(Signature, archive.Signature);
        Assert.Equal(["1", "2"], archive.Totals.Get().Rows.Single().Values);
        Assert.Equal(["3", "4"], archive.TaxTotals.Get().Rows.Single().Values);
        Assert.Equal(2, archive.InvoiceFooters.Count);
        Assert.Equal(["c", "d"], archive.InvoiceFooters[1].Rows.Single().Values);
    }

    [Fact]
    public void CompileArchive_OptionalFilesMayBeMissing()
    {
        var archive = ArchiveReader.CompileArchive([Metadata, Signature]).Success.Get();

        Assert.True(archive.Totals.IsEmpty);
        Assert.True(archive.TaxTotals.IsEmpty);
        Assert.Empty(archive.InvoiceFooters);
    }

    [Fact]
    public void CompileArchive_ReportsAllMissingRequiredFiles()
    {
        var result = ArchiveReader.CompileArchive([]);

        Assert.True(result.IsError);
        Assert.Equal(["No unique file found METADATA.json*.", "No unique file found SIGNATURE.txt*."], result.Error.Get());
    }

    [Fact]
    public void CompileArchive_RejectsDuplicateRequiredFile()
    {
        var result = ArchiveReader.CompileArchive([Metadata, Signature, new("SIGNATURE.txt.bak", "x")]);

        Assert.True(result.IsError);
        Assert.Equal(["No unique file found SIGNATURE.txt*."], result.Error.Get());
    }

    [Fact]
    public void CompileArchive_DuplicateOptionalFileIsTreatedAsMissing()
    {
        var archive = ArchiveReader.CompileArchive([Metadata, Signature, new("TOTALS-1.csv", "h\n1\n"), new("TOTALS-2.csv", "h\n2\n")]).Success.Get();

        Assert.True(archive.Totals.IsEmpty);
    }

    [Fact]
    public void CompileArchive_FileNameMatchingIsCaseSensitivePrefix()
    {
        var result = ArchiveReader.CompileArchive([new("metadata.json", "{}"), new("x-SIGNATURE.txt", "AQID")]);

        Assert.True(result.IsError);
        Assert.Equal(2, result.Error.Get().Count);
    }

    [Fact]
    public void CompileArchive_CsvSkipsHeaderAndBlankLines()
    {
        var archive = ArchiveReader.CompileArchive([Metadata, Signature, new("TOTALS.csv", "header;row\n\na;b\r\n   \nc;;d")]).Success.Get();

        var totals = archive.Totals.Get();
        var rows = totals.Rows;
        Assert.Equal("TOTALS.csv", totals.Name);
        Assert.Equal(2, rows.Count);
        Assert.Equal([3, 5], rows.Select(r => r.LineNumber));
        Assert.Equal(["a", "b\r"], rows[0].Values);
        Assert.Equal(["c", "", "d"], rows[1].Values);
    }
}
