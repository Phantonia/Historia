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
        Dictionary<SceneSymbol, FlowGraph> sceneFlowGraphs = [];

        foreach (TopLevelNode symbolDeclaration in story.TopLevelNodes)
        {
            if (symbolDeclaration is BoundSymbolDeclarationNode
                {
                    Declaration: SceneSymbolDeclarationNode
                    {
                        Body: StatementBodyNode body,
                    },
                    Symbol: SceneSymbol scene,
                })
            {
                FlowGraph sceneFlowGraph = GenerateBodyFlowGraph(body);
                sceneFlowGraphs[scene] = sceneFlowGraph;
            }
        }

        (IEnumerable<SceneSymbol>? topologicalOrder, IReadOnlyDictionary<SceneSymbol, int> referenceCounts) = PerformDependencyAnalysis(sceneFlowGraphs);

        if (topologicalOrder is null)
        {
            return new FlowAnalysisResult
            {
                MainFlowGraph = null,
                SymbolTable = null,
                DefinitelyAssignedOutcomesAtCheckpoints = null,
            };
        }

        PerformReachabilityAnalysis(sceneFlowGraphs, out ImmutableDictionary<int, IEnumerable<OutcomeSymbol>> definitelyAssignedOutcomesAtCheckpoints);

        (FlowGraph mainFlowGraph, SymbolTable updatedSymbolTable) = MergeFlowGraphs(topologicalOrder, sceneFlowGraphs, referenceCounts);

        return new FlowAnalysisResult
        {
            MainFlowGraph = mainFlowGraph,
            SymbolTable = updatedSymbolTable,
            DefinitelyAssignedOutcomesAtCheckpoints = definitelyAssignedOutcomesAtCheckpoints,
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

        foreach (SwitchOptionNode option in switchStatement.Options)
        {
            FlowGraph nestedFlowGraph = GenerateBodyFlowGraph(option.Body);

            flowGraph = flowGraph.AppendToVertex(switchStatement.Index, nestedFlowGraph);
        }

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
            Index = loopSwitchStatement.Index - 1,
            AssociatedStatement = loopSwitchStatement,
            Kind = FlowVertexKind.PurelySemantic,
        });

        List<int> nonFinalEndVertices = [];
        List<int> finalStartVertices = [];

        foreach (LoopSwitchOptionNode option in loopSwitchStatement.Options)
        {
            FlowGraph nestedFlowGraph = GenerateBodyFlowGraph(option.Body);

            flowGraph = flowGraph.AppendToVertex(loopSwitchStatement.Index, nestedFlowGraph);

            FlowGraph nestedSemanticCopy = CreateSemanticCopy(nestedFlowGraph);
            semanticCopy = semanticCopy.AppendToVertex(loopSwitchStatement.Index - 1, nestedSemanticCopy);

            if (option.Kind != LoopSwitchOptionKind.Final)
            {
                // redirect final edges as weak edges back up
                foreach (int vertex in flowGraph.OutgoingEdges.Where(p => p.Value.Contains(FlowGraph.FinalEdge)).Select(p => p.Key))
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

        foreach (int nonFinalEndVertex in nonFinalEndVertices)
        {
            if (hasFinalOption)
            {
                // add purely semantic edge from all end vertices of non final options to all start vertices of final options
                foreach (int finalStartVertex in finalStartVertices)
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
        int startVertex = flowGraph.GetStoryStartVertex();

        flowGraph = semanticCopy.Append(flowGraph);
        flowGraph = flowGraph with
        {
            StartEdges = flowGraph.StartEdges.Add(FlowEdge.CreateWeakTo(startVertex)),
        };

        return flowGraph;
    }

    private static FlowGraph CreateSemanticCopy(FlowGraph flowGraph)
    {
        static FlowVertex SemantifyVertex(FlowVertex vertex) => vertex with
        {
            Kind = FlowVertexKind.PurelySemantic,
            Index = vertex.Index != FlowGraph.FinalVertex ? vertex.Index - 1 : FlowGraph.FinalVertex,
        };

        ImmutableDictionary<int, FlowVertex> vertices
            = flowGraph.Vertices.Select(p => KeyValuePair.Create(p.Key - 1, SemantifyVertex(p.Value)))
                                .ToImmutableDictionary();

        static FlowEdge SemantifyEdge(FlowEdge edge)
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
                    ToVertex = edge.ToVertex != FlowGraph.FinalVertex ? edge.ToVertex - 1 : FlowGraph.FinalVertex,
                };
            }
        }

        ImmutableDictionary<int, ImmutableList<FlowEdge>> edges
            = flowGraph.OutgoingEdges.Select(p => KeyValuePair.Create(p.Key - 1, p.Value.Select(e => SemantifyEdge(e)).ToImmutableList()))
                                     .ToImmutableDictionary();

        IEnumerable<FlowEdge> newStartEdges = flowGraph.StartEdges.Select(e => e with { ToVertex = e.ToVertex != FlowGraph.FinalVertex ? e.ToVertex - 1 : FlowGraph.FinalVertex });

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

        return flowGraph;
    }
}
