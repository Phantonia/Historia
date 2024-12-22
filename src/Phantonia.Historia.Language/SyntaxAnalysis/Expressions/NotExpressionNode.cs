using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public sealed record NotExpressionNode() : ExpressionNode
{
    public required ExpressionNode InnerExpression { get; init; }

    public override IEnumerable<SyntaxNode> Children => [InnerExpression];

    protected internal override string GetDebuggerDisplay() => $"not ({InnerExpression.GetDebuggerDisplay()})";
}
