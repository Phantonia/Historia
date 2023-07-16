using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed record TypedExpressionNode : ExpressionNode
{
    public TypedExpressionNode() { }

    public required ExpressionNode Expression { get; init; }

    public required TypeSymbol Type { get; init; }
}
