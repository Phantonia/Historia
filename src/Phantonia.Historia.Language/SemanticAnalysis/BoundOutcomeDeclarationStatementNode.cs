using Phantonia.Historia.Language.GrammaticalAnalysis.Statements;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed record BoundNamedSwitchStatementNode : SwitchStatementNode
{
    public BoundNamedSwitchStatementNode() { }

    public required OutcomeSymbol Outcome { get; init; }
}
