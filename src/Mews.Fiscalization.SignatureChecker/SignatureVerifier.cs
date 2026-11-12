using System.Security.Cryptography;
using System.Text;
using Mews.Fiscalization.SignatureChecker.Model;

namespace Mews.Fiscalization.SignatureChecker;

/// <summary>
/// Rebuilds the signed payload of a French fiscal archive (NF525) and checks the attached signature against a Mews public key.
/// </summary>
internal sealed record VerificationResult(Archive Archive, bool IsValid);

internal static class SignatureVerifier
{
    public static Try<VerificationResult, IReadOnlyList<string>> Verify(IReadOnlyList<Dto.File> files, SigningEnvironment environment)
    {
        return Archive.Create(files).Map(archive =>
        {
            using var publicKey = GetPublicKey(environment);
            return new VerificationResult(archive, IsArchiveValid(archive, publicKey, files));
        });
    }

    public static RSACryptoServiceProvider GetPublicKey(SigningEnvironment environment)
    {
        return environment switch
        {
            SigningEnvironment.Production => CryptoServiceProvider.GetProduction(),
            SigningEnvironment.Develop => CryptoServiceProvider.GetDevelop(),
            _ => throw new ArgumentOutOfRangeException(nameof(environment), environment, "Unknown signing environment.")
        };
    }

    public static bool IsArchiveValid(Archive archive, RSA rsa, IEnumerable<Dto.File> files)
    {
        var computedSignature = ComputeSignature(archive, files);
        var hashAlgorithmName = GetHashAlgorithmName(archive.Metadata.Version);
        try
        {
            return rsa.VerifyData(computedSignature, archive.Signature.Value, hashAlgorithmName, RSASignaturePadding.Pkcs1);
        }
        catch (CryptographicException)
        {
            // Windows and Linux return false for a malformed signature (empty or the wrong length), but macOS throws.
            // A signature that cannot be checked is never valid.
            return false;
        }
    }

    public static HashAlgorithmName GetHashAlgorithmName(ArchiveVersion version)
    {
        return version switch
        {
            ArchiveVersion.v100 => HashAlgorithmName.SHA1,
            ArchiveVersion.v400 => HashAlgorithmName.SHA256,
            ArchiveVersion.v410 => HashAlgorithmName.SHA256,
            ArchiveVersion.v411 => HashAlgorithmName.SHA256,
            _ => throw new NotImplementedException("Invalid archive version.")
        };
    }

    public static byte[] ComputeSignature(Archive archive, IEnumerable<Dto.File> files)
    {
        return Encoding.UTF8.GetBytes(ComputeSignaturePayload(archive, files));
    }

    /// <summary>
    /// The comma separated string that Mews signed when the archive was created.
    /// </summary>
    public static string ComputeSignaturePayload(Archive archive, IEnumerable<Dto.File> files)
    {
        var archiveFilesContentHash = archive.Metadata.Version.Match(
            ArchiveVersion.v411, _ =>
            {
                var applicableFiles = files.Where(f => f.Name.Contains(".csv") || f.Name.Contains(".html"));
                var allFilesBytes = applicableFiles.SelectMany(f => Encoding.UTF8.GetBytes(f.Content));
                return SHA256.HashData(allFilesBytes.ToArray()).ToOption();
            },
            _ => Option.Empty<byte[]>()
        );

        var taxSummary = archive.TaxSummary;
        var reportedValue = archive.ReportedValue;
        var previousSignatureFlag = archive.Metadata.PreviousRecordSignature.Match(
            _ => "Y",
            _ => "N"
        );
        string[] signatureProperties =
        [
            taxSummary.ToSignatureString(),
            reportedValue.Value.ToSignatureString(),
            archive.Metadata.Created.ToSignatureString(),
            archive.Metadata.TerminalIdentification,
            archive.Metadata.ArchiveType.ToString().ToUpperInvariant(),
            archiveFilesContentHash.GetOrNull(h => Convert.ToBase64String(h)),
            previousSignatureFlag,
            archive.Metadata.PreviousRecordSignature.Map(s => s.Base64UrlString).GetOrElse("")
        ];
        return string.Join(",", signatureProperties.Where(p => p is not null));
    }
}
