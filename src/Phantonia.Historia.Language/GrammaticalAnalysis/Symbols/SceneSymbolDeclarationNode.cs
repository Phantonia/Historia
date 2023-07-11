using Phantonia.Historia.Language.GrammaticalAnalysis.Statements;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.Symbols;

public sealed record SceneSymbolDeclarationNode : NamedSymbolDeclarationNode
{
    public SceneSymbolDeclarationNode() { }

    public required StatementBodyNode Body { get; init; }
}
