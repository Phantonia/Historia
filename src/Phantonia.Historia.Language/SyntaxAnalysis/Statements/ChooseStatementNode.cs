using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record ChooseStatementNode() : MethodCallStatementNode, IBranchingStatementNode
{
    public required ImmutableArray<OptionNode> Options { get; init; }

    public override IEnumerable<SyntaxNode> Children => base.Children.Concat(Options);

    protected internal override string GetDebuggerDisplay()
        => $"choose {ReferenceName}.{MethodName}({string.Join(", ", Arguments.Select(a => a.GetDebuggerDisplay()))}) {{ {string.Join(", ", Options.Select(o => o.GetDebuggerDisplay()))} }}";

    IEnumerable<StatementBodyNode> IBranchingStatementNode.Bodies => Options.Select(o => o.Body);
}
