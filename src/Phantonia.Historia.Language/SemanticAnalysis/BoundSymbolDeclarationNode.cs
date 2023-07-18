using Phantonia.Historia.Language.GrammaticalAnalysis.TopLevel;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed record BoundSymbolDeclarationNode : SymbolDeclarationNode
{
    public BoundSymbolDeclarationNode() { }

    public required SymbolDeclarationNode Declaration { get; init; }

    public required Symbol Symbol { get; init; }
}
