namespace Phantonia.Historia.Language.Ast;

public readonly record struct Token
{
    public required TokenKind Kind { get; init; }

    public required int Index { get; init; }

    public required string Text { get; init; }

    public int? IntegerValue { get; init; }
}
