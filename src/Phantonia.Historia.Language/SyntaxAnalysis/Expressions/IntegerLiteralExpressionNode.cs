using System.Collections.Generic;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public sealed record IntegerLiteralExpressionNode : ExpressionNode
{
    public IntegerLiteralExpressionNode() { }

    public required int Value { get; init; }

    public override IEnumerable<SyntaxNode> Children => Enumerable.Empty<SyntaxNode>();

    protected internal override string GetDebuggerDisplay() => $"integer {Value}";
}
