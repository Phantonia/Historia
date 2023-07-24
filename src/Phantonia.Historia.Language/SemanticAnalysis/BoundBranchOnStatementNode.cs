using Phantonia.Historia.Language.GrammaticalAnalysis.Statements;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed record BoundBranchOnStatementNode : BranchOnStatementNode
{
    public BoundBranchOnStatementNode() { }

    public required OutcomeSymbol Outcome { get; init; }
}
