using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public record SwitchStatementNode : StatementNode, IOutputStatementNode
{
    public SwitchStatementNode() { }

    public string? Name { get; init; }

    public required ExpressionNode OutputExpression { get; init; }

    public required ImmutableArray<SwitchOptionNode> Options { get; init; }

    public required bool IsCheckpoint { get; init; }

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

    protected internal override string GetDebuggerDisplay() => $"switch {Name}{(Name is not null ? " " : "")} {{ {string.Join(", ", Options.Select(o => o.GetDebuggerDisplay()))} }}";
}
