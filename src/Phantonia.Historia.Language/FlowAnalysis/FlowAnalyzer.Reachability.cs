using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.FlowAnalysis;

public sealed partial class FlowAnalyzer
{
    private void PerformReachabilityAnalysis(IReadOnlyDictionary<SceneSymbol, FlowGraph> sceneFlowGraphs)
    {
        SceneSymbol mainScene = (SceneSymbol)symbolTable["main"];
        VertexData defaultVertexData = GetDefaultData();
        _ = ProcessScene(sceneFlowGraphs[mainScene], defaultVertexData, sceneFlowGraphs);
    }

    private VertexData GetDefaultData()
    {
        ImmutableDictionary<OutcomeSymbol, OutcomeData>.Builder outcomes = ImmutableDictionary.CreateBuilder<OutcomeSymbol, OutcomeData>();

        foreach (Symbol symbol in symbolTable.AllSymbols)
        {
            switch (symbol)
            {
                case OutcomeSymbol outcomeSymbol:
                    if (outcomeSymbol.AlwaysAssigned)
                    {
                        outcomes.Add(outcomeSymbol, new OutcomeData
                        {
                            DefinitelyAssigned = true,
                            PossiblyAssigned = true,
                        });
                    }
                    else if (outcomeSymbol.DefaultOption is not null)
                    {
                        outcomes.Add(outcomeSymbol, new OutcomeData
                        {
                            DefinitelyAssigned = true,
                            PossiblyAssigned = false,
                        });
                    }
                    else
                    {
                        outcomes.Add(outcomeSymbol, new OutcomeData());
                    }

                    break;
            }
        }

        return new VertexData
        {
            Outcomes = outcomes.ToImmutable(),
        };
    }

    private VertexData ProcessScene(FlowGraph sceneFlowGraph, VertexData defaultVertexData, IReadOnlyDictionary<SceneSymbol, FlowGraph> sceneFlowGraphs)
    {
        FlowGraph reversedFlowGraph = sceneFlowGraph.Reverse();
        IEnumerable<int> order = sceneFlowGraph.TopologicalSort();

        if (!order.Any())
        {
            return defaultVertexData;
        }

        Dictionary<int, VertexData> data = new();

        int firstVertex = order.First();
        data[firstVertex] = ProcessVertex(sceneFlowGraph, firstVertex, previousData: new[] { defaultVertexData }, defaultVertexData, sceneFlowGraphs);

        foreach (int vertex in order.Skip(1))
        {
            data[vertex] = ProcessVertex(sceneFlowGraph, vertex, reversedFlowGraph.OutgoingEdges[vertex].Select(i => data[i]), defaultVertexData, sceneFlowGraphs);
        }

        IEnumerable<VertexData> finalVertexData =
            data.Where(p => sceneFlowGraph.OutgoingEdges[p.Key].Contains(FlowGraph.EmptyVertex))
                .Select(p => p.Value);

        return ProcessVertex(sceneFlowGraph, FlowGraph.EmptyVertex, finalVertexData, defaultVertexData, sceneFlowGraphs);
    }

    private VertexData ProcessVertex(FlowGraph flowGraph, int vertex, IEnumerable<VertexData> previousData, VertexData defaultVertexData, IReadOnlyDictionary<SceneSymbol, FlowGraph> sceneFlowGraphs)
    {
        VertexData thisVertexData = defaultVertexData;

        StatementNode? statement = vertex == FlowGraph.EmptyVertex ? null : flowGraph.Vertices[vertex].AssociatedStatement;

        foreach ((OutcomeSymbol outcome, OutcomeData outcomeData) in defaultVertexData.Outcomes)
        {
            bool definitelyAssigned = true;
            bool possiblyAssigned = false;

            foreach (VertexData data in previousData)
            {
                if (!data.Outcomes[outcome].DefinitelyAssigned)
                {
                    definitelyAssigned = false;
                }

                if (data.Outcomes[outcome].PossiblyAssigned)
                {
                    possiblyAssigned = true;
                }
            }

            switch (statement)
            {
                case BoundOutcomeAssignmentStatementNode boundAssignment when boundAssignment.Outcome == outcome:
                    if (possiblyAssigned)
                    {
                        ErrorFound?.Invoke(Errors.OutcomeMayBeAssignedMoreThanOnce(outcome.Name, boundAssignment.Index));
                    }

                    definitelyAssigned = true;
                    possiblyAssigned = true;
                    break;
                case BoundSpectrumAdjustmentStatementNode boundAdjustment when boundAdjustment.Spectrum == outcome:
                    definitelyAssigned = true;
                    break;
                case BoundBranchOnStatementNode boundBranchOn when boundBranchOn.Outcome == outcome:
                    if (!definitelyAssigned)
                    {
                        if (outcome is SpectrumSymbol)
                        {
                            ErrorFound?.Invoke(Errors.SpectrumNotDefinitelyAssigned(outcome.Name, boundBranchOn.Index));
                        }
                        else
                        {
                            ErrorFound?.Invoke(Errors.OutcomeNotDefinitelyAssigned(outcome.Name, boundBranchOn.Index));
                        }
                    }
                    break;
            }

            thisVertexData = thisVertexData with
            {
                Outcomes = thisVertexData.Outcomes.SetItem(outcome, new OutcomeData
                {
                    DefinitelyAssigned = definitelyAssigned,
                    PossiblyAssigned = possiblyAssigned,
                }),
            };
        }

        if (statement is BoundCallStatementNode { Scene: SceneSymbol calledScene })
        {
            // this processes a scene as often as it is called
            // we just assume that is not a lot
            // it is necessary to process it as 'thisVertexData' might differ a lot for different callsites
            // we might optimize this to process local outcomes only once and only process global outcomes multiple times
            return ProcessScene(sceneFlowGraphs[calledScene], thisVertexData, sceneFlowGraphs);
        }

        return thisVertexData;
    }

    private readonly record struct VertexData
    {
        public ImmutableDictionary<OutcomeSymbol, OutcomeData> Outcomes { get; init; }
    }

    private readonly record struct OutcomeData
    {
        public bool DefinitelyAssigned { get; init; }

        public bool PossiblyAssigned { get; init; }
    }
}
