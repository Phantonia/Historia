using Phantonia.Historia.Language.GrammaticalAnalysis;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.Symbols;

public abstract record SymbolDeclarationNode : SyntaxNode
{
    protected SymbolDeclarationNode() { }

    public required string Name { get; init; }
}
