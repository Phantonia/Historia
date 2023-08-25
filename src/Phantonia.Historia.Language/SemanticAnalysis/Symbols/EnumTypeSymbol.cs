global using PseudoEnumTypeSymbol = Phantonia.Historia.Language.SemanticAnalysis.Symbols.EnumTypeSymbol;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.SemanticAnalysis.Symbols;

public sealed record EnumTypeSymbol : TypeSymbol
{
    public EnumTypeSymbol() { }

    public required ImmutableArray<string> Options { get; init; }

    protected internal override string GetDebuggerDisplay() => $"enum type {Name} ({string.Join(", ", Options)})";
}
