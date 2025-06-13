using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.FlowAnalysis;

public readonly record struct ChapterData
{
    public required IEnumerable<OutcomeSymbol> DefinitelyAssignedOutcomes { get; init; }

    public required uint EntryVertex { get; init; }
}
