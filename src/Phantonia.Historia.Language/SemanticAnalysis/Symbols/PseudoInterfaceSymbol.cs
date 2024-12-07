using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.SemanticAnalysis.Symbols;

public sealed record PseudoInterfaceSymbol() : Symbol
{
    public required ImmutableArray<PseudoInterfaceMethodSymbol> Methods { get; init; }

    protected internal override string GetDebuggerDisplay() => $"interface {Name} w/ methods ({string.Join(", ", Methods.Select(m => m.GetDebuggerDisplay()))})";
}
