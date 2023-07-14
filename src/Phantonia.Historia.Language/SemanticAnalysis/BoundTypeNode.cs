using Phantonia.Historia.Language.GrammaticalAnalysis.Types;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed record BoundTypeNode : TypeNode
{
    public BoundTypeNode() { }

    public required TypeNode Node { get; init; }

    public required TypeSymbol Symbol { get; init; }
}
