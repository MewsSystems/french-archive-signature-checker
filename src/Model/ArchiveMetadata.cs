using System;
using System.Collections.Generic;
using FuncSharp;
using Newtonsoft.Json;

namespace Mews.Fiscalization.SignatureChecker.Model
{
    internal sealed class ArchiveMetadata
    {
        private ArchiveMetadata(string terminalIdentification, IOption<string> previousRecordSignature, DateTime created, ArchiveVersion version)
        {
            TerminalIdentification = terminalIdentification;
            PreviousRecordSignature = previousRecordSignature;
            Created = created;
            Version = version;
        }

        public string TerminalIdentification { get; }

        public IOption<string> PreviousRecordSignature { get; }

        public DateTime Created { get; }

        public ArchiveVersion Version { get; }

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
                return version.Map(v => new ArchiveMetadata(
                    terminalIdentification: metadata.TerminalIdentification,
                    previousRecordSignature: metadata.PreviousRecordSignature.ToNonEmptyOption(),
                    created: metadata.Created,
                    version: v
                ));
            });
        }
    }
}