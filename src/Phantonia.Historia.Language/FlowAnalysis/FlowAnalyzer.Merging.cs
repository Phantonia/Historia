using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language.FlowAnalysis;

public sealed partial class FlowAnalyzer
{
    private (FlowGraph, SymbolTable) MergeFlowGraphs(IEnumerable<SceneSymbol> topologicalOrder, IReadOnlyDictionary<SceneSymbol, FlowGraph> sceneFlowGraphs, IReadOnlyDictionary<SceneSymbol, int> referenceCounts)
    {
        SymbolTable symbolTable = this.symbolTable;

        Debug.Assert(topologicalOrder.First().Name == "main");

        FlowGraph mainFlowGraph = sceneFlowGraphs[topologicalOrder.First()];

        foreach (SceneSymbol scene in topologicalOrder.Skip(1))
        {
            if (!referenceCounts.TryGetValue(scene, out int refCount) || refCount == 0)
            {
                continue;
            }

            if (refCount == 1)
            {
                mainFlowGraph = EmbedSingleReferenceScene(mainFlowGraph, sceneFlowGraphs[scene], scene);
            }
            else
            {
                CallerTrackerSymbol tracker = new()
                {
                    CalledScene = scene,
                    Name = $"${scene.Name}", // unspeakable name
                    CallSiteCount = refCount,
                    // the indices are the literal character indices in the source code
                    // and since a scene declaration is at least scene A{}, one more than its index is not taken
                    Index = scene.Index + 1,
                };

                symbolTable = symbolTable.Declare(tracker);

                mainFlowGraph = EmbedMultiReferenceScene(mainFlowGraph, sceneFlowGraphs[scene], scene, tracker);
            }
        }

        Debug.Assert(mainFlowGraph.Vertices.Values.All(v => v.AssociatedStatement is not BoundCallStatementNode));

        return (mainFlowGraph, symbolTable);
    }

    private FlowGraph EmbedSingleReferenceScene(FlowGraph mainFlowGraph, FlowGraph sceneFlowGraph, SceneSymbol scene)
    {
        // 1. add all scene vertices
        foreach (int vertex in sceneFlowGraph.Vertices.Keys)
        {
            mainFlowGraph = mainFlowGraph with
            {
                Vertices = mainFlowGraph.Vertices.Add(vertex, sceneFlowGraph.Vertices[vertex]),
                OutgoingEdges = mainFlowGraph.OutgoingEdges.Add(vertex, sceneFlowGraph.OutgoingEdges[vertex]),
            };
        }

        // 2. find callVertex and nextVertex (the single vertex that callVertex points to, since it's linear)
        int callVertex = int.MinValue;
        int nextVertex = int.MinValue;

        foreach (FlowVertex vertex in mainFlowGraph.Vertices.Values)
        {
            if (vertex.AssociatedStatement is not BoundCallStatementNode { Scene: SceneSymbol calledScene } || calledScene != scene)
            {
                continue;
            }

            callVertex = vertex.Index;

            Debug.Assert(mainFlowGraph.OutgoingEdges[vertex.Index].Count == 1); // assert vertex is infact linear
            nextVertex = mainFlowGraph.OutgoingEdges[vertex.Index][0].ToVertex;
            break;
        }

        Debug.Assert(callVertex != int.MinValue);
        Debug.Assert(nextVertex != int.MinValue);

        // 3. for all vertices V s.t. (V -> callVertex) instead let (V -> sceneFlowGraph.StartVertex)
        if (mainFlowGraph.StartEdges.Any(e => e.ToVertex == callVertex))
        {
            List<FlowEdge> newStartEdges = [.. mainFlowGraph.StartEdges];
            newStartEdges.RemoveAll(e => e.ToVertex == callVertex);
            newStartEdges.AddRange(sceneFlowGraph.StartEdges);

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
                                         .AddRange(sceneFlowGraph.StartEdges)),
                };
            }
        }

        // 4. for all vertices V s.t. V is in sceneFlowGraph and V points to the empty vertex, remove edge to empty vertex and instead let (V -> N)
        foreach (FlowVertex vertex in sceneFlowGraph.Vertices.Values)
        {
            if (sceneFlowGraph.OutgoingEdges[vertex.Index].Contains(FlowGraph.FinalEdge))
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
            }
        }

        mainFlowGraph = mainFlowGraph with
        {
            Vertices = mainFlowGraph.Vertices.Remove(callVertex),
            OutgoingEdges = mainFlowGraph.OutgoingEdges.Remove(callVertex),
        };

        return mainFlowGraph;
    }

    private FlowGraph EmbedMultiReferenceScene(FlowGraph mainFlowGraph, FlowGraph sceneFlowGraph, SceneSymbol scene, CallerTrackerSymbol tracker)
    {
        // 1. add all scene vertices
        foreach (int vertex in sceneFlowGraph.Vertices.Keys)
        {
            mainFlowGraph = mainFlowGraph with
            {
                Vertices = mainFlowGraph.Vertices.Add(vertex, sceneFlowGraph.Vertices[vertex]),
                OutgoingEdges = mainFlowGraph.OutgoingEdges.Add(vertex, sceneFlowGraph.OutgoingEdges[vertex]),
            };
        }

        // 2. find all callsites, replace them with tracker statements and redirect them correctly
        Dictionary<int, int> nextVertices = [];
        List<int> callSites = [];

        foreach (FlowVertex vertex in mainFlowGraph.Vertices.Values)
        {
            if (vertex.AssociatedStatement is not BoundCallStatementNode { Scene: SceneSymbol calledScene } || calledScene != scene)
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
                },
                Kind = FlowVertexKind.Invisible,
            };

            callSites.Add(vertex.Index);

            mainFlowGraph = mainFlowGraph with
            {
                Vertices = mainFlowGraph.Vertices.SetItem(vertex.Index, trackerVertex),
                OutgoingEdges = mainFlowGraph.OutgoingEdges.SetItem(
                    vertex.Index,
                    [.. sceneFlowGraph.StartEdges]),
            };
        }

        // 3. synthesize resolution statement
        CallerResolutionStatementNode resolution = new()
        {
            Tracker = tracker,
            Index = scene.Index + 2,
        };

        FlowVertex resolutionVertex = new()
        {
            AssociatedStatement = resolution,
            Index = resolution.Index,
            Kind = FlowVertexKind.Invisible,
        };

        ImmutableList<FlowEdge>.Builder edgesBuilder = ImmutableList.CreateBuilder<FlowEdge>();

        foreach (int site in callSites)
        {
            edgesBuilder.Add(FlowEdge.CreateStrongTo(nextVertices[site]));
        }

        mainFlowGraph = mainFlowGraph with
        {
            Vertices = mainFlowGraph.Vertices.Add(resolutionVertex.Index, resolutionVertex),
            OutgoingEdges = mainFlowGraph.OutgoingEdges.Add(resolutionVertex.Index, edgesBuilder.ToImmutable()),
        };

        // 4. for all vertices V s.t. V is in scene flow graph and V -> empty vertex, make V instead point to resolutionVertex
        foreach (FlowVertex vertex in sceneFlowGraph.Vertices.Values)
        {
            if (sceneFlowGraph.OutgoingEdges[vertex.Index].Contains(FlowGraph.FinalEdge))
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
