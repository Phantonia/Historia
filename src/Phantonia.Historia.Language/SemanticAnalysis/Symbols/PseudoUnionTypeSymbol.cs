using Phantonia.Historia.Language.SyntaxAnalysis.Types;
using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.SemanticAnalysis.Symbols;

public sealed record PseudoUnionTypeSymbol : TypeSymbol
{
    public PseudoUnionTypeSymbol() { }

    public required ImmutableArray<TypeNode> Subtypes { get; init; }

    protected internal override string GetDebuggerDisplay() => $"pseudo union symbol w/ subtypes ({string.Join(", ", Subtypes.Select(p => p.GetDebuggerDisplay()))})";
}
