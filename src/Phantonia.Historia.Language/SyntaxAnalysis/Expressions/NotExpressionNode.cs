using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public sealed record NotExpressionNode() : ExpressionNode
{
    public required Token NotKeywordToken { get; init; }

    public required ExpressionNode InnerExpression { get; init; }

    public override IEnumerable<SyntaxNode> Children => [InnerExpression];

    internal override string ReconstructCore() => NotKeywordToken.Reconstruct() + InnerExpression.Reconstruct();

    internal override void ReconstructCore(TextWriter writer)
    {
        writer.Write(NotKeywordToken.Reconstruct());
        InnerExpression.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"not ({InnerExpression.GetDebuggerDisplay()})";
}
