using Phantonia.Historia.Language.Ast.Expressions;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.Ast.Statements;

public sealed record SwitchStatementNode : StatementNode
{
    public SwitchStatementNode() { }

    public required ExpressionNode Expression { get; init; }

    public required ImmutableArray<OptionNode> Options { get; init; }
}
