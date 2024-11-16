using System.Collections.Immutable;

namespace Phantonia.Historia.Language.SemanticAnalysis.Symbols;

public sealed record SpectrumSymbol() : OutcomeSymbol
{
    public required ImmutableDictionary<string, SpectrumInterval> Intervals { get; init; }
}
