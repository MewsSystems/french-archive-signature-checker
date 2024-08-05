using System;
using System.Collections.Generic;
using FuncSharp;
using Newtonsoft.Json;

namespace Mews.Fiscalization.SignatureChecker.Model;

internal sealed class ArchiveMetadata
{
    private ArchiveMetadata(string terminalIdentification, IOption<Signature> previousRecordSignature, DateTime created, ArchiveVersion version, ArchiveType archiveType)
    {
        TerminalIdentification = terminalIdentification;
        PreviousRecordSignature = previousRecordSignature;
        Created = created;
        Version = version;
        ArchiveType = archiveType;
    }

    public string TerminalIdentification { get; }

    public IOption<Signature> PreviousRecordSignature { get; }

    public DateTime Created { get; }

    public ArchiveVersion Version { get; }

    public ArchiveType ArchiveType { get; }

    public static ITry<ArchiveMetadata, IEnumerable<string>> Create(Dto.Archive archive)
    {
        var rawMetadata = Try.Create<Dto.ArchiveMetadata, Exception>(_ => JsonConvert.DeserializeObject<Dto.ArchiveMetadata>(archive.Metadata.Content));
        return rawMetadata.MapError(_ => $"Invalid data ({archive.Metadata.Name}).".ToEnumerable()).FlatMap(metadata =>
        {
            var version = metadata.Version.Match(
                "1.0", _ => Try.Success<ArchiveVersion, IEnumerable<string>>(ArchiveVersion.v100),
                "4.0", _ => Try.Success<ArchiveVersion, IEnumerable<string>>(ArchiveVersion.v400),
                "4.1", _ => Try.Success<ArchiveVersion, IEnumerable<string>>(ArchiveVersion.v410),
                "4.1.1", _ => Try.Success<ArchiveVersion, IEnumerable<string>>(ArchiveVersion.v411),
                _ => Try.Error<ArchiveVersion, IEnumerable<string>>("Archive version is not supported.".ToEnumerable())
            );
            var archiveType = version.FlatMap(v => v.Match(
                ArchiveVersion.v100, u => Try.Success<ArchiveType, IEnumerable<string>>(ArchiveType.Archiving),
                ArchiveVersion.v400, u => ParseVersion4ArchiveType(metadata),
                ArchiveVersion.v410, u => ParseVersion4ArchiveType(metadata),
                ArchiveVersion.v411, u => ParseVersion4ArchiveType(metadata)
            ));
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

    private static ITry<ArchiveType, IEnumerable<string>> ParseVersion4ArchiveType(Dto.ArchiveMetadata archiveMetadata)
    {
        return archiveMetadata.ArchiveType.Match(
            "DAY", _ => Try.Success<ArchiveType, IEnumerable<string>>(ArchiveType.Day),
            "MONTH", _ => Try.Success<ArchiveType, IEnumerable<string>>(ArchiveType.Month),
            "FISCALYEAR", _ => Try.Success<ArchiveType, IEnumerable<string>>(ArchiveType.FiscalYear),
            _ => Try.Error<ArchiveType, IEnumerable<string>>($"{nameof(Model.ArchiveType)} is not supported.".ToEnumerable())
        );
    }
}