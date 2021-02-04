using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FuncSharp;
using Mews.Fiscalization.SignatureChecker.Model;

namespace Mews.Fiscalization.SignatureChecker
{
    internal static class Extensions
    {
        internal static ITry<T, IEnumerable<string>> Parse<T>(this Dto.ArchiveEntry entry, Func<Dto.ArchiveEntry, T> parser)
        {
            var result = Try.Create<T, Exception>(_ => parser(entry));
            return result.MapError(_ => $"Invalid data ({entry.Name}).".ToEnumerable());
        }

        internal static IEnumerable<T> ToEnumerable<T>(this T value)
        {
            return new List<T>{value};
        }

        internal static string MkLines(this IEnumerable<string> values)
        {
            return $"{String.Join(Environment.NewLine, values)}";
        }

        internal static string ToSignatureString(this DateTime dateTime)
        {
            return dateTime.ToString("yyyyMMddHHmmss");
        }

        internal static string ToSignatureString(this Amount amount)
        {
            return ((int)(amount.Value * 100)).ToString();
        }
    }
}