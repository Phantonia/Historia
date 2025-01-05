using System;
using System.Diagnostics;
using System.IO;

namespace Phantonia.Historia.Language.LexicalAnalysis;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public readonly record struct Token : IReconstructable
{
    internal static Token Missing(long index) => new()
    {
        Index = index,
        Kind = TokenKind.Missing,
        PrecedingTrivia = "",
        Text = "",
    };

    public required TokenKind Kind { get; init; }

    public required long Index { get; init; }

    public required string Text { get; init; }

    public required string PrecedingTrivia { get; init; }

    public int? IntegerValue { get; init; }

    public string? StringValue { get; init; }

    public void Reconstruct(TextWriter writer)
    {
        writer.Write(PrecedingTrivia);
        writer.Write(Text);
    }

    public string Reconstruct() => PrecedingTrivia + Text;

    private string GetDebuggerDisplay() => $"{Kind} token w/ text ({Text}) @ index {Index}";
}
