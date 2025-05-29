using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public sealed record LogicExpressionNode() : ExpressionNode
{
    public required ExpressionNode LeftExpression { get; init; }

    public required Token OperatorToken { get; init; }

    public LogicOperator Operator => (LogicOperator)OperatorToken.Kind;

    public required ExpressionNode RightExpression { get; init; }

    public override bool IsConstant => LeftExpression.IsConstant && RightExpression.IsConstant;

    public override IEnumerable<SyntaxNode> Children => [LeftExpression, RightExpression];

    protected override void ReconstructCore(TextWriter writer)
    {
        LeftExpression.Reconstruct(writer);
        OperatorToken.Reconstruct(writer);
        RightExpression.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"({LeftExpression.GetDebuggerDisplay()}) {Operator} ({RightExpression.GetDebuggerDisplay()})";
}
