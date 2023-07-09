using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;

namespace Phantonia.Historia.Language.FlowAnalysis;

public readonly record struct FlowVertex
{
    public required int Index { get; init; }

    public ExpressionNode? OutputExpression { get; init; }
}
