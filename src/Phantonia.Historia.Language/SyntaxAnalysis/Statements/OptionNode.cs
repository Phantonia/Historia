using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public record OptionNode : SyntaxNode
{
    public OptionNode() { }

    public required ExpressionNode Expression { get; init; }

    public required StatementBodyNode Body { get; init; }

    public override IEnumerable<SyntaxNode> Children => [Expression, Body];

    protected internal override string GetDebuggerDisplay() => $"option ({Expression.GetDebuggerDisplay()}) w/ {Body.Statements.Length} statements";
}
