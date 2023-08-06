﻿using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.FlowAnalysis;

public sealed partial class FlowAnalyzer
{
    private void PerformReachabilityAnalysis(FlowGraph mainFlowGraph)
    {
        FlowGraph dual = mainFlowGraph.Reverse();
        IEnumerable<int> order = mainFlowGraph.TopologicalSort();

        if (!order.Any())
        {
            return;
        }

        VertexData defaultVertexData = InductiveStart(dual, order.First());

        Dictionary<int, VertexData> data = new()
        {
            [order.First()] = defaultVertexData,
        };

        foreach (int vertex in order.Skip(1))
        {
            data[vertex] = InductiveStep(dual, vertex, data, defaultVertexData);
        }
    }

    private VertexData InductiveStart(FlowGraph dualFlowGraph, int startVertex)
    {
        ImmutableDictionary<OutcomeSymbol, OutcomeData>.Builder outcomes = ImmutableDictionary.CreateBuilder<OutcomeSymbol, OutcomeData>();

        foreach (Symbol symbol in symbolTable.AllSymbols)
        {
            switch (symbol)
            {
                case OutcomeSymbol outcomeSymbol:
                    if (outcomeSymbol.DefaultOption is not null)
                    {
                        outcomes.Add(outcomeSymbol, new OutcomeData
                        {
                            DefinitelyAssigned = true,
                            MightBeAssigned = false,
                        });
                    }
                    else
                    {
                        outcomes.Add(outcomeSymbol, new OutcomeData());
                    }

                    break;
            }
        }

        if (dualFlowGraph.Vertices[startVertex].AssociatedStatement is BoundOutcomeAssignmentStatementNode outcomeAssignment)
        {
            OutcomeData data = outcomes[outcomeAssignment.Outcome];
            data = data with
            {
                DefinitelyAssigned = true,
                MightBeAssigned = true,
            };
            outcomes[outcomeAssignment.Outcome] = data;
        }

        return new VertexData
        {
            Outcomes = outcomes.ToImmutable(),
        };
    }

    private VertexData InductiveStep(FlowGraph dualFlowGraph, int vertex, IReadOnlyDictionary<int, VertexData> previousData, VertexData defaultVertexData)
    {
        VertexData thisVertexData = defaultVertexData;

        foreach ((OutcomeSymbol outcome, OutcomeData data) in defaultVertexData.Outcomes)
        {
            bool definitelyAssigned = true;
            bool mightBeAssigned = false;

            foreach (int pointingVertex in dualFlowGraph.OutgoingEdges[vertex])
            {
                if (!previousData[pointingVertex].Outcomes[outcome].DefinitelyAssigned)
                {
                    definitelyAssigned = false;
                }

                if (previousData[pointingVertex].Outcomes[outcome].MightBeAssigned)
                {
                    mightBeAssigned = true;
                }
            }

            StatementNode statement = dualFlowGraph.Vertices[vertex].AssociatedStatement;

            switch (statement)
            {
                case BoundOutcomeAssignmentStatementNode boundAssignment when boundAssignment.Outcome == outcome:
                    if (mightBeAssigned)
                    {
                        ErrorFound?.Invoke(Errors.OutcomeMayBeAssignedMoreThanOnce(outcome.Name, boundAssignment.Index));
                    }

                    definitelyAssigned = true;
                    mightBeAssigned = true;
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
                    MightBeAssigned = mightBeAssigned
                }),
            };
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

        public bool MightBeAssigned { get; init; }
    }
}
