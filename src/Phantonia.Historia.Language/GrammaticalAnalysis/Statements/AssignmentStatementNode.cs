using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.Statements;

public record AssignmentStatementNode : StatementNode
{
    public AssignmentStatementNode() { }

    public required string VariableName { get; init; }

    public required ExpressionNode AssignedExpression { get; init; }

    public override IEnumerable<SyntaxNode> Children => new[] { AssignedExpression };
}
