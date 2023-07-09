using System.Collections.Immutable;

namespace Phantonia.Historia.Language.Ast.Statements;

public sealed record StatementBodyNode : SyntaxNode
{
    public StatementBodyNode() { }

    public ImmutableArray<StatementNode> Statements { get; init; }
}
