using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Phantonia.Historia.Language.FlowAnalysis;

public readonly record struct FlowAnalysisResult
{
    [MemberNotNullWhen(returnValue: true, nameof(MainFlowGraph), nameof(SymbolTable), nameof(DefinitelyAssignedOutcomesAtChapters))]
    public bool IsValid => MainFlowGraph is not null && SymbolTable is not null && DefinitelyAssignedOutcomesAtChapters is not null;

    public required FlowGraph? MainFlowGraph { get; init; }

    public required SymbolTable? SymbolTable { get; init; }

    public required ImmutableDictionary<long, IEnumerable<OutcomeSymbol>>? DefinitelyAssignedOutcomesAtChapters { get; init; }

    public void Deconstruct(out FlowGraph? mainFlowGraph, out SymbolTable? symbolTable)
    {
        mainFlowGraph = MainFlowGraph;
        symbolTable = SymbolTable;
    }
}
