using System.Collections.Immutable;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed record PseudoRecordTypeSymbol : TypeSymbol
{
    public PseudoRecordTypeSymbol() { }
    
    public required ImmutableArray<PseudoPropertySymbol> Properties { get; init; }
}
