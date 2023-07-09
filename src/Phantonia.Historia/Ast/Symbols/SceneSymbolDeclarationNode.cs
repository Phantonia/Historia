using Phantonia.Historia.Language.Ast.Statements;

namespace Phantonia.Historia.Language.Ast.Symbols;

public sealed record SceneSymbolDeclarationNode : SymbolDeclarationNode
{
    public SceneSymbolDeclarationNode() { }

    public required StatementBodyNode Body { get; init; }
}
