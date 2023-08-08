using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Edges = System.Collections.Immutable.ImmutableDictionary<
    int, System.Collections.Immutable.ImmutableList<int>>;
using Vertices = System.Collections.Immutable.ImmutableDictionary<
    int, Phantonia.Historia.Language.FlowAnalysis.FlowVertex>;
using MutEdges = System.Collections.Generic.Dictionary<
    int, System.Collections.Generic.List<int>>;
using MutVertices = System.Collections.Generic.Dictionary<
    int, Phantonia.Historia.Language.FlowAnalysis.FlowVertex>;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.FlowAnalysis;

public sealed partial class FlowAnalyzer
{
    private FlowGraph MergeFlowGraphs(IReadOnlyDictionary<SceneSymbol, FlowGraph> sceneFlowGraphs, IReadOnlyDictionary<SceneSymbol, int> referenceCounts)
    {
        SceneSymbol mainScene = (SceneSymbol)symbolTable["main"];
        FlowGraph mainFlowGraph = sceneFlowGraphs[mainScene];

        Vertices vertices = mainFlowGraph.Vertices;
        Edges edges = mainFlowGraph.OutgoingEdges;

        foreach ((SceneSymbol scene, FlowGraph graph) in sceneFlowGraphs)
        {
            if (referenceCounts[scene] == 0)
            {
                continue;
            }

            vertices = vertices.AddRange(graph.Vertices);
            edges = edges.AddRange(graph.OutgoingEdges);
        }

        FlowGraph finalFlowGraph = FlowGraph.Empty with
        {
            StartVertex = mainFlowGraph.StartVertex,
            Vertices = vertices,
            OutgoingEdges = edges,
        };

        Dictionary<SceneSymbol, CallerTrackerSymbol> trackers =
            referenceCounts.Where(p => p.Value >= 2)
                           .ToDictionary(
                                p => p.Key,
                                p => new CallerTrackerSymbol
                                {
                                    CalledScene = p.Key,
                                    CallSiteCount = p.Value,
                                    Name = p.Key.Name,
                                    Index = p.Key.Index + 1,
                                });

        return RedirectCallStatements(finalFlowGraph, sceneFlowGraphs, referenceCounts, trackers);
    }

    private FlowGraph RedirectCallStatements(FlowGraph totalFlowGraph, IReadOnlyDictionary<SceneSymbol, FlowGraph> sceneFlowGraphs, IReadOnlyDictionary<SceneSymbol, int> referenceCounts, IReadOnlyDictionary<SceneSymbol, CallerTrackerSymbol> trackers)
    {
        MutEdges edges = totalFlowGraph.OutgoingEdges.ToDictionary(p => p.Key, p => p.Value.ToList());
        MutVertices vertices = totalFlowGraph.Vertices.ToDictionary(p => p.Key, p => p.Value);

        Dictionary<SceneSymbol, List<int>> callSites = new();

        // handle all call statements
        foreach (FlowVertex vertex in totalFlowGraph.Vertices.Values)
        {
            if (vertex.AssociatedStatement is BoundCallStatementNode { Scene: SceneSymbol calledScene })
            {
                FlowGraph sceneGraph = sceneFlowGraphs[calledScene];

                if (referenceCounts[calledScene] == 1)
                {
                    // redirect every edge to this vertex to the start vertex of the scene flow graph
                    // redirect every edge to the empty vertex (in the scene flow graph) to the next vertex
                    Debug.Assert(edges[vertex.Index].Count == 1);
                    int nextVertex = edges[vertex.Index][0];

                    foreach ((int v, List<int> vEdges) in edges)
                    {
                        for (int i = 0; i < vEdges.Count; i++)
                        {
                            if (vEdges[i] == vertex.Index)
                            {
                                vEdges.RemoveAt(i);
                                vEdges.Add(sceneGraph.StartVertex);
                            }
                        }
                    }

                    foreach (int v in sceneGraph.OutgoingEdges.Keys)
                    {
                        for (int i = 0; i < edges[v].Count; i++)
                        {
                            if (edges[v][i] == nextVertex)
                            {
                                edges[v].RemoveAt(i);
                                edges[v].Add(nextVertex);
                            }
                        }
                    }
                }
                else
                {
                    callSites.TryAdd(calledScene, new List<int>());
                    int callSiteIndex = callSites[calledScene].Count;
                    callSites[calledScene].Add(vertex.Index);

                    CallerTrackerSymbol tracker = trackers[calledScene];

                    CallerTrackerStatement trackerStatement = new()
                    {
                        CallSiteIndex = callSiteIndex,
                        Tracker = tracker,
                        Index = vertex.Index,
                    };

                    // replace the call statement with the tracker statement
                    vertices[vertex.Index] = new FlowVertex
                    {
                        AssociatedStatement = trackerStatement,
                        IsVisible = false,
                        Index = vertex.Index,
                    };

                    // redirect the tracker statement to the scene start vertex
                    edges[vertex.Index].Clear();
                    edges[vertex.Index].Add(sceneGraph.StartVertex);
                }
            }
        }

        // add caller resolutions
        foreach ((SceneSymbol scene, FlowGraph sceneGraph) in sceneFlowGraphs)
        {
            if (referenceCounts[scene] <= 1)
            {
                continue;
            }

            CallerTrackerSymbol tracker = trackers[scene];

            CallerResolutionStatement resolution = new()
            {
                CallSites = callSites[scene].ToImmutableArray(),
                Tracker = tracker,
                Index = scene.Index + 2,
            };

            FlowVertex resolutionVertex = new()
            {
                AssociatedStatement = resolution,
                IsVisible = false,
                Index = resolution.Index,
            };

            vertices[resolution.Index] = resolutionVertex;
            edges[resolution.Index] = callSites[scene];

            // redirect edges into the empty vertex for the scene to the new resolution vertex
            foreach ((int v, ImmutableList<int> vEdges) in sceneGraph.OutgoingEdges)
            {
                for (int i = 0; i < vEdges.Count; i++)
                {
                    if (vEdges[i] == FlowGraph.EmptyVertex)
                    {
                        vEdges.RemoveAt(i);
                        vEdges.Add(resolution.Index);
                        break;
                    }
                }
            }
        }

        return totalFlowGraph with
        {
            OutgoingEdges = edges.ToImmutableDictionary(p => p.Key, p => p.Value.ToImmutableList()),
            Vertices = vertices.ToImmutableDictionary(),
        };
    }
}
