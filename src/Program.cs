using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FuncSharp;
using Newtonsoft.Json;

namespace Mews.SignatureChecker
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var archive = args.SingleOption().Match(
                a => Archive.Load(a),
                _ => Try.Error("Invalid arguments")
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

        private static Try<bool, string> IsArchiveValid(Archive archive)
        {
            return GetSignature(archive).FlatMap(archiveSignature => ComputeSignature(archive).Map(computedSignature =>
            {
                var xmlKey = File.ReadAllText("PublicKey.xml");
                var rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(xmlKey);
                return rsa.VerifyData(computedSignature, archiveSignature, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
            }));
        }

        private static Try<byte[], string> GetSignature(Archive archive)
        {
            return ProcessEntry(archive, "SIGNATURE.txt", e => Base64Url.GetBytes(e.Content));
        }

        private static Try<byte[], string> ComputeSignature(Archive archive)
        {
            return GetMetadata(archive).FlatMap(metadata => GetTaxSummary(archive).FlatMap(taxSummary => GetReportedValue(archive).Map(reportedValue =>
            {
                var operationName = "ARCHIVING";
                var previousSignatureFlag = metadata.PreviousRecordSignature.Match(
                    _ => "Y",
                    _ => "N"
                );
                var signatureProperties = new List<string>
                {
                    taxSummary.ToSignatureString(),
                    reportedValue.ToSignatureString(),
                    metadata.Created.ToSignatureString(),
                    metadata.TerminalIdentification,
                    operationName,
                    previousSignatureFlag,
                    metadata.PreviousRecordSignature.GetOrElse("")
                };
                return Encoding.UTF8.GetBytes(String.Join(",", signatureProperties));
            })));
        }

        private static Try<ArchiveMetadata, string> GetMetadata(Archive archive)
        {
            return ProcessEntry(archive, "METADATA.json", e => JsonConvert.DeserializeObject<ArchiveMetadata>(e.Content)).FlatMap(m =>
            {
                var isVersionSupported = m.Version == "1.0";
                return isVersionSupported.Match(
                    t => Try.Success<ArchiveMetadata, string>(m),
                    f => Try.Error("Archive version is not supported.")
                );
            });
        }

        private static Try<TaxSummary, string> GetTaxSummary(Archive archive)
        {
            return ProcessEntry(archive, "TAX_TOTALS", e =>
            {
                var data = GetCsvData(e.Content, l => new
                {
                    TaxRate = Decimal.Parse(l[4], CultureInfo.InvariantCulture),
                    TaxValue = Decimal.Parse(l[10], CultureInfo.InvariantCulture)
                });
                var lines = data.GroupBy(l => l.TaxRate).ToDictionary(
                    g => new TaxRate(g.Key),
                    g => new CurrencyValue(Currencies.Euro, g.Sum(v => v.TaxValue))
                );
                return new TaxSummary(lines);
            });
        }

        private static Try<CurrencyValue, string> GetReportedValue(Archive archive)
        {
            return ProcessEntry(archive, "TOTALS", e =>
            {
                var data = GetCsvData(e.Content, v => Decimal.Parse(v[3], CultureInfo.InvariantCulture));
                return new CurrencyValue(Currencies.Euro, data.Sum());
            });
        }

        private static Try<T, string> ProcessEntry<T>(Archive archive, string namePrefix, Func<ArchiveEntry, T> parser)
        {
            return archive.GetEntry(namePrefix).FlatMap(e =>
            {
                var result = Try.Create(_ => parser(e));
                return result.MapError(_ => $"Invalid data ({e.Name}).");
            });
        }

        private static IReadOnlyList<T> GetCsvData<T>(string source, Func<string[], T> converter)
        {
            var lines = source.Split('\n').Skip(1);
            return lines.Select(l => converter(l.Split(';'))).ToList();
        }
    }
}