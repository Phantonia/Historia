using System.Collections.Immutable;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed record NamedSwitchSymbol : Symbol
{
    public NamedSwitchSymbol() { }

    public required ImmutableArray<string> OptionNames { get; init; }
}
