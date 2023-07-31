﻿using System.Collections.Immutable;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed record SpectrumSymbol : OutcomeSymbol
{
    public SpectrumSymbol() { }

    public required ImmutableDictionary<string, SpectrumInterval> Intervals { get; init; }
}
