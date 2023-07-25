using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.Statements;

public record SwitchStatementNode : StatementNode, IOutputStatementNode
{
    public SwitchStatementNode() { }

    public string? Name { get; init; }

    public required ExpressionNode OutputExpression { get; init; }

    public required ImmutableArray<SwitchOptionNode> Options { get; init; }

    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            yield return OutputExpression;

            foreach (SwitchOptionNode option in Options)
            {
                yield return option;
            }
        }
    }
}
