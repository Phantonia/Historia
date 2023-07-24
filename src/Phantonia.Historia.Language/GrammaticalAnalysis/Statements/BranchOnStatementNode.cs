using System.Collections.Immutable;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.Statements;

public sealed record BranchOnStatementNode : StatementNode
{
    public BranchOnStatementNode() { }

    public required string OutcomeName { get; init; }

    public required ImmutableArray<BranchOnOptionNode> Options { get; init; }
}
