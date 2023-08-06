using System.Collections.Generic;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis;

public sealed record SpectrumOptionNode : SyntaxNode
{
    public SpectrumOptionNode() { }

    public required string Name { get; init; }

    public required bool Inclusive { get; init; }

    public required int Numerator { get; init; }

    public required int Denominator { get; init; }

    public override IEnumerable<SyntaxNode> Children => Enumerable.Empty<SyntaxNode>();
}
