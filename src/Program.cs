using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FuncSharp;
using Mews.Fiscalization.SignatureChecker.Model;

namespace Mews.Fiscalization.SignatureChecker
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var path = args.SingleOption().ToTry(_ => "Invalid arguments".ToEnumerable());
            var archiveFiles = path.FlatMap(p => ZipFileReader.Read(p));
            var archive = archiveFiles.FlatMap(files => Archive.Create(files));

            var result = archive.Match(
                a => IsArchiveValid(a).Match(
                    t => "Archive signature IS valid.",
                    f => "Archive signature IS NOT valid."
                ),
                e => e.MkLines()
            );
            Console.WriteLine(result);
        }

        private static bool IsArchiveValid(Archive archive)
        {
            var computedSignature = ComputeSignature(archive);
            var xmlKey = File.ReadAllText("PublicKey.xml");
            var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(xmlKey);

            var hashAlgorithmName = archive.Metadata.Version.Match(
                ArchiveVersion.v100, _ => HashAlgorithmName.SHA1,
                ArchiveVersion.v400, _ => HashAlgorithmName.SHA256
            );
            return rsa.VerifyData(computedSignature, archive.Signature.Value, hashAlgorithmName, RSASignaturePadding.Pkcs1);
        }

        private static byte[] ComputeSignature(Archive archive)
        {
            var taxSummary = archive.TaxSummary;
            var reportedValue = archive.ReportedValue;
            var previousSignatureFlag = archive.Metadata.PreviousRecordSignature.Match(
                _ => "Y",
                _ => "N"
            );
            var archiveType = archive.Metadata.Version.Match(
                ArchiveVersion.v100, _ => "ARCHIVING",
                ArchiveVersion.v400, _ => archive.Metadata.ArchiveType.GetOrElse("")
            );
            var signatureProperties = new List<string>
            {
                taxSummary.ToSignatureString(),
                reportedValue.Value.ToSignatureString(),
                archive.Metadata.Created.ToSignatureString(),
                archive.Metadata.TerminalIdentification,
                archiveType,
                previousSignatureFlag,
                archive.Metadata.PreviousRecordSignature.Map(s => s.Base64UrlString).GetOrElse("")
            };
            return Encoding.UTF8.GetBytes(String.Join(",", signatureProperties));
        }
    }
}