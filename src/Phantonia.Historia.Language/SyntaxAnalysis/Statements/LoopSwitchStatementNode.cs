using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record LoopSwitchStatementNode : StatementNode
{
    public LoopSwitchStatementNode() { }

    public required ExpressionNode OutputExpression { get; init; }

    public required ImmutableArray<LoopSwitchOptionNode> Options { get; init; }

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

}
