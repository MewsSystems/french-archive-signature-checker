using Newtonsoft.Json;

namespace Mews.Fiscalization.SignatureChecker.Dto;

internal sealed class ArchiveMetadata
{
    [JsonConstructor]
    public ArchiveMetadata(string terminalIdentification, string previousRecordSignature, DateTime created, string version, string archiveType)
    {
        TerminalIdentification = terminalIdentification;
        PreviousRecordSignature = previousRecordSignature;
        Created = created;
        Version = version;
        ArchiveType = archiveType;
    }

    public string TerminalIdentification { get; }

    public string PreviousRecordSignature { get; }

    public DateTime Created { get; }

    public string Version { get; }

    public string ArchiveType { get; }
}