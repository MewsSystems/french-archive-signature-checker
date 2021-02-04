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
            var archiveEntries = path.FlatMap(p => ZipArchiveReader.ReadArchive(p));
            var archiveFileContent = archiveEntries.FlatMap(e => ArchiveReader.ReadArchive(e));
            var archive = archiveFileContent.FlatMap(a => ArchiveParser.ParseArchive(a));

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
            return rsa.VerifyData(computedSignature, archive.Signature, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
        }

        private static byte[] ComputeSignature(Archive archive)
        {
            var taxSummary = archive.TaxSummary;
            var reportedValue = archive.ReportedValue;
            var operationName = "ARCHIVING";
            var previousSignatureFlag = archive.Metadata.PreviousRecordSignature.Match(
                _ => "Y",
                _ => "N"
            );
            var signatureProperties = new List<string>
            {
                taxSummary.ToSignatureString(),
                reportedValue.ToSignatureString(),
                archive.Metadata.Created.ToSignatureString(),
                archive.Metadata.TerminalIdentification,
                operationName,
                previousSignatureFlag,
                archive.Metadata.PreviousRecordSignature.GetOrElse("")
            };
            return Encoding.UTF8.GetBytes(String.Join(",", signatureProperties));
        }
    }
}