using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Edges = System.Collections.Immutable.ImmutableDictionary<
    uint, System.Collections.Immutable.ImmutableList<
        Phantonia.Historia.Language.FlowAnalysis.FlowEdge>>;
using MutEdges = System.Collections.Generic.Dictionary<
    uint, System.Collections.Generic.List<
        Phantonia.Historia.Language.FlowAnalysis.FlowEdge>>;

namespace Phantonia.Historia.Language.FlowAnalysis;

public sealed record FlowGraph
{
    public const uint Source = 0;
    public const uint Sink = uint.MaxValue;

    public static FlowEdge FinalEdge { get; set; } = FlowEdge.CreateStrongTo(Sink);

    public static FlowGraph Empty { get; } = new();

    public static FlowGraph CreateSimpleFlowGraph(FlowVertex vertex) => new()
    {
        StartEdges = [FlowEdge.CreateStrongTo(vertex.Index)],
        OutgoingEdges = Empty.OutgoingEdges.Add(vertex.Index, [FinalEdge]),
        Vertices = Empty.Vertices.Add(vertex.Index, vertex),
    };

    public static FlowGraph CreateSimpleSemanticFlowGraph(FlowVertex vertex) => new()
    {
        StartEdges = [FlowEdge.CreatePurelySemanticTo(vertex.Index)],
        OutgoingEdges = Empty.OutgoingEdges.Add(vertex.Index, [FlowEdge.CreatePurelySemanticTo(Sink)]),
        Vertices = Empty.Vertices.Add(vertex.Index, vertex),
    };

    private FlowGraph() { }

    public Edges OutgoingEdges { get; init; } = Edges.Empty;

    public ImmutableArray<FlowEdge> StartEdges { get; init; } = [FinalEdge];

    public ImmutableDictionary<uint, FlowVertex> Vertices { get; init; } = ImmutableDictionary<uint, FlowVertex>.Empty;

    // a flow graph is conformable if it only has a single start edge which is a story edge
    // what used to be StartVertex
    public bool IsConformable => StartEdges.Where(e => e.IsStory).Count() == 1;

    public uint GetStoryStartVertex()
    {
        if (!IsConformable)
        {
            throw new InvalidOperationException("Non conformable flow graph doesn't have a (single) story start vertex");
        }

        return StartEdges.Single(e => e.IsStory).ToVertex;
    }

    public FlowGraph AddVertex(FlowVertex vertex)
    {
        if (Vertices.ContainsKey(vertex.Index))
        {
            throw new ArgumentException($"Vertex {vertex.Index} already exists");
        }

        if (StartEdges is [{ ToVertex: Sink }])
        {
            return this with
            {
                Vertices = Vertices.Add(vertex.Index, vertex),
                OutgoingEdges = OutgoingEdges.Add(vertex.Index, []),
                StartEdges = [FlowEdge.CreateStrongTo(vertex.Index)],
            };
        }
        else
        {
            return this with
            {
                Vertices = Vertices.Add(vertex.Index, vertex),
                OutgoingEdges = OutgoingEdges.Add(vertex.Index, []),
            };
        }
    }

    public FlowGraph AddVertex(FlowVertex vertex, params FlowEdge[] edges) => AddVertex(vertex, (IEnumerable<FlowEdge>)edges);

    public FlowGraph AddVertex(FlowVertex vertex, IEnumerable<FlowEdge> edges)
    {
        if (Vertices.ContainsKey(vertex.Index))
        {
            throw new ArgumentException($"Vertex {vertex.Index} already exists");
        }

        if (StartEdges is [{ ToVertex: Sink }])
        {
            return this with
            {
                Vertices = Vertices.Add(vertex.Index, vertex),
                OutgoingEdges = OutgoingEdges.Add(vertex.Index, [.. edges]),
                StartEdges = [FlowEdge.CreateStrongTo(vertex.Index)],
            };
        }
        else
        {
            return this with
            {
                Vertices = Vertices.Add(vertex.Index, vertex),
                OutgoingEdges = OutgoingEdges.Add(vertex.Index, [.. edges]),
            };
        }
    }

    public FlowGraph AddVertexWithoutStartEdge(FlowVertex vertex)
    {
        if (Vertices.ContainsKey(vertex.Index))
        {
            throw new ArgumentException($"Vertex {vertex.Index} already exists");
        }

        return this with
        {
            Vertices = Vertices.Add(vertex.Index, vertex),
            OutgoingEdges = OutgoingEdges.Add(vertex.Index, []),
        };
    }

    public FlowGraph AddEdge(uint startingPoint, FlowEdge edge)
    {
        if (!OutgoingEdges.TryGetValue(startingPoint, out ImmutableList<FlowEdge>? edgeList))
        {
            throw new ArgumentException($"Vertex {startingPoint} does not exist");
        }

        if (edgeList.Any(e => e.ToVertex == edge.ToVertex))
        {
            throw new ArgumentException($"Edge {startingPoint} to {edge.ToVertex} already exists");
        }

        return this with
        {
            OutgoingEdges = OutgoingEdges.SetItem(startingPoint, edgeList.Add(edge)),
        };
    }

    public FlowGraph AddStartEdge(FlowEdge edge)
    {
        if (StartEdges.Any(e => e.ToVertex == edge.ToVertex))
        {
            throw new ArgumentException($"Start edge to {edge.ToVertex} already exists");
        }

        if (StartEdges is [{ ToVertex: Sink }])
        {
            return this with
            {
                StartEdges = [edge],
            };
        }

        return this with
        {
            StartEdges = StartEdges.Add(edge),
        };
    }

    public FlowGraph AddEdges(uint startingPoint, IEnumerable<FlowEdge> edges)
    {
        if (!OutgoingEdges.TryGetValue(startingPoint, out ImmutableList<FlowEdge>? edgeList))
        {
            throw new ArgumentException($"Vertex {startingPoint} does not exist");
        }

        foreach (FlowEdge edge in edges)
        {
            if (edgeList.Any(e => e.ToVertex == edge.ToVertex))
            {
                throw new ArgumentException($"Edge {startingPoint} to {edge.ToVertex} already exists");
            }
        }

        return this with
        {
            OutgoingEdges = OutgoingEdges.SetItem(startingPoint, edgeList.AddRange(edges)),
        };
    }

    public FlowGraph RemoveVertex(uint vertex)
    {
        MutEdges tempEdges = OutgoingEdges.ToDictionary(p => p.Key, p => p.Value.ToList());

        foreach (uint v in OutgoingEdges.Keys)
        {
            tempEdges[v].RemoveAll(e => e.ToVertex == vertex);
        }

        tempEdges.Remove(vertex);

        return this with
        {
            StartEdges = StartEdges.RemoveAll(e => e.ToVertex == vertex),
            OutgoingEdges = tempEdges.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutableList()),
            Vertices = Vertices.Remove(vertex)
        };
    }

    public FlowGraph RemoveEdge(uint startingPoint, uint endPoint)
    {
        if (!OutgoingEdges.TryGetValue(startingPoint, out ImmutableList<FlowEdge>? edgeList))
        {
            throw new ArgumentException($"Vertex {startingPoint} does not exist");
        }

        if (edgeList.All(e => e.ToVertex != endPoint))
        {
            throw new ArgumentException($"Edge {startingPoint} to {endPoint} does not exist");
        }

        return this with
        {
            OutgoingEdges = OutgoingEdges.SetItem(startingPoint, edgeList.RemoveAll(e => e.ToVertex == endPoint)),
        };
    }

    public FlowGraph SetVertex(uint index, FlowVertex newVertex)
    {
        if (!Vertices.ContainsKey(index))
        {
            throw new ArgumentException($"Vertex {index} does not exist");
        }

        newVertex = newVertex with
        {
            Index = index,
        };

        return this with
        {
            Vertices = Vertices.SetItem(index, newVertex),
        };
    }

    public FlowGraph SetEdges(uint vertex, IEnumerable<FlowEdge> edges)
    {
        if (!Vertices.ContainsKey(vertex))
        {
            throw new ArgumentException($"Vertex {vertex} does not exist");
        }

        Debug.Assert(OutgoingEdges.ContainsKey(vertex));

        return this with
        {
            OutgoingEdges = OutgoingEdges.SetItem(vertex, [.. edges]),
        };
    }

    public FlowGraph ReplaceEdge(uint startingPoint, uint originalEndPoint, FlowEdge newEdge)
    {
        if (!Vertices.ContainsKey(startingPoint))
        {
            throw new ArgumentException($"Vertex {startingPoint} does not exist");
        }

        if (!Vertices.ContainsKey(originalEndPoint) && originalEndPoint is not Sink)
        {
            throw new ArgumentException($"Vertex {originalEndPoint} does not exist");
        }

        if (OutgoingEdges[startingPoint].All(e => e.ToVertex != originalEndPoint))
        {
            throw new ArgumentException($"Edge {startingPoint} to {originalEndPoint} does not exist");
        }

        if (OutgoingEdges[startingPoint].Any(e => e.ToVertex == newEdge.ToVertex))
        {
            throw new ArgumentException($"Edge {startingPoint} to {newEdge.ToVertex} already exists");
        }

        FlowEdge originalEdge = OutgoingEdges[startingPoint].Single(e => e.ToVertex == originalEndPoint);

        return SetEdges(startingPoint, OutgoingEdges[startingPoint].Replace(originalEdge, newEdge));
    }

    public FlowGraph Append(FlowGraph graph)
    {
        // this method generates a huge amount of waste - can we optimize this?

        MutEdges tempEdges = OutgoingEdges.ToDictionary(p => p.Key, p => p.Value.ToList());
        Dictionary<uint, FlowVertex> tempVertices = Vertices.ToDictionary(p => p.Key, p => p.Value);

        foreach ((uint currentVertex, List<FlowEdge> edges) in tempEdges)
        {
            FlowEdgeKind outgoingKind = FlowEdgeKind.None;

            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i].ToVertex is Sink)
                {
                    outgoingKind = edges[i].Kind;
                    edges.RemoveAt(i);
                    break;
                }
            }

            Debug.Assert(edges.All(e => e.ToVertex is not Sink));

            if (outgoingKind == FlowEdgeKind.None)
            {
                continue;
            }

            foreach (FlowEdge startEdge in graph.StartEdges)
            {
                // if any of the two kinds is semantic, it's probably for a reason, so our new combined edge is also semantic
                // else we keep the outgoing kind
                // purely empirically, this seems to work but we might have to come back to this, idk
                FlowEdgeKind incomingKind = startEdge.Kind;
                FlowEdgeKind newKind = (outgoingKind, incomingKind) switch
                {
                    (FlowEdgeKind.Semantic, _) => FlowEdgeKind.Semantic,
                    (_, FlowEdgeKind.Semantic) => FlowEdgeKind.Semantic,
                    _ => outgoingKind,
                };

                edges.Add(startEdge with { Kind = newKind });
            }
        }

        foreach ((uint currentVertex, ImmutableList<FlowEdge> edges) in graph.OutgoingEdges)
        {
            if (tempEdges.ContainsKey(currentVertex))
            {
                throw new InvalidOperationException($"Vertex {currentVertex} exists in both the current as well as the appended flow graph");
            }

            tempEdges[currentVertex] = [.. edges];
            tempVertices[currentVertex] = graph.Vertices[currentVertex];
        }

        List<FlowEdge> newStartEdges = [.. StartEdges];

        if (newStartEdges.Any(e => e.ToVertex == Sink))
        {
            newStartEdges.RemoveAll(e => e.ToVertex == Sink);
            newStartEdges.AddRange(graph.StartEdges);
        }

        return this with
        {
            OutgoingEdges = tempEdges.ToImmutableDictionary(p => p.Key, p => p.Value.ToImmutableList()),
            Vertices = tempVertices.ToImmutableDictionary(),
            StartEdges = [.. newStartEdges],
        };
    }

    public FlowGraph AppendToVertex(uint vertex, FlowGraph graph)
    {
        // this method generates a huge amount of waste - can we optimize this?

        if (!Vertices.ContainsKey(vertex))
        {
            throw new ArgumentException($"Vertex {vertex} is not a vertex of this graph.");
        }

        MutEdges tempEdges = OutgoingEdges.ToDictionary(p => p.Key, p => p.Value.ToList());
        Dictionary<uint, FlowVertex> tempVertices = Vertices.ToDictionary(p => p.Key, p => p.Value);

        // add 'graph' into this graph
        foreach ((uint currentVertex, ImmutableList<FlowEdge> edges) in graph.OutgoingEdges)
        {
            if (tempEdges.ContainsKey(currentVertex))
            {
                throw new InvalidOperationException($"Vertex {currentVertex} exists in both the current as well as the appended flow graph");
            }

            tempEdges[currentVertex] = [.. edges];
            tempVertices[currentVertex] = graph.Vertices[currentVertex];
        }

        tempEdges[vertex].RemoveAll(e => e.ToVertex is Sink);

        foreach (FlowEdge edge in graph.StartEdges)
        {
            if (!tempEdges[vertex].Any(e => e.ToVertex == edge.ToVertex))
            {
                tempEdges[vertex].Add(edge);
            }
        }

        List<FlowEdge> newStartEdges = [.. StartEdges];

        if (newStartEdges.Any(e => e.ToVertex is Sink))
        {
            newStartEdges.RemoveAll(e => e.ToVertex is Sink);
            newStartEdges.AddRange(graph.StartEdges);
        }

        return this with
        {
            OutgoingEdges = tempEdges.ToImmutableDictionary(p => p.Key, p => p.Value.ToImmutableList()),
            Vertices = tempVertices.ToImmutableDictionary(),
            StartEdges = [.. newStartEdges],
        };
    }

    public FlowGraph Replace(uint replacedVertex, FlowGraph graph)
    {
        // this method generates a huge amount of waste - can we optimize this?

        if (!Vertices.ContainsKey(replacedVertex))
        {
            throw new ArgumentException($"{nameof(replacedVertex)} is not a vertex of this graph.");
        }

        MutEdges tempEdges = OutgoingEdges.ToDictionary(p => p.Key, p => p.Value.ToList());
        Dictionary<uint, FlowVertex> tempVertices = Vertices.ToDictionary(p => p.Key, p => p.Value);

        // add everything from 'graph' to our graph
        foreach ((uint currentVertex, ImmutableList<FlowEdge> edges) in graph.OutgoingEdges)
        {
            if (Vertices.ContainsKey(currentVertex))
            {
                throw new InvalidOperationException("Duplicated vertex key");
            }

            tempEdges[currentVertex] = [.. edges];
            tempVertices[currentVertex] = graph.Vertices[currentVertex];
        }

        // turn every edge to 'vertex' into 'graph.StartEdges'
        foreach ((uint currentVertex, List<FlowEdge> edges) in tempEdges)
        {
            FlowEdgeKind kind = FlowEdgeKind.None;

            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i].ToVertex == replacedVertex)
                {
                    kind = edges[i].Kind;
                    edges.RemoveAt(i);
                    break;
                }
            }

            // assert that there was only one edge to 'replacedVertex'
            Debug.Assert(edges.All(e => e.ToVertex != replacedVertex));

            if (kind == FlowEdgeKind.None)
            {
                continue;
            }

            foreach (FlowEdge startEdge in graph.StartEdges)
            {
                // the old edge kind overrides the startEdge kind - is that correct?
                edges.Add(startEdge with { Kind = kind });
            }
        }

        // redirect all edges to 'graph's EmptyVertex to every vertex that 'replacedVertex' points to
        ImmutableList<FlowEdge> replacedVertexEdges = OutgoingEdges[replacedVertex];

        foreach ((uint currentVertex, ImmutableList<FlowEdge> edges) in graph.OutgoingEdges)
        {
            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i].ToVertex == Sink)
                {
                    tempEdges[currentVertex].RemoveAt(i);

                    for (int j = 0; j < replacedVertexEdges.Count; j++)
                    {
                        tempEdges[currentVertex].Add(replacedVertexEdges[j]);
                    }
                }
            }
        }

        List<FlowEdge> newStartEdges = [.. StartEdges];

        if (newStartEdges.Any(e => e.ToVertex == replacedVertex))
        {
            newStartEdges.RemoveAll(e => e.ToVertex == replacedVertex);
            newStartEdges.AddRange(graph.StartEdges);
        }

        tempVertices.Remove(replacedVertex);
        tempEdges.Remove(replacedVertex);

        return this with
        {
            OutgoingEdges = tempEdges.ToImmutableDictionary(p => p.Key, p => p.Value.ToImmutableList()),
            Vertices = tempVertices.ToImmutableDictionary(),
            StartEdges = [.. newStartEdges],
        };
    }

    public FlowGraph Reverse()
    {
        MutEdges tempEdges = [];
        List<FlowEdge> newStartEdges = [];

        foreach ((uint vertex, ImmutableList<FlowEdge> edges) in OutgoingEdges)
        {
            foreach (FlowEdge edge in edges)
            {
                tempEdges.TryAdd(edge.ToVertex, []);
                tempEdges[edge.ToVertex].Add(edge with { ToVertex = vertex });

                if (edge.ToVertex == Sink)
                {
                    newStartEdges.Add(edge with { ToVertex = vertex });
                }
            }
        }

        return new FlowGraph
        {
            Vertices = Vertices,
            OutgoingEdges = tempEdges.ToImmutableDictionary(p => p.Key, p => p.Value.ToImmutableList()),
            StartEdges = [.. newStartEdges],
        };
    }

    public FlowGraph RemoveInvisible()
    {
        MutEdges tempEdges = OutgoingEdges.ToDictionary(p => p.Key, p => p.Value.Where(e => e.IsStory || (Vertices.TryGetValue(e.ToVertex, out FlowVertex v) && v.IsVisible)).ToList());
        Dictionary<uint, FlowVertex> tempVertices = Vertices.ToDictionary(p => p.Key, p => p.Value);

        List<uint> newStartVertices = [.. StartEdges.Where(e => e.IsStory).Select(e => e.ToVertex)];

        foreach (FlowVertex vertex in Vertices.Values)
        {
            if (vertex.IsVisible)
            {
                continue;
            }

            tempVertices.Remove(vertex.Index);
            tempEdges.Remove(vertex.Index);

            ImmutableList<FlowEdge> outgoingEdges = OutgoingEdges[vertex.Index];

            foreach (uint otherVertex in tempVertices.Keys)
            {
                if (!tempEdges[otherVertex].Any(e => e.ToVertex == vertex.Index))
                {
                    continue;
                }

                tempEdges[otherVertex].RemoveAll(e => e.ToVertex == vertex.Index);
                tempEdges[otherVertex].AddRange(outgoingEdges.Where(e => e.ToVertex != otherVertex));
            }

            if (newStartVertices.Contains(vertex.Index))
            {
                newStartVertices.Remove(vertex.Index);

                foreach (FlowEdge edge in outgoingEdges)
                {
                    newStartVertices.Add(edge.ToVertex);
                }
            }
        }

        return this with
        {
            OutgoingEdges = tempEdges.ToImmutableDictionary(p => p.Key, p => p.Value.ToImmutableList()),
            Vertices = tempVertices.ToImmutableDictionary(),
            StartEdges = [.. newStartVertices.Select(FlowEdge.CreateStrongTo)],
        };
    }

    public IEnumerable<uint> TopologicalSort()
    {
        Dictionary<uint, bool> marked = [];

        foreach (uint vertex in Vertices.Keys)
        {
            marked[vertex] = false;
        }

        Stack<uint> postOrder = [];

        foreach (uint vertex in Vertices.Keys)
        {
            if (!marked[vertex])
            {
                DepthFirstSearch(vertex);
            }
        }

        return postOrder;

        void DepthFirstSearch(uint vertex)
        {
            marked[vertex] = true;

            // without a reverse here, the way the stack works, vertices on the same level will always be the wrong way around
            foreach (FlowEdge edge in OutgoingEdges[vertex].Reverse())
            {
                if (edge.IsSemantic && edge.ToVertex != Sink && !marked[edge.ToVertex])
                {
                    DepthFirstSearch(edge.ToVertex);
                }
            }

            postOrder.Push(vertex);
        }
    }
}
