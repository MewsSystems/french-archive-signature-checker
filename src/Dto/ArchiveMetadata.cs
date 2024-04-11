using Newtonsoft.Json;

namespace Mews.Fiscalization.SignatureChecker.Dto;

[method: JsonConstructor]
internal sealed record ArchiveMetadata(string TerminalIdentification, string PreviousRecordSignature, DateTime Created, string Version, string ArchiveType);