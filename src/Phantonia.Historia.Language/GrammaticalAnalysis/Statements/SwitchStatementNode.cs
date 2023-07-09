using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.Statements;

public sealed record SwitchStatementNode : StatementNode
{
    public SwitchStatementNode() { }

    public required ExpressionNode Expression { get; init; }

    public required ImmutableArray<OptionNode> Options { get; init; }
}
