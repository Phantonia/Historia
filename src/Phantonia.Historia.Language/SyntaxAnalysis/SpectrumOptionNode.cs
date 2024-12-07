using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis;

public sealed record SpectrumOptionNode() : SyntaxNode
{
    public required string Name { get; init; }

    public required bool Inclusive { get; init; }

    public required int Numerator { get; init; }

    public required int Denominator { get; init; }

    public override IEnumerable<SyntaxNode> Children => [];

    protected internal override string GetDebuggerDisplay() => $"spectrum option {Name} <{(Inclusive ? "=" : "")} {Numerator}/{Denominator}";
}
