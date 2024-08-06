using System;
using System.Collections.Generic;
using FuncSharp;

namespace Mews.Fiscalization.SignatureChecker.Model;

internal sealed class Signature
{
    private Signature(string base64UrlString, byte[] value)
    {
        Base64UrlString = base64UrlString;
        Value = value;
    }

    public string Base64UrlString { get; }

    public byte[] Value { get; }

    public static Try<Signature, IReadOnlyList<string>> Create(string base64UrlString)
    {
        var value = Try.Catch<byte[], Exception>(_ =>
        {
            var paddingLength = (base64UrlString.Length % 4).Match(
                0, u => 0,
                i => 4 - i
            );
            var sourceBase64 = base64UrlString.Replace('-', '+').Replace('_', '/')
                .PadRight(base64UrlString.Length + paddingLength, '=');
            return Convert.FromBase64String(sourceBase64);
        });

        return value.Map(v => new Signature(base64UrlString, v)).MapError(e => "Failed to read signature.".ToReadOnlyList());
    }
}