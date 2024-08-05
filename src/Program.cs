using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FuncSharp;
using Mews.Fiscalization.SignatureChecker.Model;

namespace Mews.Fiscalization.SignatureChecker;

internal class Program
{
    public static void Main(string[] args)
    {
        var (optionArguments, pathArguments) = args.Partition(a => a.StartsWith("--"));

        var archivePath = pathArguments.SingleOption().ToTry(_ => "Invalid arguments".ToEnumerable());
        var archiveFiles = archivePath.FlatMap(p => ZipFileReader.Read(p));
        var result = archiveFiles.FlatMap(files =>
        {
            var archive = Archive.Create(files);
            var cryptoServiceProvider = GetCryptoServiceProvider(optionArguments);

            return archive.Map(a =>
            {
                var isArchiveValid = IsArchiveValid(a, cryptoServiceProvider, files);
                return isArchiveValid.Match(
                    t => "Archive signature IS valid.",
                    f => "Archive signature IS NOT valid."
                );
            });
        });

        result.Match(
            r => Console.WriteLine(r),
            errors =>  Console.WriteLine(errors.MkLines())
        );
    }

    private static RSACryptoServiceProvider GetCryptoServiceProvider(IEnumerable<string> optionArguments)
    {
        var useDevelopProvider = optionArguments.Contains("--develop", StringComparer.InvariantCultureIgnoreCase);
        return useDevelopProvider.Match(
            t => CryptoServiceProvider.GetDevelop(),
            f => CryptoServiceProvider.GetProduction()
        );
    }

    private static bool IsArchiveValid(Archive archive, RSACryptoServiceProvider cryptoServiceProvider, IEnumerable<Dto.File> files)
    {
        var computedSignature = ComputeSignature(archive, files);
        var hashAlgorithmName = archive.Metadata.Version.Match(
            ArchiveVersion.v100, _ => HashAlgorithmName.SHA1,
            ArchiveVersion.v400, _ => HashAlgorithmName.SHA256,
            ArchiveVersion.v410, _ => HashAlgorithmName.SHA256,
            ArchiveVersion.v411, _ => HashAlgorithmName.SHA256
        );
        return cryptoServiceProvider.VerifyData(computedSignature, archive.Signature.Value, hashAlgorithmName, RSASignaturePadding.Pkcs1);
    }

    private static byte[] ComputeSignature(Archive archive, IEnumerable<Dto.File> files)
    {
        var archiveFilesContentHash = archive.Metadata.Version.Match(
            ArchiveVersion.v411, _ =>
            {
                var applicableFiles = files.Where(f => f.Name.Contains(".csv") || f.Name.Contains(".html"));
                var allFilesBytes = applicableFiles.SelectMany(f => Encoding.UTF8.GetBytes(f.Content));
                return SHA256.Create().ComputeHash(allFilesBytes.ToArray()).ToOption();
            },
            _ => Option.Empty<byte[]>()
        );

        var taxSummary = archive.TaxSummary;
        var reportedValue = archive.ReportedValue;
        var previousSignatureFlag = archive.Metadata.PreviousRecordSignature.Match(
            _ => "Y",
            _ => "N"
        );
        var signatureProperties = new List<string>
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
        return Encoding.UTF8.GetBytes(String.Join(",", signatureProperties.Where(p => p != null)));
    }
}