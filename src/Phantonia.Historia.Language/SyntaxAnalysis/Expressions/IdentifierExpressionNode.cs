using System.Collections.Generic;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public sealed record IdentifierExpressionNode : ExpressionNode
{
    public IdentifierExpressionNode() { }

    public required string Identifier { get; init; }

    public override IEnumerable<SyntaxNode> Children => Enumerable.Empty<SyntaxNode>();
}
