using System.Collections.Immutable;
using Phantonia.Historia.Language.Ast.Statements;

namespace Phantonia.Historia.Language.Ast.Symbols;

public sealed record SceneBodyNode : SyntaxNode
{
    public SceneBodyNode() { }

    public ImmutableArray<StatementNode> Statements { get; init; }
}
