using System.Collections.Immutable;

namespace Phantonia.Historia.Language.SemanticAnalysis.Symbols;

public sealed record RecordTypeSymbol : TypeSymbol
{
    public RecordTypeSymbol() { }

    public required ImmutableArray<PropertySymbol> Properties { get; init; }
}
