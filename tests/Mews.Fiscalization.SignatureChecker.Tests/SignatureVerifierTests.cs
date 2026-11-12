using System.Security.Cryptography;
using System.Text;
using Mews.Fiscalization.SignatureChecker.Dto;
using Mews.Fiscalization.SignatureChecker.Model;
using Mews.Fiscalization.SignatureChecker.Tests.Support;

namespace Mews.Fiscalization.SignatureChecker.Tests;

public sealed class SignatureVerifierTests
{
    // Footers in TestArchives.V4Files: 20 % = 100 + 20, 10 % = 50 + 5, 5.5 % = 10 + 0.55; totals 175 + 10.55.
    private const string V4Payload = "2000:12000|1000:5500|0550:1055,18555,20240131235959,TERMINAL-1,DAY,N,";

    // TestArchives.V1Files: 20 % = 20 + 5, 5.5 % = 1.10; totals 100 + 26.10.
    private const string V1Payload = "2000:2500|0550:110,12610,20240131235959,TERMINAL-1,ARCHIVING,N,";

    [Theory]
    [InlineData("4.0")]
    [InlineData("4.1")]
    public void Payload_Version40And41(string version)
    {
        Assert.Equal(V4Payload, Payload(TestArchives.V4Files(version)));
    }

    [Fact]
    public void Payload_Version411IncludesHashOfCsvAndHtmlFiles()
    {
        var files = TestArchives.V4Files("4.1.1");
        files.Add(new File("REPORT.html", "<p>é</p>"));
        files.Add(new File("NOTES.txt", "not hashed"));

        var hashedContent = files[0].Content + files[1].Content + "<p>é</p>";
        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(hashedContent)));

        Assert.Equal($"2000:12000|1000:5500|0550:1055,18555,20240131235959,TERMINAL-1,DAY,{hash},N,", Payload(files));
    }

    [Fact]
    public void Payload_Version411HashDependsOnFileOrder()
    {
        var files = TestArchives.V4Files("4.1.1");
        var reordered = new List<File> { files[1], files[0], files[2] };

        Assert.NotEqual(Payload(files), Payload(reordered));
    }

    [Theory]
    [InlineData("MONTH")]
    [InlineData("FISCALYEAR")]
    public void Payload_UsesUpperCaseArchiveType(string archiveType)
    {
        Assert.Equal(V4Payload.Replace(",DAY,", $",{archiveType},"), Payload(TestArchives.V4Files("4.0", archiveType)));
    }

    [Fact]
    public void Payload_IncludesPreviousRecordSignature()
    {
        Assert.Equal(V4Payload.Replace(",N,", ",Y,AQIDBA"), Payload(TestArchives.V4Files("4.0", previousRecordSignature: "AQIDBA")));
    }

    [Fact]
    public void Payload_Version1()
    {
        Assert.Equal(V1Payload, Payload(TestArchives.V1Files()));
        Assert.Equal(V1Payload.Replace(",N,", ",Y,AQID"), Payload(TestArchives.V1Files(previousRecordSignature: "AQID")));
    }

    [Theory]
    [InlineData(nameof(ArchiveVersion.v100), "SHA1")]
    [InlineData(nameof(ArchiveVersion.v400), "SHA256")]
    [InlineData(nameof(ArchiveVersion.v410), "SHA256")]
    [InlineData(nameof(ArchiveVersion.v411), "SHA256")]
    public void HashAlgorithm_DependsOnVersion(string version, string expected)
    {
        Assert.Equal(new HashAlgorithmName(expected), SignatureVerifier.GetHashAlgorithmName(Enum.Parse<ArchiveVersion>(version)));
    }

    [Fact]
    public void HashAlgorithm_UnknownVersionThrows()
    {
        Assert.Throws<NotImplementedException>(() => SignatureVerifier.GetHashAlgorithmName((ArchiveVersion)42));
    }

    [Theory]
    [InlineData("4.0")]
    [InlineData("4.1")]
    [InlineData("4.1.1")]
    public void IsArchiveValid_AcceptsCorrectlySignedVersion4Archive(string version)
    {
        var files = TestArchives.V4Files(version, previousRecordSignature: "AQIDBA");
        var signed = files.WithSignature(TestArchives.Sign(Payload(files), HashAlgorithmName.SHA256));

        Assert.True(Verify(signed));
    }

    [Fact]
    public void IsArchiveValid_AcceptsCorrectlySignedVersion1ArchiveWithSha1()
    {
        var files = TestArchives.V1Files();
        var signed = files.WithSignature(TestArchives.Sign(V1Payload, HashAlgorithmName.SHA1));

        Assert.True(Verify(signed));
    }

    [Fact]
    public void IsArchiveValid_RejectsWrongHashAlgorithm()
    {
        var files = TestArchives.V1Files();
        var signed = files.WithSignature(TestArchives.Sign(V1Payload, HashAlgorithmName.SHA256));

        Assert.False(Verify(signed));
    }

    [Fact]
    public void IsArchiveValid_RejectsSignatureFromOtherKey()
    {
        using var otherKey = RSA.Create(2048);
        var files = TestArchives.V4Files("4.1.1");
        var signed = files.WithSignature(TestArchives.Sign(Payload(files), HashAlgorithmName.SHA256, otherKey));

        Assert.False(Verify(signed));
    }

    [Fact]
    public void IsArchiveValid_RejectsTamperedAmount()
    {
        var files = TestArchives.V4Files("4.0");
        var signature = TestArchives.Sign(Payload(files), HashAlgorithmName.SHA256);
        files[1] = new File(files[1].Name, files[1].Content.Replace("175,00 €", "175,01 €"));

        Assert.False(Verify(files.WithSignature(signature)));
    }

    [Fact]
    public void IsArchiveValid_RejectsTamperedMetadata()
    {
        var files = TestArchives.V4Files("4.0");
        var signature = TestArchives.Sign(Payload(files), HashAlgorithmName.SHA256);
        files[2] = new File("METADATA.json", TestArchives.Metadata("4.0", created: "2024-01-31T23:59:58"));

        Assert.False(Verify(files.WithSignature(signature)));
    }

    [Fact]
    public void IsArchiveValid_Version411RejectsTamperedFileThatIsNotPartOfTotals()
    {
        var files = TestArchives.V4Files("4.1.1");
        var signature = TestArchives.Sign(Payload(files), HashAlgorithmName.SHA256);
        files[0] = new File(files[0].Name, files[0].Content + "extra;row\n");

        Assert.False(Verify(files.WithSignature(signature)));
    }

    [Fact]
    public void IsArchiveValid_Version41IgnoresChangesInFilesThatAreNotPartOfTotals()
    {
        var files = TestArchives.V4Files("4.1");
        var signature = TestArchives.Sign(Payload(files), HashAlgorithmName.SHA256);
        files[0] = new File(files[0].Name, files[0].Content + "extra;row\n");

        Assert.True(Verify(files.WithSignature(signature)));
    }

    [Fact]
    public void IsArchiveValid_RejectsEmptySignature()
    {
        Assert.False(Verify(TestArchives.V4Files("4.0").WithSignature("")));
    }

    [Fact]
    public void Verify_ReturnsErrorsForInvalidArchive()
    {
        var result = SignatureVerifier.Verify([], SigningEnvironment.Production);

        Assert.Equal(["No unique file found METADATA.json*.", "No unique file found SIGNATURE.txt*."], result.Error.Get());
    }

    [Theory]
    [InlineData(nameof(SigningEnvironment.Production))]
    [InlineData(nameof(SigningEnvironment.Develop))]
    public void Verify_SignatureFromUnknownKeyIsNotValid(string environment)
    {
        var files = TestArchives.V4Files("4.1.1");
        var signed = files.WithSignature(TestArchives.Sign(Payload(files), HashAlgorithmName.SHA256));

        var result = SignatureVerifier.Verify(signed, Enum.Parse<SigningEnvironment>(environment)).Success.Get();

        Assert.False(result.IsValid);
        Assert.Equal(ArchiveVersion.v411, result.Archive.Metadata.Version);
    }

    private static string Payload(List<File> files)
    {
        var archive = Model.Archive.Create(files.WithSignature("AQID")).Success.Get();
        return SignatureVerifier.ComputeSignaturePayload(archive, files);
    }

    private static bool Verify(List<File> signedFiles)
    {
        var archive = Model.Archive.Create(signedFiles).Success.Get();
        using var key = TestArchives.CreateTestKey();
        return SignatureVerifier.IsArchiveValid(archive, key, signedFiles);
    }
}
