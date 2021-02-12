using System;
using System.Collections.Generic;
using FuncSharp;
using Newtonsoft.Json;

namespace Mews.Fiscalization.SignatureChecker.Model
{
    internal sealed class ArchiveMetadata
    {
        private ArchiveMetadata(string terminalIdentification, IOption<Signature> previousRecordSignature, DateTime created, ArchiveVersion version, string archiveType = null)
        {
            TerminalIdentification = terminalIdentification;
            PreviousRecordSignature = previousRecordSignature;
            Created = created;
            Version = version;
            ArchiveType = archiveType.ToOption();
        }

        public string TerminalIdentification { get; }

        public IOption<Signature> PreviousRecordSignature { get; }

        public DateTime Created { get; }

        public ArchiveVersion Version { get; }

        public IOption<string> ArchiveType { get; }

        public static ITry<ArchiveMetadata, IEnumerable<string>> Create(Dto.Archive archive)
        {
            var rawMetadata = Try.Create<Dto.ArchiveMetadata, Exception>(_ => JsonConvert.DeserializeObject<Dto.ArchiveMetadata>(archive.Metadata.Content));
            return rawMetadata.MapError(_ => $"Invalid data ({archive.Metadata.Name}).".ToEnumerable()).FlatMap(metadata =>
            {
                var version = metadata.Version.Match(
                    "1.0", _ => Try.Success<ArchiveVersion, IEnumerable<string>>(ArchiveVersion.v100),
                    "4.0", _ => Try.Success<ArchiveVersion, IEnumerable<string>>(ArchiveVersion.v400),
                    _ => Try.Error<ArchiveVersion, IEnumerable<string>>("Archive version is not supported.".ToEnumerable())
                );
                var archiveType = metadata.ArchiveType.ToOption().Match(
                    type => version.FlatMap(v => v.Match(
                        ArchiveVersion.v100, _ => type.Equals("ARCHIVING").ToTry(t => type, f => $"Archive version {v} must have archive type: ARCHIVING".ToEnumerable()),
                        ArchiveVersion.v400, _ => Try.Success<string, IEnumerable<string>>(type)
                    )),
                    _ => Try.Error<string, IEnumerable<string>>($"{metadata.ArchiveType} is missing.".ToEnumerable())
                );
                var previousRecordSignature = metadata.PreviousRecordSignature.ToOption().Match(
                    s => Signature.Create(s),
                    _ => Try.Success<Signature, IEnumerable<string>>(null)
                );
                return Try.Aggregate(
                    version,
                    previousRecordSignature,
                    archiveType,
                    (v, s, t) => new ArchiveMetadata(metadata.TerminalIdentification, s.ToOption(), metadata.Created, v, t)
                );
            });
        }
    }
}