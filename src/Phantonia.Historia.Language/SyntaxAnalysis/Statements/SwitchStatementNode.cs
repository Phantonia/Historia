using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record SwitchStatementNode() : StatementNode, IOutputStatementNode, IBranchingStatementNode
{
    public required ExpressionNode OutputExpression { get; init; }

    public required ImmutableArray<OptionNode> Options { get; init; }

    public required bool IsCheckpoint { get; init; }

    public override IEnumerable<SyntaxNode> Children => [OutputExpression, .. Options];

    protected internal override string GetDebuggerDisplay() => $"switch {{ {string.Join(", ", Options.Select(o => o.GetDebuggerDisplay()))} }}";

    IEnumerable<StatementBodyNode> IBranchingStatementNode.Bodies => Options.Select(o => o.Body);
}
