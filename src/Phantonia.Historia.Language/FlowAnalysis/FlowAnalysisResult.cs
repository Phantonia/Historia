using Phantonia.Historia.Language.SemanticAnalysis;
using System.Diagnostics.CodeAnalysis;

namespace Phantonia.Historia.Language.FlowAnalysis;

public readonly record struct FlowAnalysisResult
{
    [MemberNotNullWhen(returnValue: true, nameof(MainFlowGraph), nameof(SymbolTable))]
    public bool IsValid => MainFlowGraph is not null && SymbolTable is not null;

    public required FlowGraph? MainFlowGraph { get; init; }

    public required SymbolTable? SymbolTable { get; init; }

    public void Deconstruct(out FlowGraph? mainFlowGraph, out SymbolTable? symbolTable)
    {
        mainFlowGraph = MainFlowGraph;
        symbolTable = SymbolTable;
    }
}
