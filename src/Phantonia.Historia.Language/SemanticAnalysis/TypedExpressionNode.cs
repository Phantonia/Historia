using Phantonia.Historia.Language.GrammaticalAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed record TypedExpressionNode : ExpressionNode
{
    public TypedExpressionNode() { }

    public required ExpressionNode Expression { get; init; }

    public required TypeSymbol Type { get; init; }

    public override IEnumerable<SyntaxNode> Children => new[] { Expression };
}
