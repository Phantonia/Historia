using System.Collections.Generic;
using System.Linq;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;

public sealed record StringLiteralExpressionNode : ExpressionNode
{
    public StringLiteralExpressionNode() { }

    public required string StringLiteral { get; init; }

    public override IEnumerable<SyntaxNode> Children => Enumerable.Empty<SyntaxNode>();
}
