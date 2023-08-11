using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public record AssignmentStatementNode : StatementNode
{
    public AssignmentStatementNode() { }

    public required string VariableName { get; init; }

    public required ExpressionNode AssignedExpression { get; init; }

    public override IEnumerable<SyntaxNode> Children => new[] { AssignedExpression };

    protected internal override string GetDebuggerDisplay() => $"assignment {VariableName} = {AssignedExpression.GetDebuggerDisplay()}";
}
