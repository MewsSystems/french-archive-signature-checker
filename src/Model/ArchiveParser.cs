using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FuncSharp;
using Newtonsoft.Json;

namespace Mews.Fiscalization.SignatureChecker.Model
{
    internal static class ArchiveParser
    {
        private static readonly CultureInfo FrenchCulture = new CultureInfo("fr-FR");

        public static ITry<Archive, IEnumerable<string>> ParseArchive(Dto.Archive archive)
        {
            var rawMetadata = archive.Metadata.Parse(e => JsonConvert.DeserializeObject<Dto.ArchiveMetadata>(e.Content));
            return rawMetadata.FlatMap(m => ParseMetadata(m)).FlatMap(metadata =>
            {
                var reportedValue = ParseReportedValue(archive, metadata);
            });
        }

        public static ITry<ArchiveMetadata, IEnumerable<string>> ParseMetadata(Dto.ArchiveMetadata metadata)
        {
            var version = metadata.Version.Match(
                "1.0", _ => Try.Success<ArchiveVersion, IEnumerable<string>>(ArchiveVersion.v100),
                "1.2", _ => Try.Success<ArchiveVersion, IEnumerable<string>>(ArchiveVersion.v120),
                "4.0", _ => Try.Success<ArchiveVersion, IEnumerable<string>>(ArchiveVersion.v400),
                _ => Try.Error<ArchiveVersion, IEnumerable<string>>("Archive version is not supported.".ToEnumerable())
            );
            return version.Map(v => new ArchiveMetadata(
                terminalIdentification: metadata.TerminalIdentification,
                previousRecordSignature: metadata.PreviousRecordSignature.ToNonEmptyOption(),
                created: metadata.Created,
                version: v
            ));
        }

        private static ITry<Amount, IEnumerable<string>> ParseReportedValue(Dto.Archive archive, ArchiveMetadata metadata)
        {
            return metadata.Version.Match(
                ArchiveVersion.v100, _ => GetReportedValuev1(archive),
                ArchiveVersion.v120, _ => GetReportedValuev1(archive),
                ArchiveVersion.v400, _ =>
                {
                    var values = archive.InvoiceFooter.Rows.Select(row => ParseAmount(row.Values[18]));
                    return Try.Aggregate(values).Map(v => Amount.Sum(v));
                }
            );
        }

        private static ITry<Amount, IEnumerable<string>> GetReportedValuev1(Dto.Archive archive)
        {
            var data = archive.Totals.Rows.Select(row => Decimal.Parse(row.Values[3], CultureInfo.InvariantCulture));
            return Amount.Create(data.Sum(), "EUR");
        }

        private static ITry<Amount, string> ParseAmount(string stringValue)
        {
            var tokens = stringValue.Split('\u00A0', ' ').Where(t => !String.IsNullOrWhiteSpace(t)).ToList();
            return tokens.Count.Match(
                2, _ => ParseDecimal(tokens[0]).FlatMap(value => Amount.Create(value: value, currencyCodeOrSymbol: tokens[1].Trim())),
                _ => Try.Error<Amount, string>($"Invalid {nameof(Amount)}.")
            );
        }

        private static ITry<decimal, string> ParseDecimal(string value)
        {
            return Try.Create<decimal, Exception>(_ => Decimal.Parse(value.Replace('.', ',').Trim(), FrenchCulture)).MapError(e => "Invalid number.");
        }
    }
}