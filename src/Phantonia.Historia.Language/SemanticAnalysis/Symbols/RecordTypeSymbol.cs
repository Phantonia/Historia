using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.SemanticAnalysis.Symbols;

public sealed record RecordTypeSymbol() : TypeSymbol
{
    public required ImmutableArray<PropertySymbol> Properties { get; init; }

    protected internal override string GetDebuggerDisplay() => $"record type symbol {Name} w/ properties ({string.Join(", ", Properties.Select(p => p.GetDebuggerDisplay()))})";
}
