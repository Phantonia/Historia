namespace Phantonia.Historia.Language.GrammaticalAnalysis;

public abstract record SyntaxNode
{
    protected SyntaxNode() { }

    public required int Index { get; init; }
}
