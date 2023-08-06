using Phantonia.Historia.Language.SyntaxAnalysis.Types;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.SemanticAnalysis.Symbols;

public sealed record PseudoRecordTypeSymbol : TypeSymbol
{
    public PseudoRecordTypeSymbol() { }

    public required ImmutableArray<PseudoPropertySymbol> Properties { get; init; }
}

public sealed record PseudoUnionTypeSymbol : TypeSymbol
{
    public PseudoUnionTypeSymbol() { }

    public required ImmutableArray<TypeNode> Subtypes { get; init; }
}
