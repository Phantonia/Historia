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

    public override IEnumerable<SyntaxNode> Children => [LeftExpression, RightExpression];

    internal override string ReconstructCore() => LeftExpression.Reconstruct() + OperatorToken.Reconstruct() + RightExpression.Reconstruct();

    internal override void ReconstructCore(TextWriter writer)
    {
        LeftExpression.Reconstruct(writer);
        writer.Write(OperatorToken.Reconstruct());
        RightExpression.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"({LeftExpression.GetDebuggerDisplay()}) {Operator} ({RightExpression.GetDebuggerDisplay()})";
}
