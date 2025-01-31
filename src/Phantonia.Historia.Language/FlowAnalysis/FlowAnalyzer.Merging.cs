using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language.FlowAnalysis;

public sealed partial class FlowAnalyzer
{
    private (FlowGraph, SymbolTable) MergeFlowGraphs(IEnumerable<SubroutineSymbol> topologicalOrder, IReadOnlyDictionary<SubroutineSymbol, FlowGraph> subroutineFlowGraphs, IReadOnlyDictionary<SubroutineSymbol, int> referenceCounts)
    {
        Debug.Assert(topologicalOrder.First().Name == "main");

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
        // 1. add all subroutine vertices
        foreach (long vertex in subroutineFlowGraph.Vertices.Keys)
        {
            mainFlowGraph = mainFlowGraph with
            {
                Vertices = mainFlowGraph.Vertices.Add(vertex, subroutineFlowGraph.Vertices[vertex]),
                OutgoingEdges = mainFlowGraph.OutgoingEdges.Add(vertex, subroutineFlowGraph.OutgoingEdges[vertex]),
            };
        }

        // 2. find callVertex and nextVertex (the single vertex that callVertex points to, since it's linear)
        long callVertex = int.MinValue;
        long nextVertex = int.MinValue;

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

        Debug.Assert(callVertex != int.MinValue);
        Debug.Assert(nextVertex != int.MinValue);

        // 3. for all vertices V s.t. (V -> callVertex) instead let (V -> subroutineFlowGraph.StartVertex)
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
                mainFlowGraph = mainFlowGraph with
                {
                    OutgoingEdges =
                        mainFlowGraph.OutgoingEdges.SetItem(
                            vertex.Index,
                            mainFlowGraph.OutgoingEdges[vertex.Index]
                                         .Remove(FlowEdge.CreateStrongTo(callVertex)) // there are no weak edges to call vertices (only weak edges back to loop switches)
                                         .AddRange(subroutineFlowGraph.StartEdges)),
                };

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

        // 4. for all vertices V s.t. V is in subroutineFlowGraph and V points to the empty vertex, remove edge to empty vertex and instead let (V -> N)
        foreach (FlowVertex vertex in subroutineFlowGraph.Vertices.Values)
        {
            if (subroutineFlowGraph.OutgoingEdges[vertex.Index].Contains(FlowGraph.FinalEdge))
            {
                mainFlowGraph = mainFlowGraph with
                {
                    OutgoingEdges =
                        mainFlowGraph.OutgoingEdges.SetItem(
                            vertex.Index,
                            mainFlowGraph.OutgoingEdges[vertex.Index]
                                         .Remove(FlowGraph.FinalEdge)
                                         .Add(FlowEdge.CreateStrongTo(nextVertex))),
                };

                if (vertex.AssociatedStatement is FlowBranchingStatementNode { Original: LoopSwitchStatementNode } flowBranchingStatement)
                {
                    flowBranchingStatement = flowBranchingStatement with
                    {
                        OutgoingEdges = flowBranchingStatement.OutgoingEdges.RemoveAt(flowBranchingStatement.OutgoingEdges.Count - 1).Add(FlowEdge.CreateStrongTo(nextVertex)),
                    };

                    mainFlowGraph = mainFlowGraph.SetVertex(vertex.Index, vertex with
                    {
                        AssociatedStatement = flowBranchingStatement,
                    });
                }
            }
        }

        mainFlowGraph = mainFlowGraph with
        {
            Vertices = mainFlowGraph.Vertices.Remove(callVertex),
            OutgoingEdges = mainFlowGraph.OutgoingEdges.Remove(callVertex),
        };

        return mainFlowGraph;
    }

    private static FlowGraph EmbedMultiReferenceSubroutine(FlowGraph mainFlowGraph, FlowGraph subroutineFlowGraph, SubroutineSymbol subroutine, CallerTrackerSymbol tracker)
    {
        // 1. add all subroutine vertices
        foreach (long vertex in subroutineFlowGraph.Vertices.Keys)
        {
            mainFlowGraph = mainFlowGraph with
            {
                Vertices = mainFlowGraph.Vertices.Add(vertex, subroutineFlowGraph.Vertices[vertex]),
                OutgoingEdges = mainFlowGraph.OutgoingEdges.Add(vertex, subroutineFlowGraph.OutgoingEdges[vertex]),
            };
        }

        // 2. find all callsites, replace them with tracker statements and redirect them correctly
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

            mainFlowGraph = mainFlowGraph with
            {
                Vertices = mainFlowGraph.Vertices.SetItem(vertex.Index, trackerVertex),
                OutgoingEdges = mainFlowGraph.OutgoingEdges.SetItem(
                    vertex.Index,
                    [.. subroutineFlowGraph.StartEdges]),
            };

            // flow branching statements are fine because they used to point to the call statement
            // but since the index of the tracker statement is the same as of the call statement
            // nothing needs to be changed
        }

        // 3. synthesize resolution statement
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

        ImmutableList<FlowEdge>.Builder edgesBuilder = ImmutableList.CreateBuilder<FlowEdge>();

        foreach (long site in callSites)
        {
            edgesBuilder.Add(FlowEdge.CreateStrongTo(nextVertices[site]));
        }

        mainFlowGraph = mainFlowGraph with
        {
            Vertices = mainFlowGraph.Vertices.Add(resolutionVertex.Index, resolutionVertex),
            OutgoingEdges = mainFlowGraph.OutgoingEdges.Add(resolutionVertex.Index, edgesBuilder.ToImmutable()),
        };

        // 4. for all vertices V s.t. V is in subroutine flow graph and V -> empty vertex, make V instead point to resolutionVertex
        foreach (FlowVertex vertex in subroutineFlowGraph.Vertices.Values)
        {
            if (subroutineFlowGraph.OutgoingEdges[vertex.Index].Contains(FlowGraph.FinalEdge))
            {
                mainFlowGraph = mainFlowGraph with
                {
                    OutgoingEdges = mainFlowGraph.OutgoingEdges.SetItem(
                        vertex.Index,
                        mainFlowGraph.OutgoingEdges[vertex.Index].Remove(FlowGraph.FinalEdge).Add(FlowEdge.CreateStrongTo(resolution.Index))),
                };
            }
        }

        return mainFlowGraph;
    }
}
