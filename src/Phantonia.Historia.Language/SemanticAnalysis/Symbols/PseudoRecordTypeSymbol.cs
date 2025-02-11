using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.SemanticAnalysis.Symbols;

public sealed record PseudoRecordTypeSymbol() : TypeSymbol
{
    public required bool IsLineRecord { get; init; }

    public required ImmutableArray<PseudoPropertySymbol> Properties { get; init; }

    protected internal override string GetDebuggerDisplay() => $"pseudo record symbol w/ properties ({string.Join(", ", Properties.Select(p => p.GetDebuggerDisplay()))})";
}
