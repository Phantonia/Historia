namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public abstract record SymbolDeclarationNode() : TopLevelNode
{
    public required string Name { get; init; }
}
