using Phantonia.Historia.Language.SyntaxAnalysis.Types;

namespace Phantonia.Historia.Language.SemanticAnalysis.Symbols;

public sealed record PseudoPropertySymbol : Symbol
{
    public PseudoPropertySymbol() { }

    public required TypeNode Type { get; init; }
}
