﻿using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language.FlowAnalysis;

public sealed partial class FlowAnalyzer
{
    private void PerformReachabilityAnalysis(IReadOnlyDictionary<SubroutineSymbol, FlowGraph> subroutineFlowGraphs, out ImmutableDictionary<long, IEnumerable<OutcomeSymbol>> definitelyAssignedOutcomesAtChapters)
    {
        SubroutineSymbol mainSubroutine = (SubroutineSymbol)symbolTable["main"];
        VertexData defaultVertexData = GetDefaultData();
        VertexData finalVertexData = ProcessSubroutine(subroutineFlowGraphs[mainSubroutine], defaultVertexData, subroutineFlowGraphs, [mainSubroutine]);
        definitelyAssignedOutcomesAtChapters = finalVertexData.DefinitelyAssignedOutcomesAtChapters;
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
                            IsDefinitelyAssigned = true,
                            IsPossiblyAssigned = true,
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
            DefinitelyAssignedOutcomesAtChapters = ImmutableDictionary<long, IEnumerable<OutcomeSymbol>>.Empty,
        };
    }

    private VertexData ProcessSubroutine(FlowGraph subroutineFlowGraph,
                                    VertexData defaultVertexData,
                                    IReadOnlyDictionary<SubroutineSymbol, FlowGraph> subroutineFlowGraphs,
                                    ImmutableStack<SubroutineSymbol> callStack)
    {
        FlowGraph reversedFlowGraph = subroutineFlowGraph.Reverse();
        IEnumerable<long> order = subroutineFlowGraph.TopologicalSort();

        if (!order.Any())
        {
            return defaultVertexData;
        }

        Dictionary<long, VertexData> data = [];

        long firstVertex = order.First();
        data[firstVertex] = ProcessVertex(subroutineFlowGraph, firstVertex, previousData: [defaultVertexData], defaultVertexData, subroutineFlowGraphs, callStack);

        foreach (long vertex in order.Skip(1))
        {
            IEnumerable<VertexData> previousData = reversedFlowGraph.OutgoingEdges[vertex]
                                                                    .Where(e => e.IsSemantic)
                                                                    .Select(e => data[e.ToVertex]);
            data[vertex] = ProcessVertex(subroutineFlowGraph, vertex, previousData, defaultVertexData, subroutineFlowGraphs, callStack);
        }

        IEnumerable<VertexData> finalVertexData =
            data.Where(p => subroutineFlowGraph.OutgoingEdges[p.Key].Contains(FlowGraph.FinalEdge))
                .Select(p => p.Value);

        return ProcessVertex(subroutineFlowGraph, FlowGraph.FinalVertex, finalVertexData, defaultVertexData, subroutineFlowGraphs, callStack);
    }

    private VertexData ProcessVertex(FlowGraph flowGraph,
                                     long vertex,
                                     IEnumerable<VertexData> previousData,
                                     VertexData defaultVertexData,
                                     IReadOnlyDictionary<SubroutineSymbol, FlowGraph> subroutineFlowGraphs,
                                     ImmutableStack<SubroutineSymbol> callStack)
    {
        VertexData thisVertexData = defaultVertexData;

        StatementNode? statement = vertex == FlowGraph.FinalVertex ? null : flowGraph.Vertices[vertex].AssociatedStatement;

        foreach ((OutcomeSymbol outcome, OutcomeData outcomeData) in defaultVertexData.Outcomes)
        {
            thisVertexData = ProcessOutcome(flowGraph, vertex, previousData, callStack, thisVertexData, statement, outcome);
        }

        thisVertexData = thisVertexData with
        {
            DefinitelyAssignedOutcomesAtChapters = previousData.First().DefinitelyAssignedOutcomesAtChapters,
        };

        foreach (VertexData previousVertexData in previousData.Skip(1))
        {
            thisVertexData = thisVertexData with
            {
                DefinitelyAssignedOutcomesAtChapters = thisVertexData.DefinitelyAssignedOutcomesAtChapters.AddRange(previousVertexData.DefinitelyAssignedOutcomesAtChapters),
            };
        }

        if (statement is BoundCallStatementNode { Subroutine: SubroutineSymbol calledSubroutine })
        {
            if (calledSubroutine.IsChapter)
            {
                thisVertexData = PerformLocking(calledSubroutine, thisVertexData);
            }

            // this processes a subroutine as often as it is called
            // we just assume that is not a lot
            // it is necessary to process it as 'thisVertexData' might differ a lot for different callsites
            // we might optimize this to process local outcomes only once and only process global outcomes multiple times
            return ProcessSubroutine(subroutineFlowGraphs[calledSubroutine], thisVertexData, subroutineFlowGraphs, callStack.Push(calledSubroutine));
        }

        return thisVertexData;
    }

    private VertexData ProcessOutcome(
        FlowGraph flowGraph,
        long vertex,
        IEnumerable<VertexData> previousData,
        ImmutableStack<SubroutineSymbol> callStack,
        VertexData thisVertexData,
        StatementNode? statement,
        OutcomeSymbol outcome)
    {
        bool definitelyAssigned = true;
        bool possiblyAssigned = false;
        bool locked = false;

        foreach (VertexData data in previousData)
        {
            if (!data.Outcomes[outcome].IsDefinitelyAssigned)
            {
                definitelyAssigned = false;
            }

            if (data.Outcomes[outcome].IsPossiblyAssigned)
            {
                possiblyAssigned = true;
            }

            if (data.Outcomes[outcome].IsLocked)
            {
                locked = true;
            }
        }

        void ErrorOnUnassigned(long index)
        {
            if (!definitelyAssigned && outcome.DefaultOption is null && flowGraph.Vertices[vertex].IsStory)
            {
                if (outcome is SpectrumSymbol)
                {
                    ErrorFound?.Invoke(Errors.SpectrumNotDefinitelyAssigned(outcome.Name, callStack.Select(s => s.Name), index));
                }
                else
                {
                    ErrorFound?.Invoke(Errors.OutcomeNotDefinitelyAssigned(outcome.Name, callStack.Select(s => s.Name), index));
                }
            }
        }

        void ErrorOnLocked(long index)
        {
            if (locked)
            {
                if (outcome is SpectrumSymbol)
                {
                    ErrorFound?.Invoke(Errors.SpectrumIsLocked(outcome.Name, callStack.Select(s => s.Name), index));
                }
                else
                {
                    ErrorFound?.Invoke(Errors.OutcomeIsLocked(outcome.Name, callStack.Select(s => s.Name), index));
                }
            }
        }

        switch (statement)
        {
            case BoundOutcomeAssignmentStatementNode boundAssignment when boundAssignment.Outcome == outcome:
                if (possiblyAssigned && flowGraph.Vertices[vertex].IsStory)
                {
                    ErrorFound?.Invoke(Errors.OutcomeMightBeAssignedMoreThanOnce(outcome.Name, callStack.Select(s => s.Name), boundAssignment.Index));
                }

                ErrorOnLocked(boundAssignment.Index);

                definitelyAssigned = true;
                possiblyAssigned = true;
                break;
            case BoundSpectrumAdjustmentStatementNode boundAdjustment when boundAdjustment.Spectrum == outcome:
                ErrorOnLocked(boundAdjustment.Index);
                definitelyAssigned = true;
                break;
            case FlowBranchingStatementNode { Original: BoundBranchOnStatementNode boundBranchOn } when boundBranchOn.Outcome == outcome:
                ErrorOnUnassigned(boundBranchOn.Index);
                ErrorOnLocked(boundBranchOn.Index);
                break;
            case FlowBranchingStatementNode { Original: IfStatementNode ifStatement }:
                void ProcessExpression(ExpressionNode expression)
                {
                    if (expression is not TypedExpressionNode { Original: ExpressionNode untypedExpression })
                    {
                        Debug.Assert(false);
                        return;
                    }

                    switch (untypedExpression)
                    {
                        case NotExpressionNode { InnerExpression: ExpressionNode innerExpression }:
                            ProcessExpression(innerExpression);
                            break;
                        case LogicExpressionNode { LeftExpression: ExpressionNode leftHandSide, RightExpression: ExpressionNode rightHandSide }:
                            ProcessExpression(leftHandSide);
                            ProcessExpression(rightHandSide);
                            break;
                        case BoundIsExpressionNode { Outcome: OutcomeSymbol thisOutcome, Index: long index } when thisOutcome == outcome:
                            ErrorOnUnassigned(index);
                            ErrorOnLocked(index);
                            break;
                    }
                }

                ProcessExpression(ifStatement.Condition);
                break;
        }

        thisVertexData = thisVertexData with
        {
            Outcomes = thisVertexData.Outcomes.SetItem(outcome, new OutcomeData
            {
                // this was here for a reason but it's wrong
                // investigate further
                IsDefinitelyAssigned = /*(vertex == FlowGraph.FinalVertex || flowGraph.Vertices[vertex].IsStory) &&*/ definitelyAssigned,
                IsPossiblyAssigned = possiblyAssigned,
                IsLocked = locked,
            }),
        };
        return thisVertexData;
    }

    private static VertexData PerformLocking(SubroutineSymbol chapter, VertexData thisVertexData)
    {
        thisVertexData = thisVertexData with
        {
            DefinitelyAssignedOutcomesAtChapters
                            = thisVertexData.DefinitelyAssignedOutcomesAtChapters
                                            .SetItem(chapter.Index, thisVertexData.Outcomes.Keys.Where(o => thisVertexData.Outcomes[o].IsDefinitelyAssigned)),
        };

        foreach (OutcomeSymbol definitelyAssignedOutcome in thisVertexData.DefinitelyAssignedOutcomesAtChapters[chapter.Index])
        {
            if (definitelyAssignedOutcome.IsPublic)
            {
                continue;
            }

            OutcomeData updatedOutcomeData = thisVertexData.Outcomes[definitelyAssignedOutcome] with
            {
                IsLocked = true,
            };

            ImmutableDictionary<OutcomeSymbol, OutcomeData> updatedOutcomeTable
                = thisVertexData.Outcomes.SetItem(definitelyAssignedOutcome, updatedOutcomeData);

            thisVertexData = thisVertexData with
            {
                Outcomes = updatedOutcomeTable,
            };
        }

        return thisVertexData;
    }

    private readonly record struct VertexData
    {
        public ImmutableDictionary<OutcomeSymbol, OutcomeData> Outcomes { get; init; }

        public ImmutableDictionary<long, IEnumerable<OutcomeSymbol>> DefinitelyAssignedOutcomesAtChapters { get; init; }
    }

    private readonly record struct OutcomeData
    {
        public bool IsDefinitelyAssigned { get; init; }

        public bool IsPossiblyAssigned { get; init; }

        public bool IsLocked { get; init; }
    }
}
