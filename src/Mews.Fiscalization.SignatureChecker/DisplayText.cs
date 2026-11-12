using System.Globalization;
using System.Text;

namespace Mews.Fiscalization.SignatureChecker;

/// <summary>
/// Makes text that comes from an archive (or from the user) safe to print. A crafted archive could otherwise put
/// newlines, ANSI/OSC escape sequences or bidirectional overrides into a value such as the terminal identification,
/// and use them to hide the real result or print a fake one. Only the displayed text is escaped; the values used to
/// verify the signature are never changed.
/// </summary>
internal static class DisplayText
{
    public static string Escape(string value)
    {
        if (value is null)
        {
            return value;
        }
        if (!value.Any(IsUnsafe))
        {
            return value;
        }

        var builder = new StringBuilder(value.Length + 16);
        foreach (var c in value)
        {
            if (IsUnsafe(c))
            {
                builder.Append($"\\u{(int)c:X4}");
            }
            else
            {
                builder.Append(c);
            }
        }
        return builder.ToString();
    }

    public static string EscapeLines(IEnumerable<string> lines)
    {
        return lines.Select(Escape).MkLines();
    }

    private static bool IsUnsafe(char c)
    {
        // Control characters (newlines, ESC, BEL, C1 controls such as CSI), invisible formatting characters
        // (bidirectional overrides and isolates, zero-width characters) and Unicode line/paragraph separators.
        return char.GetUnicodeCategory(c) switch
        {
            UnicodeCategory.Control => true,
            UnicodeCategory.Format => true,
            UnicodeCategory.LineSeparator => true,
            UnicodeCategory.ParagraphSeparator => true,
            _ => false
        };
    }
}
