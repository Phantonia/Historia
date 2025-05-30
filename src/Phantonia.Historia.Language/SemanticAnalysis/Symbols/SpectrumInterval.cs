﻿namespace Phantonia.Historia.Language.SemanticAnalysis.Symbols;

public readonly record struct SpectrumInterval
{
    public required bool Inclusive { get; init; }

    public required int UpperNumerator { get; init; }

    public required int UpperDenominator { get; init; }
}
