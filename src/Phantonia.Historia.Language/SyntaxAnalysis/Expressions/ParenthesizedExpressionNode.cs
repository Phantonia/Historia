using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public sealed record ParenthesizedExpressionNode : ExpressionNode
{
    public required Token OpenParenthesisToken { get; init; }

    public required ExpressionNode InnerExpression { get; init; }

    public required Token ClosedParenthesisToken { get; init; }

    public override IEnumerable<SyntaxNode> Children => [InnerExpression];

    protected override void ReconstructCore(TextWriter writer)
    {
        OpenParenthesisToken.Reconstruct(writer);
        InnerExpression.Reconstruct(writer);
        ClosedParenthesisToken.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"({InnerExpression.GetDebuggerDisplay()})";
}
