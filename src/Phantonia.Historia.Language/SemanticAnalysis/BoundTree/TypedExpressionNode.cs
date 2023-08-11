using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.SemanticAnalysis.BoundTree;

public sealed record TypedExpressionNode : ExpressionNode
{
    public TypedExpressionNode() { }

    public required ExpressionNode Expression { get; init; }

    public required TypeSymbol SourceType { get; init; }

    public TypeSymbol? TargetType { get; init; }

    public override IEnumerable<SyntaxNode> Children => new[] { Expression };

    protected internal override string GetDebuggerDisplay() => $"{Expression.GetDebuggerDisplay()} w/ type {SourceType.GetDebuggerDisplay()} (target type: {TargetType?.GetDebuggerDisplay() ?? "none"}";
}
