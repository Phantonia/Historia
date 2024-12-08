using Phantonia.Historia.Language.LexicalAnalysis;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public abstract record SymbolDeclarationNode() : TopLevelNode
{
    public required Token NameToken { get; init; }

    public string Name => NameToken.Text;
}
