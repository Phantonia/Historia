namespace Phantonia.Historia.Language.Ast.Symbols;

public sealed record SceneSymbolDeclarationNode : SymbolDeclarationNode
{
    public SceneSymbolDeclarationNode() { }

    public required string Name { get; init; }

    public required SceneBodyNode Body { get; init; }
}
