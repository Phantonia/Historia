using System.Collections.Immutable;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public record OutcomeSymbol : Symbol
{
    public OutcomeSymbol() { }

    public required ImmutableArray<string> OptionNames { get; init; }

    public string? DefaultOption { get; init; }
}
