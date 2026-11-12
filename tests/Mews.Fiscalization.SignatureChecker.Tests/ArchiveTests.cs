using Mews.Fiscalization.SignatureChecker.Dto;
using Mews.Fiscalization.SignatureChecker.Model;
using Mews.Fiscalization.SignatureChecker.Tests.Support;

namespace Mews.Fiscalization.SignatureChecker.Tests;

public sealed class ArchiveTests
{
    [Theory]
    [InlineData("1.0", nameof(ArchiveVersion.v100))]
    [InlineData("4.0", nameof(ArchiveVersion.v400))]
    [InlineData("4.1", nameof(ArchiveVersion.v410))]
    [InlineData("4.1.1", nameof(ArchiveVersion.v411))]
    public void Create_ParsesSupportedVersions(string version, string expected)
    {
        var files = version == "1.0" ? TestArchives.V1Files() : TestArchives.V4Files(version);

        var archive = Model.Archive.Create(files.WithSignature("AQID")).Success.Get();

        Assert.Equal(Enum.Parse<ArchiveVersion>(expected), archive.Metadata.Version);
        Assert.Equal(TestArchives.Terminal, archive.Metadata.TerminalIdentification);
        Assert.Equal(new DateTime(2024, 1, 31, 23, 59, 59), archive.Metadata.Created);
        Assert.True(archive.Metadata.PreviousRecordSignature.IsEmpty);
        Assert.Equal([1, 2, 3], archive.Signature.Value);
    }

    [Theory]
    [InlineData("2.0")]
    [InlineData("4")]
    [InlineData("")]
    public void Create_RejectsUnsupportedVersion(string version)
    {
        var result = Model.Archive.Create(TestArchives.V4Files(version).WithSignature("AQID"));

        Assert.Equal([$"Archive version {version} is not supported."], result.Error.Get());
    }

    [Fact]
    public void Create_RejectsMissingVersion()
    {
        var files = new List<File> { new("METADATA.json", """{"TerminalIdentification":"T","Created":"2024-01-31T00:00:00","ArchiveType":"DAY"}""") };

        var result = Model.Archive.Create(files.WithSignature("AQID"));

        Assert.Equal(["Archive version  is not supported."], result.Error.Get());
    }

    [Theory]
    [InlineData("DAY", nameof(ArchiveType.Day))]
    [InlineData("MONTH", nameof(ArchiveType.Month))]
    [InlineData("FISCALYEAR", nameof(ArchiveType.FiscalYear))]
    public void Create_ParsesVersion4ArchiveTypes(string archiveType, string expected)
    {
        foreach (var version in new[] { "4.0", "4.1", "4.1.1" })
        {
            var archive = Model.Archive.Create(TestArchives.V4Files(version, archiveType).WithSignature("AQID")).Success.Get();
            Assert.Equal(Enum.Parse<ArchiveType>(expected), archive.Metadata.ArchiveType);
        }
    }

    [Theory]
    [InlineData("day")]
    [InlineData("ARCHIVING")]
    [InlineData("YEAR")]
    public void Create_RejectsUnsupportedVersion4ArchiveType(string archiveType)
    {
        var result = Model.Archive.Create(TestArchives.V4Files("4.1.1", archiveType).WithSignature("AQID"));

        Assert.Equal([$"Archive type {archiveType} is not supported."], result.Error.Get());
    }

    [Fact]
    public void Create_Version1IsAlwaysArchivingType()
    {
        var archive = Model.Archive.Create(TestArchives.V1Files().WithSignature("AQID")).Success.Get();

        Assert.Equal(ArchiveType.Archiving, archive.Metadata.ArchiveType);
    }

    [Fact]
    public void Create_ReadsPreviousRecordSignature()
    {
        var archive = Model.Archive.Create(TestArchives.V4Files("4.1.1", previousRecordSignature: "AQIDBA").WithSignature("AQID")).Success.Get();

        Assert.Equal("AQIDBA", archive.Metadata.PreviousRecordSignature.Get().Base64UrlString);
    }

    [Fact]
    public void Create_RejectsInvalidPreviousRecordSignature()
    {
        var result = Model.Archive.Create(TestArchives.V4Files("4.1.1", previousRecordSignature: "A").WithSignature("AQID"));

        Assert.StartsWith("Failed to read signature: ", Assert.Single(result.Error.Get()));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("""{"Created":"not a date"}""")]
    public void Create_RejectsInvalidMetadata(string metadata)
    {
        var files = new List<File> { new("METADATA.json", metadata) }.WithSignature("AQID");

        var result = Model.Archive.Create(files);

        Assert.Equal(["Invalid data (METADATA.json)."], result.Error.Get());
    }

    [Fact]
    public void Create_RejectsInvalidSignature()
    {
        var result = Model.Archive.Create(TestArchives.V4Files("4.1.1").WithSignature("A"));

        Assert.StartsWith("Failed to read signature: ", Assert.Single(result.Error.Get()));
    }

    [Fact]
    public void Create_ReportsMissingRequiredFiles()
    {
        var result = Model.Archive.Create([]);

        Assert.Equal(["No unique file found METADATA.json*.", "No unique file found SIGNATURE.txt*."], result.Error.Get());
    }

    [Fact]
    public void Create_Version4SumsNetAndTaxPerRateAcrossAllFooters()
    {
        var files = TestArchives.V4Files("4.1.1");
        files.Add(new File("INVOICE_FOOTER-2024-02-01.csv", TestArchives.InvoiceFooter(("20,00 %:1,00 €", "20,00 %:0,20 €", "1,20 €"))));

        var archive = Model.Archive.Create(files.WithSignature("AQID")).Success.Get();

        var summary = archive.TaxSummary.Data.ToDictionary(d => d.Key.Value, d => d.Value.Value);
        Assert.Equal(3, summary.Count);
        Assert.Equal(121.20m, summary[0.2m]);
        Assert.Equal(55.00m, summary[0.1m]);
        Assert.Equal(10.55m, summary[0.055m]);
        Assert.Equal(186.75m, archive.ReportedValue.Value.Value);
    }

    [Fact]
    public void Create_Version4ToleratesEmptyBreakdowns()
    {
        var files = new List<File>
        {
            new("INVOICE_FOOTER.csv", TestArchives.InvoiceFooter(("", "|", "0,00 €"))),
            new("METADATA.json", TestArchives.Metadata("4.0")),
        };

        var archive = Model.Archive.Create(files.WithSignature("AQID")).Success.Get();

        Assert.Empty(archive.TaxSummary.Data);
        Assert.Equal(0m, archive.ReportedValue.Value.Value);
    }

    [Fact]
    public void Create_Version4RequiresInvoiceFooters()
    {
        var files = new List<File> { new("METADATA.json", TestArchives.Metadata("4.0")) };

        var result = Model.Archive.Create(files.WithSignature("AQID"));

        Assert.Equal(["Invoice footer file/s not found."], result.Error.Get());
    }

    [Fact]
    public void Create_Version4ReportsInvalidValues()
    {
        var files = new List<File>
        {
            new("INVOICE_FOOTER.csv", TestArchives.InvoiceFooter(("20,00 %:abc €", "20,00 %:1,00 USD", "1,00 USD"))),
            new("METADATA.json", TestArchives.Metadata("4.0")),
        };

        var result = Model.Archive.Create(files.WithSignature("AQID"));

        Assert.Equal(["Invalid number.", "Currency not found.", "Currency not found."], result.Error.Get());
    }

    [Fact]
    public void Create_Version4ReportsRowsWithTooFewColumns()
    {
        var files = new List<File>
        {
            new("INVOICE_FOOTER-1.csv", "header\na;b;c\n\nd\n"),
            new("INVOICE_FOOTER-2.csv", TestArchives.InvoiceFooter(("20,00 %:1,00 €", "20,00 %:0,20 €", "1,20 €"))),
            new("METADATA.json", TestArchives.Metadata("4.0")),
        };

        var result = Model.Archive.Create(files.WithSignature("AQID"));

        Assert.Equal(
        [
            "Invalid data (INVOICE_FOOTER-1.csv, line 2): expected at least 19 columns, found 3.",
            "Invalid data (INVOICE_FOOTER-1.csv, line 4): expected at least 19 columns, found 1.",
        ], result.Error.Get());
    }

    [Fact]
    public void Create_Version1ReportsRowsWithTooFewColumns()
    {
        var files = new List<File>
        {
            new("TAX_TOTALS.csv", "header\na;b\n"),
            new("TOTALS.csv", "header\na;b;c\n"),
            new("METADATA.json", TestArchives.Metadata("1.0")),
        };

        var result = Model.Archive.Create(files.WithSignature("AQID"));

        Assert.Equal(
        [
            "Invalid data (TAX_TOTALS.csv, line 2): expected at least 11 columns, found 2.",
            "Invalid data (TOTALS.csv, line 2): expected at least 4 columns, found 3.",
        ], result.Error.Get());
    }

    [Theory]
    [InlineData("20,00 %")]
    [InlineData("20,00 %:1,00 €|garbage")]
    public void Create_Version4ReportsTaxBreakdownWithoutAmount(string breakdown)
    {
        var files = new List<File>
        {
            new("INVOICE_FOOTER.csv", TestArchives.InvoiceFooter((breakdown, "", "1,00 €"))),
            new("METADATA.json", TestArchives.Metadata("4.0")),
        };

        var result = Model.Archive.Create(files.WithSignature("AQID"));

        var error = Assert.Single(result.Error.Get());
        Assert.StartsWith("Invalid tax breakdown \"", error);
        Assert.EndsWith("\", expected \"<rate> %:<amount>\".", error);
    }

    [Fact]
    public void Create_Version1SumsTaxTotalsPerRateAndTotals()
    {
        var archive = Model.Archive.Create(TestArchives.V1Files().WithSignature("AQID")).Success.Get();

        var summary = archive.TaxSummary.Data.ToDictionary(d => d.Key.Value, d => d.Value.Value);
        Assert.Equal(2, summary.Count);
        Assert.Equal(25.00m, summary[0.2m]);
        Assert.Equal(1.10m, summary[0.055m]);
        Assert.Equal(126.10m, archive.ReportedValue.Value.Value);
    }

    [Fact]
    public void Create_Version1RequiresTotalsAndTaxTotals()
    {
        var files = new List<File> { new("METADATA.json", TestArchives.Metadata("1.0")) };

        var result = Model.Archive.Create(files.WithSignature("AQID"));

        Assert.Equal(["Tax totals file not found.", "Totals file not found."], result.Error.Get());
    }

    [Fact]
    public void Create_Version1ReportsInvalidNumbers()
    {
        var files = new List<File>
        {
            new("TAX_TOTALS.csv", TestArchives.TaxTotals(("x", "1,00"))),
            new("TOTALS.csv", TestArchives.Totals("y")),
            new("METADATA.json", TestArchives.Metadata("1.0")),
        };

        var result = Model.Archive.Create(files.WithSignature("AQID"));

        Assert.Equal(["Invalid number.", "Invalid number."], result.Error.Get());
    }
}

public sealed class UnknownVersionTests
{
    [Fact]
    public void TaxSummaryAndReportedValue_RejectUnknownVersion()
    {
        var dto = Dto.ArchiveReader.CompileArchive(TestArchives.V4Files("4.0").WithSignature("AQID")).Success.Get();
        var unknown = (ArchiveVersion)42;

        Assert.Throws<ArgumentOutOfRangeException>(() => TaxSummary.Create(dto, unknown));
        Assert.Equal(["Unsupported archive version 42."], ReportedValue.Create(dto, unknown).Error.Get());
        Assert.Equal(["Unsupported archive version 42."], ArchiveStructure.Validate(dto, unknown).Error.Get());
    }
}
