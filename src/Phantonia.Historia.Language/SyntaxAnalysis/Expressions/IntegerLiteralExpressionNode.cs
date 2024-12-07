using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public sealed record IntegerLiteralExpressionNode() : ExpressionNode
{
    public required int Value { get; init; }

    public override IEnumerable<SyntaxNode> Children => [];

    protected internal override string GetDebuggerDisplay() => $"integer {Value}";
}
