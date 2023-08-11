using System.Diagnostics;

namespace Phantonia.Historia.Language.LexicalAnalysis;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public readonly record struct Token
{
    public required TokenKind Kind { get; init; }

    public required int Index { get; init; }

    public required string Text { get; init; }

    public int? IntegerValue { get; init; }

    private string GetDebuggerDisplay() => $"{Kind} token w/ text ({Text}) @ index {Index}";
}
