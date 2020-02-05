using System;
using FuncSharp;

namespace Mews.SignatureChecker
{
    public static class Base64Url
    {
        public static byte[] GetBytes(string encodedString)
        {
            var paddingLength = (encodedString.Length % 4).Match(
                0, _ => 0,
                i => 4 - i
            );
            var sourceBase64 = encodedString.Replace('-', '+').Replace('_', '/')
                .PadRight(encodedString.Length + paddingLength, '=');
            return Convert.FromBase64String(sourceBase64);
        }
    }
}