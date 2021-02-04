using System;
using System.Collections.Generic;
using System.Linq;
using FuncSharp;

namespace Mews.Fiscalization.SignatureChecker.Model
{
    internal sealed class Signature
    {
        private Signature(string base64String, byte[] value)
        {
            Base64String = base64String;
            Value = value;
        }

        public string Base64String { get; }

        public byte[] Value { get; }

        public static ITry<Signature, IEnumerable<string>> Create(string base64String)
        {
            var value = Try.Create<byte[], Exception>(_ =>
            {
                var paddingLength = (base64String.Length % 4).Match(
                    0, u => 0,
                    i => 4 - i
                );
                var sourceBase64 = base64String.Replace('-', '+').Replace('_', '/')
                    .PadRight(base64String.Length + paddingLength, '=');
                return Convert.FromBase64String(sourceBase64);
            });

            return value.Map(v => new Signature(base64String, v)).MapError(e => "Failed to read signature.".ToEnumerable());
        }
    }
}