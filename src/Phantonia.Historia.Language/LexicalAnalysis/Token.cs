using System.Diagnostics;
using System.IO;

namespace Phantonia.Historia.Language.LexicalAnalysis;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public readonly record struct Token
{
    public required TokenKind Kind { get; init; }

    public required int Index { get; init; }

    public required string Text { get; init; }

    public required string PrecedingTrivia { get; init; }

    public int? IntegerValue { get; init; }

    public string? StringValue { get; init; }

    public void Reconstruct(TextWriter writer)
    {
        writer.Write(PrecedingTrivia);
        writer.Write(Text);
    }

    private string GetDebuggerDisplay() => $"{Kind} token w/ text ({Text}) @ index {Index}";
}
