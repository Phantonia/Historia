using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;

namespace Phantonia.Historia.Language.SemanticAnalysis.BoundTree;

public sealed record BoundOutcomeAssignmentStatementNode : AssignmentStatementNode
{
    public BoundOutcomeAssignmentStatementNode() { }

    public required OutcomeSymbol Outcome { get; init; }

    public string AssignedOption => ((IdentifierExpressionNode)AssignedExpression).Identifier;
}
