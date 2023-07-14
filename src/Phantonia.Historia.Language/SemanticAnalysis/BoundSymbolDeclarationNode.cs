using Phantonia.Historia.Language.GrammaticalAnalysis.Symbols;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed record BoundSymbolDeclarationNode : NamedSymbolDeclarationNode
{
    public BoundSymbolDeclarationNode() { }

    public required NamedSymbolDeclarationNode Declaration { get; init; }

    public required Symbol Symbol { get; init; }
}
