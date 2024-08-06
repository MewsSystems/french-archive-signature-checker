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
            var version = metadata.Version switch
            {
                "1.0" => Try.Success<ArchiveVersion, IReadOnlyList<string>>(ArchiveVersion.v100),
                "4.0" => Try.Success<ArchiveVersion, IReadOnlyList<string>>(ArchiveVersion.v400),
                "4.1" => Try.Success<ArchiveVersion, IReadOnlyList<string>>(ArchiveVersion.v410),
                "4.1.1" => Try.Success<ArchiveVersion, IReadOnlyList<string>>(ArchiveVersion.v411),
                _ => Try.Error<ArchiveVersion, IReadOnlyList<string>>($"Archive version {metadata.Version} is not supported.".ToReadOnlyList())
            };
            var archiveType = version.FlatMap(v => v switch
            {
                ArchiveVersion.v100 => Try.Success<ArchiveType, IReadOnlyList<string>>(ArchiveType.Archiving),
                ArchiveVersion.v400 => ParseVersion4ArchiveType(metadata),
                ArchiveVersion.v410 => ParseVersion4ArchiveType(metadata),
                ArchiveVersion.v411 => ParseVersion4ArchiveType(metadata),
                _ => Try.Error<ArchiveType, IReadOnlyList<string>>($"Archive type {v} is not supported.".ToReadOnlyList())
            });
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
        return archiveMetadata.ArchiveType switch
        {
            "DAY" => Try.Success<ArchiveType, IReadOnlyList<string>>(ArchiveType.Day),
            "MONTH" => Try.Success<ArchiveType, IReadOnlyList<string>>(ArchiveType.Month),
            "FISCALYEAR" => Try.Success<ArchiveType, IReadOnlyList<string>>(ArchiveType.FiscalYear),
            _ => Try.Error<ArchiveType, IReadOnlyList<string>>($"Archive type {archiveMetadata.ArchiveType} is not supported.".ToReadOnlyList())
        };
    }
}