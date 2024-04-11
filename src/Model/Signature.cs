namespace Mews.Fiscalization.SignatureChecker.Model;

internal sealed record Signature(string Base64UrlString, byte[] Value)
{
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

        return value.Map(v => new Signature(base64UrlString, v)).MapError(e => $"Failed to read signature. {e.Message}".ToReadOnlyList());
    }
}