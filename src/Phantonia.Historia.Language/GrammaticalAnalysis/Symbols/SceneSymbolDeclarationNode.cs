using Phantonia.Historia.Language.GrammaticalAnalysis.Statements;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.TopLevel;

public sealed record SceneSymbolDeclarationNode : SymbolDeclarationNode
{
    public SceneSymbolDeclarationNode() { }

    public required StatementBodyNode Body { get; init; }
}
