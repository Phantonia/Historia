using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public sealed record IdentifierExpressionNode() : ExpressionNode
{
    public required string Identifier { get; init; }

    public override IEnumerable<SyntaxNode> Children => [];

    protected internal override string GetDebuggerDisplay() => $"identifier {Identifier}";
}
