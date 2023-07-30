using System.Collections.Immutable;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed record UnionTypeSymbol : TypeSymbol
{
    public UnionTypeSymbol() { }

    public required ImmutableArray<TypeSymbol> Subtypes { get; init; }
}
