using System.Security.Cryptography;
using System.Text;
using Mews.Fiscalization.SignatureChecker;
using Mews.Fiscalization.SignatureChecker.Model;

var (optionArguments, pathArguments) = args.Partition(a => a.StartsWith("--"));
var archivePath = pathArguments.SingleOption().ToTry(_ => "Invalid arguments".ToReadOnlyList());
var archiveFiles = archivePath.FlatMap(p => ZipFileReader.Read(p));
var result = archiveFiles.FlatMap(files => ValidateArchive(files, optionArguments));

result.Match(
    r => PrintResult(isValid: r),
    errors => PrintResult(isValid: false, errors.MkLines())
);
return;

static void PrintResult(bool isValid, string message = null)
{
    Console.ForegroundColor = ConsoleColor.White;
    if (isValid)
    {
        Console.BackgroundColor = ConsoleColor.Green;
        Console.WriteLine("Archive signature IS valid.");
    }
    else
    {
        Console.BackgroundColor = ConsoleColor.Red;
        Console.WriteLine($"Archive signature IS NOT valid.");
        if (message is not null)
        {
            Console.WriteLine(message);
        }
    }
    Console.ResetColor();
}

static Try<bool, IReadOnlyList<string>> ValidateArchive(IReadOnlyList<Mews.Fiscalization.SignatureChecker.Dto.File> files, IEnumerable<string> optionArguments)
{
    var archive = Archive.Create(files);
    var cryptoServiceProvider = GetCryptoServiceProvider(optionArguments);

    return archive.Map(a => IsArchiveValid(a, cryptoServiceProvider, files));
}

static RSACryptoServiceProvider GetCryptoServiceProvider(IEnumerable<string> optionArguments)
{
    var useDevelopProvider = optionArguments.Contains("--develop", StringComparer.InvariantCultureIgnoreCase);
    return useDevelopProvider ? CryptoServiceProvider.GetDevelop() : CryptoServiceProvider.GetProduction();
}

static bool IsArchiveValid(Archive archive, RSACryptoServiceProvider cryptoServiceProvider, IEnumerable<Mews.Fiscalization.SignatureChecker.Dto.File> files)
{
    var computedSignature = ComputeSignature(archive, files);
    var hashAlgorithmName = archive.Metadata.Version switch
    {
        ArchiveVersion.v100 => HashAlgorithmName.SHA1,
        ArchiveVersion.v400 => HashAlgorithmName.SHA256,
        ArchiveVersion.v410 => HashAlgorithmName.SHA256,
        ArchiveVersion.v411 => HashAlgorithmName.SHA256,
        _ => throw new NotSupportedException($"{archive.Metadata.Version} archive version is not supported.")
    };
    return cryptoServiceProvider.VerifyData(computedSignature, archive.Signature.Value, hashAlgorithmName, RSASignaturePadding.Pkcs1);
}

static byte[] ComputeSignature(Archive archive, IEnumerable<Mews.Fiscalization.SignatureChecker.Dto.File> files)
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
    var signatureProperties = new[]
    {
        taxSummary.ToSignatureString(),
        reportedValue.Value.ToSignatureString(),
        archive.Metadata.Created.ToSignatureString(),
        archive.Metadata.TerminalIdentification,
        archive.Metadata.ArchiveType.ToString().ToUpperInvariant(),
        archiveFilesContentHash.Map(h => Convert.ToBase64String(h)).GetOrNull(),
        previousSignatureFlag,
        archive.Metadata.PreviousRecordSignature.Map(s => s.Base64UrlString).GetOrElse("")
    };
    return Encoding.UTF8.GetBytes(string.Join(",", signatureProperties.Where(p => p != null)));
}