﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            var (optionArguments, pathArguments) = args.Partition(a => a.StartsWith("--"));

            var archivePath = pathArguments.SingleOption().ToTry(_ => "Invalid arguments".ToEnumerable());
            var archiveFiles = archivePath.FlatMap(p => ZipFileReader.Read(p));
            var archive = archiveFiles.FlatMap(files => Archive.Create(files));

            var cryptoServiceProvider = GetCryptoServiceProvider(optionArguments);

            var result = Try.Aggregate(
                archive,
                cryptoServiceProvider,
                (a, rsa) => IsArchiveValid(a, rsa).Match(
                    t => "Archive signature IS valid.",
                    f => "Archive signature IS NOT valid."
                )
            );
            Console.WriteLine(result.Match(r => r, e => e.MkLines()));
        }

        private static ITry<RSACryptoServiceProvider, IEnumerable<string>> GetCryptoServiceProvider(IEnumerable<string> optionArguments)
        {
            var useDevelopKey = optionArguments.Contains("--develop", StringComparer.InvariantCultureIgnoreCase);
            var fileName = useDevelopKey.Match(
                t => "DevelopPublicKey.xml",
                f => "ProductionPublicKey.xml"
            );
            var cryptoServiceProvider = Try.Create<RSACryptoServiceProvider, Exception>(_ =>
            {
                var xmlKey = File.ReadAllText(fileName);
                var rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(xmlKey);
                return rsa;
            });
            return cryptoServiceProvider.MapError(_ => "Missing or invalid key.".ToEnumerable());
        }

        private static bool IsArchiveValid(Archive archive, RSACryptoServiceProvider cryptoServiceProvider)
        {
            var computedSignature = ComputeSignature(archive);
            var hashAlgorithmName = archive.Metadata.Version.Match(
                ArchiveVersion.v100, _ => HashAlgorithmName.SHA1,
                ArchiveVersion.v400, _ => HashAlgorithmName.SHA256
            );
            return cryptoServiceProvider.VerifyData(computedSignature, archive.Signature.Value, hashAlgorithmName, RSASignaturePadding.Pkcs1);
        }

        private static byte[] ComputeSignature(Archive archive)
        {
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
                previousSignatureFlag,
                archive.Metadata.PreviousRecordSignature.Map(s => s.Base64UrlString).GetOrElse("")
            };
            return Encoding.UTF8.GetBytes(String.Join(",", signatureProperties));
        }
    }
}