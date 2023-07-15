using Phantonia.Historia.Language.GrammaticalAnalysis.Symbols;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed record BoundPropertyDeclarationNode : PropertyDeclarationNode
{
    public BoundPropertyDeclarationNode() { }

    public required PropertySymbol Symbol { get; init; }
}
