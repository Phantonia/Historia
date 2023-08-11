using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.SemanticAnalysis.Symbols;

public sealed record UnionTypeSymbol : TypeSymbol
{
    public UnionTypeSymbol() { }

    public required ImmutableArray<TypeSymbol> Subtypes { get; init; }

    protected internal override string GetDebuggerDisplay() => $"union type symbol {Name} w/ subtypes ({string.Join(", ", Subtypes.Select(s => s.GetDebuggerDisplay()))})";
}
