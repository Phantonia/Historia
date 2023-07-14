using Phantonia.Historia.Language.GrammaticalAnalysis.Types;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed record PropertySymbol : Symbol
{
    public PropertySymbol() { }

    public required TypeNode Type { get; init; }
}
