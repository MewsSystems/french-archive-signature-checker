using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FuncSharp;

namespace Mews.SignatureChecker
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var archive = args.SingleOption().Match(
                a => Archive.Load(a),
                _ => Try.Error<Archive, string>("Invalid arguments")
            );
            var result = archive.FlatMap(a => IsArchiveValid(a)).Match(
                r => r.Match(
                    t => "Archive signature IS valid.",
                    f => "Archive signature IS NOT valid."
                ),
                e => e
            );
            Console.WriteLine(result);
        }

        private static ITry<bool, string> IsArchiveValid(Archive archive)
        {
            return ComputeSignature(archive).Map(computedSignature =>
            {
                var xmlKey = File.ReadAllText("PublicKey.xml");
                var rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(xmlKey);
                return rsa.VerifyData(computedSignature, archive.Signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            });
        }

        private static ITry<byte[], string> ComputeSignature(Archive archive)
        {
            return ArchiveParser.GetTaxSummary(archive).FlatMap(taxSummary => ArchiveParser.GetReportedValue(archive).Map(reportedValue =>
            {
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
            }));
        }
    }
}