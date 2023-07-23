using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.Statements;

public sealed record SwitchStatementNode : StatementNode, IOutputStatementNode
{
    public SwitchStatementNode() { }

    public string? Name { get; init; }

    public required ExpressionNode OutputExpression { get; init; }

    public required ImmutableArray<OptionNode> Options { get; init; }
}
