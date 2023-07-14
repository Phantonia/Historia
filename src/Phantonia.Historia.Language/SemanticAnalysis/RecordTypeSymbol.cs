using System.Collections.Immutable;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed record RecordTypeSymbol : TypeSymbol
{
    public RecordTypeSymbol() { }
    
    public required ImmutableArray<PropertySymbol> Properties { get; init; }
}
