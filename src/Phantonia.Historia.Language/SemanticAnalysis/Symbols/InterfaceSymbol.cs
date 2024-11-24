using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.SemanticAnalysis.Symbols;

public sealed record InterfaceSymbol() : Symbol
{
    public required ImmutableArray<InterfaceMethodSymbol> Methods { get; init; }

    protected internal override string GetDebuggerDisplay() => $"interface {Name} w/ methods ({string.Join(", ", Methods.Select(m => m.GetDebuggerDisplay()))})";
}
