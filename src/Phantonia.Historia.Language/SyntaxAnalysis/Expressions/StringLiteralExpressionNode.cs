using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public sealed record StringLiteralExpressionNode() : ExpressionNode
{
    public required string StringLiteral { get; init; }

    public override IEnumerable<SyntaxNode> Children => [];

    protected internal override string GetDebuggerDisplay() => $"string {StringLiteral}";
}
