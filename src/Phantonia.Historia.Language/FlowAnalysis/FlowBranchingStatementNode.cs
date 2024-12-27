using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.FlowAnalysis;

public sealed record FlowBranchingStatementNode() : StatementNode
{
    public required IBranchingStatementNode Original { get; init; }

    public required ImmutableList<FlowEdge> OutgoingEdges { get; init; }

    public override IEnumerable<SyntaxNode> Children => [(SyntaxNode)Original];

    protected internal override string GetDebuggerDisplay() => ((SyntaxNode)Original).GetDebuggerDisplay();
}
