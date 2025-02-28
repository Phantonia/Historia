using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language.FlowAnalysis;

public sealed partial class FlowAnalyzer
{
    private (FlowGraph, SymbolTable) MergeFlowGraphs(IEnumerable<SubroutineSymbol> topologicalOrder, IReadOnlyDictionary<SubroutineSymbol, FlowGraph> subroutineFlowGraphs, IReadOnlyDictionary<SubroutineSymbol, int> referenceCounts)
    {
        Debug.Assert(topologicalOrder.Any() && topologicalOrder.First().Name == "main");

        FlowGraph mainFlowGraph = subroutineFlowGraphs[topologicalOrder.First()];

        foreach (SubroutineSymbol subroutine in topologicalOrder.Skip(1))
        {
            if (!referenceCounts.TryGetValue(subroutine, out int refCount) || refCount == 0)
            {
                continue;
            }

            if (refCount == 1)
            {
                mainFlowGraph = EmbedSingleReferenceSubroutine(mainFlowGraph, subroutineFlowGraphs[subroutine], subroutine);
            }
            else
            {
                CallerTrackerSymbol tracker = new()
                {
                    CalledSubroutine = subroutine,
                    Name = $"${subroutine.Name}", // unspeakable name
                    CallSiteCount = refCount,
                    // the indices are the literal character indices in the source code
                    // and since a subroutine declaration is at least scene A{} or chapter A{}, one more than its index is not taken
                    Index = subroutine.Index + 1,
                };

                symbolTable = symbolTable.Declare(tracker);

                mainFlowGraph = EmbedMultiReferenceSubroutine(mainFlowGraph, subroutineFlowGraphs[subroutine], subroutine, tracker);
            }
        }

        Debug.Assert(mainFlowGraph.Vertices.Values.All(v => !v.IsStory || v.AssociatedStatement is not BoundCallStatementNode));

        return (mainFlowGraph, symbolTable);
    }

    private static FlowGraph EmbedSingleReferenceSubroutine(FlowGraph mainFlowGraph, FlowGraph subroutineFlowGraph, SubroutineSymbol subroutine)
    {
        mainFlowGraph = AddSubroutineVertices(mainFlowGraph, subroutineFlowGraph);

        (long callVertex, long nextVertex) = FindSingleCallSite(mainFlowGraph, subroutine);

        mainFlowGraph = RedirectEdgesToCallVertexToSubroutineStart(mainFlowGraph, callVertex, subroutineFlowGraph);
        mainFlowGraph = RedirectFinalEdgesFromSubroutineToNextVertex(mainFlowGraph, subroutineFlowGraph, nextVertex);

        mainFlowGraph = mainFlowGraph.RemoveVertex(callVertex);

        return mainFlowGraph;
    }
    private static FlowGraph EmbedMultiReferenceSubroutine(FlowGraph mainFlowGraph, FlowGraph subroutineFlowGraph, SubroutineSymbol subroutine, CallerTrackerSymbol tracker)
    {
        mainFlowGraph = AddSubroutineVertices(mainFlowGraph, subroutineFlowGraph);

        (mainFlowGraph, List<long> callSites, Dictionary<long, long> nextVertices) = RedirectAndTrackCallSites(mainFlowGraph, subroutine, subroutineFlowGraph, tracker);
        (mainFlowGraph, CallerResolutionStatementNode resolution) = SynthesizeResolutionStatement(mainFlowGraph, subroutine, tracker, callSites, nextVertices);
        mainFlowGraph = RedirectFinalVerticesInSubroutine(mainFlowGraph, subroutineFlowGraph, resolution);

        return mainFlowGraph;
    }

    private static FlowGraph AddSubroutineVertices(FlowGraph mainFlowGraph, FlowGraph subroutineFlowGraph)
    {
        foreach (long vertex in subroutineFlowGraph.Vertices.Keys)
        {
            mainFlowGraph = mainFlowGraph.AddVertex(subroutineFlowGraph.Vertices[vertex], subroutineFlowGraph.OutgoingEdges[vertex]);
        }

        return mainFlowGraph;
    }

    private static (long callVertex, long nextVertex) FindSingleCallSite(FlowGraph mainFlowGraph, SubroutineSymbol subroutine)
    {
        long? callVertex = null;
        long? nextVertex = null;

        foreach (FlowVertex vertex in mainFlowGraph.Vertices.Values)
        {
            if (!vertex.IsStory || (vertex.AssociatedStatement is not BoundCallStatementNode { Subroutine: SubroutineSymbol calledSubroutine } || calledSubroutine != subroutine))
            {
                continue;
            }

            callVertex = vertex.Index;

            Debug.Assert(mainFlowGraph.OutgoingEdges[vertex.Index].Where(e => e.IsStory).Count() == 1); // assert vertex is infact linear
            nextVertex = mainFlowGraph.OutgoingEdges[vertex.Index].First(e => e.IsStory).ToVertex;
            break;
        }

        Debug.Assert(callVertex is not null);
        Debug.Assert(nextVertex is not null);

        return ((long)callVertex, (long)nextVertex);
    }

    private static FlowGraph RedirectEdgesToCallVertexToSubroutineStart(FlowGraph mainFlowGraph, long callVertex, FlowGraph subroutineFlowGraph)
    {
        // for all vertices V s.t. (V -> callVertex) instead let (V -> subroutineFlowGraph.StartVertex)

        if (mainFlowGraph.StartEdges.Any(e => e.ToVertex == callVertex))
        {
            List<FlowEdge> newStartEdges = [.. mainFlowGraph.StartEdges];
            newStartEdges.RemoveAll(e => e.ToVertex == callVertex);
            newStartEdges.AddRange(subroutineFlowGraph.StartEdges);

            mainFlowGraph = mainFlowGraph with
            {
                StartEdges = [.. newStartEdges],
            };
        }

        foreach (FlowVertex vertex in mainFlowGraph.Vertices.Values)
        {
            if (mainFlowGraph.OutgoingEdges[vertex.Index].Any(e => e.ToVertex == callVertex))
            {
                mainFlowGraph = mainFlowGraph.RemoveEdge(vertex.Index, callVertex)
                                             .AddEdges(vertex.Index, subroutineFlowGraph.StartEdges);

                if (mainFlowGraph.Vertices[vertex.Index].AssociatedStatement is FlowBranchingStatementNode branchingStatement)
                {
                    int edgeIndex = -1;

                    for (int i = 0; i < branchingStatement.OutgoingEdges.Count; i++)
                    {
                        if (branchingStatement.OutgoingEdges[i].ToVertex == callVertex)
                        {
                            edgeIndex = i;
                            break;
                        }
                    }

                    Debug.Assert(edgeIndex >= 0);
                    Debug.Assert(subroutineFlowGraph.IsConformable);

                    branchingStatement = branchingStatement with
                    {
                        OutgoingEdges = branchingStatement.OutgoingEdges.SetItem(edgeIndex, subroutineFlowGraph.StartEdges.Single(e => e.IsStory)),
                    };

                    mainFlowGraph = mainFlowGraph.SetVertex(vertex.Index, mainFlowGraph.Vertices[vertex.Index] with
                    {
                        AssociatedStatement = branchingStatement,
                    });
                }
            }
        }

        return mainFlowGraph;
    }

    private static FlowGraph RedirectFinalEdgesFromSubroutineToNextVertex(FlowGraph mainFlowGraph, FlowGraph subroutineFlowGraph, long nextVertex)
    {
        // for all vertices V s.t. V is in subroutineFlowGraph and V points to the empty vertex, remove edge to empty vertex and instead let (V -> N)

        foreach (FlowVertex vertex in subroutineFlowGraph.Vertices.Values)
        {
            if (subroutineFlowGraph.OutgoingEdges[vertex.Index].Contains(FlowGraph.FinalEdge))
            {
                mainFlowGraph = mainFlowGraph.RemoveEdge(vertex.Index, FlowGraph.FinalVertex)
                                             .AddEdge(vertex.Index, FlowEdge.CreateStrongTo(nextVertex));

                if (vertex.AssociatedStatement is FlowBranchingStatementNode { Original: LoopSwitchStatementNode } flowBranchingStatement)
                {
                    flowBranchingStatement = flowBranchingStatement with
                    {
                        OutgoingEdges = flowBranchingStatement.OutgoingEdges
                                                              .RemoveAt(flowBranchingStatement.OutgoingEdges.Count - 1)
                                                              .Add(FlowEdge.CreateStrongTo(nextVertex)),
                    };

                    mainFlowGraph = mainFlowGraph.SetVertex(vertex.Index, vertex with
                    {
                        AssociatedStatement = flowBranchingStatement,
                    });
                }
            }
        }

        return mainFlowGraph;
    }

    private static (FlowGraph mainFlowGraph, List<long> callSites, Dictionary<long, long> nextVertices) RedirectAndTrackCallSites(FlowGraph mainFlowGraph, SubroutineSymbol subroutine, FlowGraph subroutineFlowGraph, CallerTrackerSymbol tracker)
    {
        Dictionary<long, long> nextVertices = [];
        List<long> callSites = [];

        foreach (FlowVertex vertex in mainFlowGraph.Vertices.Values)
        {
            if (vertex.AssociatedStatement is not BoundCallStatementNode { Subroutine: SubroutineSymbol calledSubroutine } || calledSubroutine != subroutine)
            {
                continue;
            }

            Debug.Assert(mainFlowGraph.OutgoingEdges[vertex.Index].Count == 1); // assert vertex is infact linear
            nextVertices[vertex.Index] = mainFlowGraph.OutgoingEdges[vertex.Index][0].ToVertex;

            FlowVertex trackerVertex = vertex with
            {
                AssociatedStatement = new CallerTrackerStatementNode
                {
                    CallSiteIndex = callSites.Count,
                    Index = vertex.Index,
                    Tracker = tracker,
                    PrecedingTokens = [],
                },
                Kind = FlowVertexKind.Invisible,
            };

            callSites.Add(vertex.Index);

            mainFlowGraph = mainFlowGraph.SetVertex(vertex.Index, trackerVertex)
                                         .SetEdges(vertex.Index, subroutineFlowGraph.StartEdges);

            // flow branching statements are fine because they used to point to the call statement
            // but since the index of the tracker statement is the same as of the call statement
            // nothing needs to be changed
        }

        return (mainFlowGraph, callSites, nextVertices);
    }

    private static (FlowGraph mainFlowGraph, CallerResolutionStatementNode resolutionStatement) SynthesizeResolutionStatement(FlowGraph mainFlowGraph, SubroutineSymbol subroutine, CallerTrackerSymbol tracker, List<long> callSites, Dictionary<long, long> nextVertices)
    {
        CallerResolutionStatementNode resolution = new()
        {
            Tracker = tracker,
            Index = subroutine.Index + 2,
            PrecedingTokens = [],
        };

        FlowVertex resolutionVertex = new()
        {
            AssociatedStatement = resolution,
            Index = resolution.Index,
            Kind = FlowVertexKind.Invisible,
        };

        List<FlowEdge> edgesBuilder = [];

        foreach (long site in callSites)
        {
            edgesBuilder.Add(FlowEdge.CreateStrongTo(nextVertices[site]));
        }

        mainFlowGraph = mainFlowGraph with
        {
            Vertices = mainFlowGraph.Vertices.Add(resolutionVertex.Index, resolutionVertex),
            OutgoingEdges = mainFlowGraph.OutgoingEdges.Add(resolutionVertex.Index, edgesBuilder.ToImmutableList()),
        };

        return (mainFlowGraph, resolution);
    }

    private static FlowGraph RedirectFinalVerticesInSubroutine(FlowGraph mainFlowGraph, FlowGraph subroutineFlowGraph, CallerResolutionStatementNode resolution)
    {
        // for all vertices V s.t. V is in subroutine flow graph and V -> empty vertex, make V instead point to resolutionVertex

        foreach (FlowVertex vertex in subroutineFlowGraph.Vertices.Values)
        {
            if (subroutineFlowGraph.OutgoingEdges[vertex.Index].Contains(FlowGraph.FinalEdge))
            {
                mainFlowGraph = mainFlowGraph.RemoveEdge(vertex.Index, FlowGraph.FinalVertex).AddEdge(vertex.Index, FlowEdge.CreateStrongTo(resolution.Index));
            }
        }

        return mainFlowGraph;
    }
}
