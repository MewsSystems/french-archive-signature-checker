using System;
using FuncSharp;

namespace Mews.Fiscalization.SignatureChecker.Model
{
    internal sealed class ArchiveMetadata
    {
        public ArchiveMetadata(string terminalIdentification, IOption<string> previousRecordSignature, DateTime created, ArchiveVersion version)
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
    }
}