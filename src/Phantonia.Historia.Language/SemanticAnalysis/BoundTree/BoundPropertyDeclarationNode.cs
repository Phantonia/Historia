using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

namespace Phantonia.Historia.Language.SemanticAnalysis.BoundTree;

public sealed record BoundPropertyDeclarationNode() : PropertyDeclarationNode
{
    public required PropertySymbol Symbol { get; init; }
}
