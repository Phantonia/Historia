using Phantonia.Historia.Language.Ast.Expressions;

namespace Phantonia.Historia.Language.Flow;

public readonly record struct FlowVertex
{
    public required int Index { get; init; }

    public ExpressionNode? OutputExpression { get; init; }
}
