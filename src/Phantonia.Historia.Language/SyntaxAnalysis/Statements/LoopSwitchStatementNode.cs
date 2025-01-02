using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record LoopSwitchStatementNode() : StatementNode, IOutputStatementNode, IBranchingStatementNode
{
    public required ExpressionNode OutputExpression { get; init; }

    public required ImmutableArray<LoopSwitchOptionNode> Options { get; init; }

    public required bool IsCheckpoint { get; init; }

    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            yield return OutputExpression;

            foreach (LoopSwitchOptionNode option in Options)
            {
                yield return option;
            }
        }
    }

    protected internal override string GetDebuggerDisplay() => $"loopswitch {{ {string.Join(", ", Options.Select(o => o.GetDebuggerDisplay()))} }}";

    IEnumerable<StatementBodyNode> IBranchingStatementNode.Bodies => Options.Select(o => o.Body);
}
