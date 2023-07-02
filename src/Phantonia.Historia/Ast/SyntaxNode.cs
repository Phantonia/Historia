namespace Phantonia.Historia.Language.Ast;

public abstract record SyntaxNode
{
    protected SyntaxNode() { }

    public required int Index { get; init; }
}
