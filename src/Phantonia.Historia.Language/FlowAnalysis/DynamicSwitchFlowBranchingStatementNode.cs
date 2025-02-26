using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.FlowAnalysis;

public sealed record DynamicSwitchFlowBranchingStatementNode() : FlowBranchingStatementNode
{
    public required FlowEdge? NonOptionEdge { get; init; }

    public required ImmutableArray<ExpressionNode> OptionExpressions { get; init; }

    protected internal override string GetDebuggerDisplay() => $"dynamic switch branching: {Original.GetDebuggerDisplay()}";
}
