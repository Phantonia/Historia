using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.FlowAnalysis;

// there is no abbreviation for this thing
// always use its full name, i'm serious
public sealed partial class FlowAnalyzer
{
    public FlowAnalyzer(StoryNode story, SymbolTable symbolTable)
    {
        this.story = story;
        this.symbolTable = symbolTable;
    }

    private readonly StoryNode story;
    private readonly SymbolTable symbolTable;

    public event Action<Error>? ErrorFound;

    public FlowAnalysisResult PerformFlowAnalysis()
    {
        Dictionary<SceneSymbol, FlowGraph> sceneFlowGraphs = new();

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
            };
        }

        PerformReachabilityAnalysis(sceneFlowGraphs);

        (FlowGraph mainFlowGraph, SymbolTable updatedSymbolTable) = MergeFlowGraphs(topologicalOrder, sceneFlowGraphs, referenceCounts);

        return new FlowAnalysisResult
        {
            MainFlowGraph = mainFlowGraph,
            SymbolTable = updatedSymbolTable,
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
            OutputStatementNode => FlowGraph.CreateSimpleFlowGraph(new FlowVertex
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
            AssignmentStatementNode => FlowGraph.CreateSimpleFlowGraph(new FlowVertex
            {
                Index = statement.Index,
                AssociatedStatement = statement,
                Kind = FlowVertexKind.Invisible,
            }),
            SpectrumAdjustmentStatementNode => FlowGraph.CreateSimpleFlowGraph(new FlowVertex
            {
                Index = statement.Index,
                AssociatedStatement = statement,
                Kind = FlowVertexKind.Invisible,
            }),
            CallStatementNode => FlowGraph.CreateSimpleFlowGraph(new FlowVertex
            {
                Index = statement.Index,
                AssociatedStatement = statement,
                Kind = FlowVertexKind.Visible,
            }),
            _ => throw new NotImplementedException($"Unknown statement type {statement.GetType().FullName}"),
        };
    }

    private FlowGraph GenerateSwitchFlowGraph(SwitchStatementNode switchStatement)
    {
        FlowGraph flowGraph = FlowGraph.Empty.AddVertex(new FlowVertex
        {
            Index = switchStatement.Index,
            AssociatedStatement = switchStatement,
            Kind = FlowVertexKind.Visible,
        });

        foreach (SwitchOptionNode option in switchStatement.Options)
        {
            FlowGraph nestedFlowGraph = GenerateBodyFlowGraph(option.Body);

            flowGraph = flowGraph.AppendToVertex(flowGraph.StartVertex, nestedFlowGraph);
        }

        return flowGraph;
    }

    private FlowGraph GenerateLoopSwitchFlowGraph(LoopSwitchStatementNode loopSwitchStatement)
    {
        bool hasFinalOption = loopSwitchStatement.Options.Any(o => o.Kind == LoopSwitchOptionKind.Final);

        FlowGraph flowGraph = FlowGraph.Empty.AddVertex(new FlowVertex
        {
            Index = loopSwitchStatement.Index,
            AssociatedStatement = loopSwitchStatement,
            Kind = FlowVertexKind.Visible,
        });

        FlowGraph semanticCopy = FlowGraph.Empty.AddVertex(new FlowVertex
        {
            Index = loopSwitchStatement.Index - 1,
            AssociatedStatement = loopSwitchStatement,
            Kind = FlowVertexKind.PurelySemantic,
        });

        List<int> nonFinalEndVertices = new();
        List<int> finalStartVertices = new();

        foreach (LoopSwitchOptionNode option in loopSwitchStatement.Options)
        {
            FlowGraph nestedFlowGraph = GenerateBodyFlowGraph(option.Body);

            flowGraph = flowGraph.AppendToVertex(flowGraph.StartVertex, nestedFlowGraph);

            if (option.Kind != LoopSwitchOptionKind.Final)
            {
                FlowGraph nestedSemanticCopy = CreateSemanticCopy(nestedFlowGraph);
                semanticCopy = semanticCopy.AppendToVertex(semanticCopy.StartVertex, nestedSemanticCopy);

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
                finalStartVertices.Add(nestedFlowGraph.StartVertex);
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

        flowGraph = semanticCopy.Append(flowGraph);

        return flowGraph;
    }

    private static FlowGraph CreateSemanticCopy(FlowGraph flowGraph)
    {
        static FlowVertex SemantifyVertex(FlowVertex vertex) => vertex with
        {
            Kind = FlowVertexKind.PurelySemantic,
            Index = vertex.Index - 1,
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
                    ToVertex = edge.ToVertex - 1,
                };
            }
        }

        ImmutableDictionary<int, ImmutableList<FlowEdge>> edges
            = flowGraph.OutgoingEdges.Select(p => KeyValuePair.Create(p.Key - 1, p.Value.Select(e => SemantifyEdge(e)).ToImmutableList()))
                                     .ToImmutableDictionary();

        return flowGraph with
        {
            StartVertex = flowGraph.StartVertex - 1,
            Vertices = vertices,
            OutgoingEdges = edges,
        };
    }

    private FlowGraph GenerateBranchOnFlowGraph(BranchOnStatementNode branchOnStatement)
    {
        FlowGraph flowGraph = FlowGraph.Empty.AddVertex(new FlowVertex
        {
            Index = branchOnStatement.Index,
            AssociatedStatement = branchOnStatement,
            Kind = FlowVertexKind.Invisible,
        });

        foreach (BranchOnOptionNode option in branchOnStatement.Options)
        {
            FlowGraph nestedFlowGraph = GenerateBodyFlowGraph(option.Body);

            flowGraph = flowGraph.AppendToVertex(flowGraph.StartVertex, nestedFlowGraph);
        }

        return flowGraph;
    }
}
