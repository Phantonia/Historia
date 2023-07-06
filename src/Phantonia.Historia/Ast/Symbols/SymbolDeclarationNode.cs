namespace Phantonia.Historia.Language.Ast.Symbols;

public abstract record SymbolDeclarationNode : SyntaxNode
{
    protected SymbolDeclarationNode() { }

    public required string Name { get; init; }
}
