using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record OutputStatementNode() : StatementNode, IOutputStatementNode
{
    public required ExpressionNode OutputExpression { get; init; }

    public required bool IsCheckpoint { get; init; }

    public override IEnumerable<SyntaxNode> Children => [OutputExpression];

    protected internal override string GetDebuggerDisplay() => $"output {OutputExpression.GetDebuggerDisplay()}";
}
