using System;
using System.Security.Cryptography;
using FuncSharp;
using Newtonsoft.Json;

namespace Mews.SignatureChecker
{
    internal sealed class ArchiveMetadata
    {
        [JsonConstructor]
        public ArchiveMetadata(string terminalIdentification, string previousRecordSignature, DateTime created, string version, HashAlgorithmName hashAlgorithmName)
        {
            TerminalIdentification = terminalIdentification;
            PreviousRecordSignature = previousRecordSignature.ToNonEmptyOption();
            Created = created;
            Version = version;
            HashAlgorithmName = hashAlgorithmName;
        }

        public string TerminalIdentification { get; }

        public IOption<string> PreviousRecordSignature { get; }

        public DateTime Created { get; }

        public string Version { get; }

        public HashAlgorithmName HashAlgorithmName { get; }
    }
}