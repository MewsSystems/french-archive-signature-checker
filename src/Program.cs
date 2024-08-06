using System.Security.Cryptography;
using System.Text;
using Mews.Fiscalization.SignatureChecker;
using Mews.Fiscalization.SignatureChecker.Model;

while (true)
{
    Console.WriteLine("Enter the archive zip file path and options (e.g. 2025.zip --develop or 2025.zip --production):");

    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
    {
        LogMessage("Invalid input. Please try again.", isError: true);
        continue;
    }
    var arguments = input.Split(' ');
    var (optionArguments, pathArguments) = arguments.Partition(a => a.StartsWith("--"));

    if (!pathArguments.Any() || pathArguments.Count > 1)
    {
        LogMessage("Invalid path argument/s. One path argument is expected.", isError: true);
        continue;
    }

    var archivePath = pathArguments.SingleOption().ToTry(_ => "Invalid arguments".ToReadOnlyList());
    if (archivePath.IsError)
    {
        LogMessage(archivePath.Error.Get().MkLines(), isError: true);
        continue;
    }

    var archiveFiles = archivePath.FlatMap(p => ZipFileReader.Read(p));
    if (archiveFiles.IsError)
    {
        LogMessage(archiveFiles.Error.Get().MkLines(), isError: true);
        continue;
    }

    var result = archiveFiles.FlatMap(files => ValidateArchive(files, optionArguments));

    result.Match(
        r => PrintResult(isValid: r),
        errors => PrintResult(isValid: false, errors.MkLines())
    );

    Console.WriteLine("Do you want to verify another file? (yes/no):");

    var response = Console.ReadLine();
    if (!string.Equals(response?.Trim().ToLower(), "yes", StringComparison.InvariantCultureIgnoreCase))
    {
        break;
    }
}

return;

static void PrintResult(bool isValid, string message = null)
{
    isValid.Match(
        t => LogMessage("Archive signature is valid."),
        f => LogMessage($"Archive signature is not valid. {message}", isError: true)
    );
}

static void LogMessage(string message, bool isError = false)
{
    Console.ForegroundColor = ConsoleColor.White;
    Console.BackgroundColor = isError ? ConsoleColor.Red : ConsoleColor.Green;
    Console.WriteLine(message);
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
        _ => throw new NotImplementedException("Invalid archive version.")
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
        archiveFilesContentHash.GetOrNull(h => Convert.ToBase64String(h)),
        previousSignatureFlag,
        archive.Metadata.PreviousRecordSignature.Map(s => s.Base64UrlString).GetOrElse("")
    };
    return Encoding.UTF8.GetBytes(string.Join(",", signatureProperties.Where(p => p is not null)));
}