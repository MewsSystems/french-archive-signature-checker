namespace Mews.Fiscalization.SignatureChecker.Dto;

internal sealed class File(string name, string content)
{
    public string Name { get; } = name;

    public string Content { get; } = content;
}
