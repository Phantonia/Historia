namespace Phantonia.Historia.Language.GrammaticalAnalysis.TopLevel;

public abstract record SymbolDeclarationNode : TopLevelNode
{
    public SymbolDeclarationNode() { }

    public required string Name { get; init; }
}
