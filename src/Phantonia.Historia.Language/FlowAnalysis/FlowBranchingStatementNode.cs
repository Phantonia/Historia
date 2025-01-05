using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace Phantonia.Historia.Language.FlowAnalysis;

public sealed record FlowBranchingStatementNode() : StatementNode
{
    public required IBranchingStatementNode Original { get; init; }

    public required ImmutableList<FlowEdge> OutgoingEdges { get; init; }

    public override IEnumerable<SyntaxNode> Children => [(SyntaxNode)Original];

    protected override void ReconstructCore(TextWriter writer) => Original.Reconstruct(writer);

    protected internal override string GetDebuggerDisplay() => $"flow branching: {((SyntaxNode)Original).GetDebuggerDisplay()}";
}
