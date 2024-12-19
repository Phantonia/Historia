using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public sealed record LogicExpressionNode() : ExpressionNode
{
    public required ExpressionNode LeftExpression { get; init; }

    public required ExpressionNode RightExpression { get; init; }

    public required LogicOperator Operator { get; init; }

    public override IEnumerable<SyntaxNode> Children => [LeftExpression, RightExpression];

    protected internal override string GetDebuggerDisplay() => $"({LeftExpression.GetDebuggerDisplay()}) {Operator} ({RightExpression.GetDebuggerDisplay()})";
}
