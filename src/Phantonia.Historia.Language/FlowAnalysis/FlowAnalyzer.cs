using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language.FlowAnalysis;

// there is no abbreviation for this thing
// always use its full name, i'm serious
public sealed partial class FlowAnalyzer(StoryNode story, SymbolTable symbolTable)
{
    public event Action<Error>? ErrorFound;

    public FlowAnalysisResult PerformFlowAnalysis()
    {
        Dictionary<SubroutineSymbol, FlowGraph> subroutineFlowGraphs = [];

        foreach (TopLevelNode symbolDeclaration in story.GetTopLevelNodes())
        {
            if (symbolDeclaration is BoundSymbolDeclarationNode
                {
                    Original: SubroutineSymbolDeclarationNode
                    {
                        Body: StatementBodyNode body,
                    },
                    Symbol: SubroutineSymbol subroutine,
                })
            {
                FlowGraph subroutineFlowGraph = GenerateBodyFlowGraph(body);
                subroutineFlowGraphs[subroutine] = subroutineFlowGraph;
            }
        }

        (IEnumerable<SubroutineSymbol>? topologicalOrder, IReadOnlyDictionary<SubroutineSymbol, int> referenceCounts) = PerformDependencyAnalysis(subroutineFlowGraphs);

        if (topologicalOrder is null)
        {
            return new FlowAnalysisResult
            {
                MainFlowGraph = null,
                SymbolTable = null,
                DefinitelyAssignedOutcomesAtChapters = null,
            };
        }

        PerformReachabilityAnalysis(subroutineFlowGraphs, out ImmutableDictionary<long, IEnumerable<OutcomeSymbol>> definitelyAssignedOutcomesAtChapters);

        (FlowGraph mainFlowGraph, SymbolTable updatedSymbolTable) = MergeFlowGraphs(topologicalOrder, subroutineFlowGraphs, referenceCounts);

        return new FlowAnalysisResult
        {
            MainFlowGraph = mainFlowGraph,
            SymbolTable = updatedSymbolTable,
            DefinitelyAssignedOutcomesAtChapters = definitelyAssignedOutcomesAtChapters,
        };
    }

    private FlowGraph GenerateBodyFlowGraph(StatementBodyNode body)
    {
        FlowGraph graph = FlowGraph.Empty;

        foreach (StatementNode statement in body.Statements)
        {
            FlowGraph statementGraph = GenerateStatementFlowGraph(statement);
            graph = graph.Append(statementGraph);
        }

        graph = UpdateLoopSwitchLastEdges(graph);

        return graph;
    }

    private static FlowGraph UpdateLoopSwitchLastEdges(FlowGraph graph)
    {
        // the flow branching statement for loop switches always ends in a final edge because it is not updated in FlowGraph.Append
        // so we do that here

        foreach (FlowVertex vertex in graph.Vertices.Values)
        {
            if (vertex.AssociatedStatement is not FlowBranchingStatementNode { Original: LoopSwitchStatementNode, OutgoingEdges: ImmutableList<FlowEdge> outgoingEdges } flowBranchingStatement)
            {
                continue;
            }

            FlowEdge lastEdge = graph.OutgoingEdges[vertex.Index][^1];

            if (lastEdge != FlowGraph.FinalEdge)
            {
                graph = graph.SetVertex(vertex.Index, vertex with
                {
                    AssociatedStatement = flowBranchingStatement with
                    {
                        OutgoingEdges = [.. outgoingEdges.RemoveAt(outgoingEdges.Count - 1), lastEdge],
                    }
                });
            }
        }

        return graph;
    }

    private FlowGraph GenerateStatementFlowGraph(StatementNode statement)
    {
        return statement switch
        {
            OutputStatementNode or CallStatementNode => FlowGraph.CreateSimpleFlowGraph(new FlowVertex
            {
                Index = statement.Index,
                AssociatedStatement = statement,
                Kind = FlowVertexKind.Visible,
            }),
            SwitchStatementNode switchStatement => GenerateSwitchFlowGraph(switchStatement),
            LoopSwitchStatementNode loopSwitchStatement => GenerateLoopSwitchFlowGraph(loopSwitchStatement),
            BranchOnStatementNode branchOnStatement => GenerateBranchOnFlowGraph(branchOnStatement),
            OutcomeDeclarationStatementNode => FlowGraph.Empty,
            SpectrumDeclarationStatementNode => FlowGraph.Empty,
            AssignmentStatementNode or SpectrumAdjustmentStatementNode or BoundRunStatementNode => FlowGraph.CreateSimpleFlowGraph(new FlowVertex
            {
                Index = statement.Index,
                AssociatedStatement = statement,
                Kind = FlowVertexKind.Invisible,
            }),
            BoundChooseStatementNode chooseStatement => GenerateChooseFlowGraph(chooseStatement),
            IfStatementNode ifStatement => GenerateIfFlowGraph(ifStatement),
            NoOpStatementNode noopStatement => FlowGraph.CreateSimpleFlowGraph(new FlowVertex
            {
                Index = noopStatement.Index,
                AssociatedStatement = noopStatement,
                Kind = FlowVertexKind.Invisible,
            }),
            _ => throw new NotImplementedException($"Unknown statement type {statement.GetType().FullName}"),
        };
    }

    private FlowGraph GenerateSwitchFlowGraph(SwitchStatementNode switchStatement)
    {
        FlowGraph flowGraph = FlowGraph.CreateSimpleFlowGraph(new FlowVertex
        {
            Index = switchStatement.Index,
            AssociatedStatement = switchStatement,
            Kind = FlowVertexKind.Visible,
        });

        foreach (OptionNode option in switchStatement.Options)
        {
            FlowGraph nestedFlowGraph = GenerateBodyFlowGraph(option.Body);
            flowGraph = flowGraph.AppendToVertex(switchStatement.Index, nestedFlowGraph);
        }

        FlowBranchingStatementNode flowBranchingStatement = new()
        {
            Original = switchStatement,
            OutgoingEdges = flowGraph.OutgoingEdges[switchStatement.Index],
            Index = switchStatement.Index,
            PrecedingTokens = [],
        };

        flowGraph = flowGraph.SetVertex(switchStatement.Index, flowGraph.Vertices[switchStatement.Index] with
        {
            AssociatedStatement = flowBranchingStatement,
        });

        return flowGraph;
    }

    private FlowGraph GenerateLoopSwitchFlowGraph(LoopSwitchStatementNode loopSwitchStatement)
    {
        bool hasFinalOption = loopSwitchStatement.Options.Any(o => o.Kind == LoopSwitchOptionKind.Final);

        FlowGraph flowGraph = FlowGraph.CreateSimpleFlowGraph(new FlowVertex
        {
            Index = loopSwitchStatement.Index,
            AssociatedStatement = loopSwitchStatement,
            Kind = FlowVertexKind.Visible,
        });

        FlowGraph semanticCopy = FlowGraph.CreateSimpleSemanticFlowGraph(new FlowVertex
        {
            Index = CalculateNewIndex(loopSwitchStatement.Index, loopSwitchStatement.Index),
            AssociatedStatement = loopSwitchStatement,
            Kind = FlowVertexKind.PurelySemantic,
        });

        List<long> nonFinalEndVertices = [];
        List<long> finalStartVertices = [];

        foreach (LoopSwitchOptionNode option in loopSwitchStatement.Options)
        {
            FlowGraph nestedFlowGraph = GenerateBodyFlowGraph(option.Body);

            flowGraph = flowGraph.AppendToVertex(loopSwitchStatement.Index, nestedFlowGraph);

            FlowGraph nestedSemanticCopy = CreateSemanticCopy(nestedFlowGraph, uniqueId: loopSwitchStatement.Index);
            semanticCopy = semanticCopy.AppendToVertex(CalculateNewIndex(loopSwitchStatement.Index, loopSwitchStatement.Index), nestedSemanticCopy);

            if (option.Kind != LoopSwitchOptionKind.Final)
            {
                // redirect final edges as weak edges back up
                foreach (long vertex in nestedFlowGraph.OutgoingEdges.Where(p => p.Value.Contains(FlowGraph.FinalEdge)).Select(p => p.Key))
                {
                    flowGraph = flowGraph with
                    {
                        OutgoingEdges =
                            flowGraph.OutgoingEdges.SetItem(
                                vertex,
                                flowGraph.OutgoingEdges[vertex].Replace(
                                    FlowGraph.FinalEdge,
                                    FlowEdge.CreateWeakTo(loopSwitchStatement.Index))), // important: weak edge
                    };

                    nonFinalEndVertices.Add(vertex);
                }
            }
            else
            {
                foreach (FlowEdge edge in nestedFlowGraph.StartEdges)
                {
                    finalStartVertices.Add(edge.ToVertex);
                }
            }
        }

        foreach (long nonFinalEndVertex in nonFinalEndVertices)
        {
            if (hasFinalOption)
            {
                // add purely semantic edge from all end vertices of non final options to all start vertices of final options
                foreach (long finalStartVertex in finalStartVertices)
                {
                    flowGraph = flowGraph with
                    {
                        OutgoingEdges = flowGraph.OutgoingEdges.SetItem(
                            nonFinalEndVertex,
                            flowGraph.OutgoingEdges[nonFinalEndVertex].Add(
                                FlowEdge.CreatePurelySemanticTo(finalStartVertex))),
                    };
                }
            }
            else
            {
                // add purely semantic edges from all end vertices of non final options into nothingness
                flowGraph = flowGraph with
                {
                    OutgoingEdges = flowGraph.OutgoingEdges.SetItem(
                            nonFinalEndVertex,
                            flowGraph.OutgoingEdges[nonFinalEndVertex].Add(
                                FlowEdge.CreatePurelySemanticTo(FlowGraph.FinalVertex))),
                };
            }
        }

        // loop switches without final options automatically continue after all normal options have been selected
        if (!hasFinalOption)
        {
            flowGraph = flowGraph with
            {
                OutgoingEdges = flowGraph.OutgoingEdges.SetItem(
                    loopSwitchStatement.Index,
                    flowGraph.OutgoingEdges[loopSwitchStatement.Index].Add(
                        FlowGraph.FinalEdge)),
            };
        }

        Debug.Assert(flowGraph.IsConformable);
        long startVertex = flowGraph.GetStoryStartVertex();

        flowGraph = semanticCopy.Append(flowGraph);
        flowGraph = flowGraph with
        {
            StartEdges = flowGraph.StartEdges.Add(FlowEdge.CreateWeakTo(startVertex)),
        };

        FlowBranchingStatementNode flowBranchingStatement = new()
        {
            Original = loopSwitchStatement,
            OutgoingEdges = flowGraph.OutgoingEdges[loopSwitchStatement.Index],
            Index = loopSwitchStatement.Index,
            PrecedingTokens = [],
        };

        flowGraph = flowGraph.SetVertex(loopSwitchStatement.Index, flowGraph.Vertices[loopSwitchStatement.Index] with
        {
            AssociatedStatement = flowBranchingStatement,
        });

        return flowGraph;
    }

    private FlowGraph CreateSemanticCopy(FlowGraph flowGraph, long uniqueId)
    {
        FlowVertex SemantifyVertex(FlowVertex vertex) => vertex with
        {
            Kind = FlowVertexKind.PurelySemantic,
            Index = vertex.Index != FlowGraph.FinalVertex ? CalculateNewIndex(vertex.Index, uniqueId) : FlowGraph.FinalVertex,
        };

        ImmutableDictionary<long, FlowVertex> vertices
            = flowGraph.Vertices.Select(p => KeyValuePair.Create(CalculateNewIndex(p.Key, uniqueId), SemantifyVertex(p.Value)))
                                .ToImmutableDictionary();

        FlowEdge SemantifyEdge(FlowEdge edge)
        {
            FlowEdgeKind kind = edge.IsSemantic ? FlowEdgeKind.Semantic : FlowEdgeKind.None;

            if (edge.ToVertex == FlowGraph.FinalVertex)
            {
                return edge with
                {
                    Kind = kind,
                };
            }
            else
            {
                return edge with
                {
                    Kind = kind,
                    ToVertex = edge.ToVertex != FlowGraph.FinalVertex ? CalculateNewIndex(edge.ToVertex, uniqueId) : FlowGraph.FinalVertex,
                };
            }
        }

        ImmutableDictionary<long, ImmutableList<FlowEdge>> edges
            = flowGraph.OutgoingEdges.Select(p => KeyValuePair.Create(CalculateNewIndex(p.Key, uniqueId), p.Value.Select(e => SemantifyEdge(e)).ToImmutableList()))
                                     .ToImmutableDictionary();

        IEnumerable<FlowEdge> newStartEdges = flowGraph.StartEdges.Select(e => e with { ToVertex = e.ToVertex != FlowGraph.FinalVertex ? CalculateNewIndex(e.ToVertex, uniqueId) : FlowGraph.FinalVertex });
        
        return flowGraph with
        {
            StartEdges = [.. newStartEdges],
            Vertices = vertices,
            OutgoingEdges = edges,
        };
    }

    private FlowGraph GenerateBranchOnFlowGraph(BranchOnStatementNode branchOnStatement)
    {
        FlowGraph flowGraph = FlowGraph.CreateSimpleFlowGraph(new FlowVertex
        {
            Index = branchOnStatement.Index,
            AssociatedStatement = branchOnStatement,
            Kind = FlowVertexKind.Invisible,
        });

        foreach (BranchOnOptionNode option in branchOnStatement.Options)
        {
            FlowGraph nestedFlowGraph = GenerateBodyFlowGraph(option.Body);

            flowGraph = flowGraph.AppendToVertex(branchOnStatement.Index, nestedFlowGraph);
        }

        FlowBranchingStatementNode flowBranchingStatement = new()
        {
            Original = branchOnStatement,
            OutgoingEdges = flowGraph.OutgoingEdges[branchOnStatement.Index],
            Index = branchOnStatement.Index,
            PrecedingTokens = [],
        };

        flowGraph = flowGraph.SetVertex(branchOnStatement.Index, flowGraph.Vertices[branchOnStatement.Index] with
        {
            AssociatedStatement = flowBranchingStatement,
        });

        return flowGraph;
    }

    private FlowGraph GenerateChooseFlowGraph(BoundChooseStatementNode chooseStatement)
    {
        FlowGraph flowGraph = FlowGraph.CreateSimpleFlowGraph(new FlowVertex
        {
            Index = chooseStatement.Index,
            AssociatedStatement = chooseStatement,
            Kind = FlowVertexKind.Invisible,
        });

        foreach (OptionNode option in chooseStatement.Options)
        {
            FlowGraph nestedFlowGraph = GenerateBodyFlowGraph(option.Body);

            flowGraph = flowGraph.AppendToVertex(chooseStatement.Index, nestedFlowGraph);
        }

        FlowBranchingStatementNode flowBranchingStatement = new()
        {
            Original = chooseStatement,
            OutgoingEdges = flowGraph.OutgoingEdges[chooseStatement.Index],
            Index = chooseStatement.Index,
            PrecedingTokens = [],
        };

        flowGraph = flowGraph.SetVertex(chooseStatement.Index, flowGraph.Vertices[chooseStatement.Index] with
        {
            AssociatedStatement = flowBranchingStatement,
        });

        return flowGraph;
    }

    private FlowGraph GenerateIfFlowGraph(IfStatementNode ifStatement)
    {
        FlowGraph flowGraph = FlowGraph.CreateSimpleFlowGraph(new FlowVertex
        {
            Index = ifStatement.Index,
            AssociatedStatement = ifStatement,
            Kind = FlowVertexKind.Invisible,
        });

        FlowGraph thenFlowGraph = GenerateBodyFlowGraph(ifStatement.ThenBlock);

        flowGraph = flowGraph.Append(thenFlowGraph);

        if (ifStatement.ElseBlock is not null)
        {
            FlowGraph elseFlowGraph = GenerateBodyFlowGraph(ifStatement.ElseBlock);
            flowGraph = flowGraph.AppendToVertex(ifStatement.Index, elseFlowGraph);
        }
        else
        {
            // we need to draw an edge from the if statement vertex into nothingness
            flowGraph = flowGraph with
            {
                OutgoingEdges = flowGraph.OutgoingEdges.SetItem(ifStatement.Index, flowGraph.OutgoingEdges[ifStatement.Index].Add(FlowEdge.CreateStrongTo(FlowGraph.FinalVertex))),
            };
        }

        FlowBranchingStatementNode flowBranchingStatement = new()
        {
            Original = ifStatement,
            OutgoingEdges = flowGraph.OutgoingEdges[ifStatement.Index],
            Index = ifStatement.Index,
            PrecedingTokens = [],
        };

        flowGraph = flowGraph.SetVertex(ifStatement.Index, flowGraph.Vertices[ifStatement.Index] with
        {
            AssociatedStatement = flowBranchingStatement,
        });

        return flowGraph;
    }

    private long CalculateNewIndex(long index, long uniqueId) => uniqueId * story.Length + index;
}
