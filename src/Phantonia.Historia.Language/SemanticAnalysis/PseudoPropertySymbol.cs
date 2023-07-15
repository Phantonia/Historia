using Phantonia.Historia.Language.GrammaticalAnalysis.Types;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed record PseudoPropertySymbol : Symbol
{
    public PseudoPropertySymbol() { }

    public required TypeNode Type { get; init; }
}
