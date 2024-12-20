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

    internal override string ReconstructCore() => OpenParenthesisToken.Reconstruct() + InnerExpression.Reconstruct() + ClosedParenthesisToken.Reconstruct();

    internal override void ReconstructCore(TextWriter writer)
    {
        writer.Write(OpenParenthesisToken.Reconstruct());
        InnerExpression.Reconstruct(writer);
        writer.Write(ClosedParenthesisToken.Reconstruct());
    }

    protected internal override string GetDebuggerDisplay() => $"({InnerExpression.GetDebuggerDisplay()})";
}
