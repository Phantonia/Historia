using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed record BoundArgumentNode : ArgumentNode
{
    public BoundArgumentNode() { }

    public required PropertySymbol Property { get; init; }
}
