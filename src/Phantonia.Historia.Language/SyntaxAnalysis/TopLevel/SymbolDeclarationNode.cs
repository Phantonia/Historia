namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public abstract record SymbolDeclarationNode : TopLevelNode
{
    public SymbolDeclarationNode() { }

    public required string Name { get; init; }
}
