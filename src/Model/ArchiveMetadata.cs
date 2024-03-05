using System;
using System.Collections.Generic;
using FuncSharp;
using Newtonsoft.Json;

namespace Mews.Fiscalization.SignatureChecker.Model;

internal sealed class ArchiveMetadata
{
    private ArchiveMetadata(string terminalIdentification, Option<Signature> previousRecordSignature, DateTime created, ArchiveVersion version, ArchiveType archiveType)
    {
        TerminalIdentification = terminalIdentification;
        PreviousRecordSignature = previousRecordSignature;
        Created = created;
        Version = version;
        ArchiveType = archiveType;
    }

    public string TerminalIdentification { get; }

    public Option<Signature> PreviousRecordSignature { get; }

    public DateTime Created { get; }

    public ArchiveVersion Version { get; }

    public ArchiveType ArchiveType { get; }

    public static Try<ArchiveMetadata, IReadOnlyList<string>> Create(Dto.Archive archive)
    {
        var rawMetadata = Try.Catch<Dto.ArchiveMetadata, Exception>(_ => JsonConvert.DeserializeObject<Dto.ArchiveMetadata>(archive.Metadata.Content));
        return rawMetadata.MapError(_ => $"Invalid data ({archive.Metadata.Name}).".ToReadOnlyList()).FlatMap(metadata =>
        {
            var version = metadata.Version.Match(
                "1.0", _ => Try.Success<ArchiveVersion, IReadOnlyList<string>>(ArchiveVersion.v100),
                "4.0", _ => Try.Success<ArchiveVersion, IReadOnlyList<string>>(ArchiveVersion.v400),
                "4.1", _ => Try.Success<ArchiveVersion, IReadOnlyList<string>>(ArchiveVersion.v410),
                "4.1.1", _ => Try.Success<ArchiveVersion, IReadOnlyList<string>>(ArchiveVersion.v411),
                e => Try.Error<ArchiveVersion, IReadOnlyList<string>>($"Archive version: ({e}) is not supported.".ToReadOnlyList())
            );
            var archiveType = version.FlatMap(v => v.Match(
                ArchiveVersion.v100, u => Try.Success<ArchiveType, IReadOnlyList<string>>(ArchiveType.Archiving),
                ArchiveVersion.v400, u => ParseVersion4ArchiveType(metadata),
                ArchiveVersion.v410, u => ParseVersion4ArchiveType(metadata),
                ArchiveVersion.v411, u => ParseVersion4ArchiveType(metadata)
            ));
            var previousRecordSignature = metadata.PreviousRecordSignature.ToOption().Match(
                s => Signature.Create(s),
                _ => Try.Success<Signature, IReadOnlyList<string>>(null)
            );
            return Try.Aggregate(
                version,
                previousRecordSignature,
                archiveType,
                (v, s, t) => new ArchiveMetadata(metadata.TerminalIdentification, s.ToOption(), metadata.Created, v, t)
            );
        });
    }

    private static Try<ArchiveType, IReadOnlyList<string>> ParseVersion4ArchiveType(Dto.ArchiveMetadata archiveMetadata)
    {
        return archiveMetadata.ArchiveType.Match(
            "DAY", _ => Try.Success<ArchiveType, IReadOnlyList<string>>(ArchiveType.Day),
            "MONTH", _ => Try.Success<ArchiveType, IReadOnlyList<string>>(ArchiveType.Month),
            "FISCALYEAR", _ => Try.Success<ArchiveType, IReadOnlyList<string>>(ArchiveType.FiscalYear),
            e => Try.Error<ArchiveType, IReadOnlyList<string>>($"({e}) Archive type is not supported.".ToReadOnlyList())
        );
    }
}