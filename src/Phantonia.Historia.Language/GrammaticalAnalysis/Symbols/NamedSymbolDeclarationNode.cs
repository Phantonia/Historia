namespace Phantonia.Historia.Language.GrammaticalAnalysis.Symbols;

public abstract record NamedSymbolDeclarationNode : SymbolDeclarationNode
{
    public NamedSymbolDeclarationNode() { }

    public required string Name { get; init; }
}
