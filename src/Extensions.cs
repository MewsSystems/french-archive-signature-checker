using System;
using System.IO;

namespace Mews.Fiscalization.SignatureChecker
{
    public static class Extensions
    {
        public static string ToSignatureString(this DateTime dateTime)
        {
            return dateTime.ToString("yyyyMMddHHmmss");
        }
    }
}