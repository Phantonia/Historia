using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public sealed record NotExpressionNode() : ExpressionNode
{
    public required Token NotKeywordToken { get; init; }

    public required ExpressionNode InnerExpression { get; init; }

    public override bool IsConstant => InnerExpression.IsConstant;

    public override IEnumerable<SyntaxNode> Children => [InnerExpression];

    protected override void ReconstructCore(TextWriter writer)
    {
        NotKeywordToken.Reconstruct(writer);
        InnerExpression.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"not ({InnerExpression.GetDebuggerDisplay()})";
}
