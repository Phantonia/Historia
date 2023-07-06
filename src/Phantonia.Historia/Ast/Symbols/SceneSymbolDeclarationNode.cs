namespace Phantonia.Historia.Language.Ast.Symbols;

public sealed record SceneSymbolDeclarationNode : SymbolDeclarationNode
{
    public SceneSymbolDeclarationNode() { }

    public required SceneBodyNode Body { get; init; }
}
