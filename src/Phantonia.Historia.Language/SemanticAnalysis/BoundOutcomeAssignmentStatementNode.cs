using Phantonia.Historia.Language.GrammaticalAnalysis.Statements;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed record BoundOutcomeAssignmentStatementNode : AssignmentStatementNode
{
    public BoundOutcomeAssignmentStatementNode() { }

    public required OutcomeSymbol Outcome { get; init; }
}
