using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Edges = System.Collections.Immutable.ImmutableDictionary<
    long, System.Collections.Immutable.ImmutableList<
        Phantonia.Historia.Language.FlowAnalysis.FlowEdge>>;
using MutEdges = System.Collections.Generic.Dictionary<
    long, System.Collections.Generic.List<
        Phantonia.Historia.Language.FlowAnalysis.FlowEdge>>;

namespace Phantonia.Historia.Language.FlowAnalysis;

public sealed record FlowGraph
{
    public const int FinalVertex = -1;

    public static FlowEdge FinalEdge { get; set; } = FlowEdge.CreateStrongTo(FinalVertex);

    public static FlowGraph Empty { get; } = new();

    public static FlowGraph CreateSimpleFlowGraph(FlowVertex vertex) => Empty with
    {
        StartEdges = [FlowEdge.CreateStrongTo(vertex.Index)],
        OutgoingEdges = Empty.OutgoingEdges.Add(vertex.Index, [FinalEdge]),
        Vertices = Empty.Vertices.Add(vertex.Index, vertex),
    };

    public static FlowGraph CreateSimpleSemanticFlowGraph(FlowVertex vertex) => Empty with
    {
        StartEdges = [FlowEdge.CreatePurelySemanticTo(vertex.Index)],
        OutgoingEdges = Empty.OutgoingEdges.Add(vertex.Index, [FlowEdge.CreatePurelySemanticTo(FinalVertex)]),
        Vertices = Empty.Vertices.Add(vertex.Index, vertex),
    };

    private FlowGraph() { }

    public Edges OutgoingEdges { get; init; } = Edges.Empty;

    public ImmutableArray<FlowEdge> StartEdges { get; init; } = [FinalEdge];

    public ImmutableDictionary<long, FlowVertex> Vertices { get; init; } = ImmutableDictionary<long, FlowVertex>.Empty;

    // a flow graph is conformable if it only has a single start edge which is a story edge
    // what used to be StartVertex
    public bool IsConformable => StartEdges.Where(e => e.IsStory).Count() == 1;

    public long GetStoryStartVertex()
    {
        if (!IsConformable)
        {
            throw new InvalidOperationException("Non conformable flow graph doesn't have a (single) story start vertex");
        }

        return StartEdges.Single(e => e.IsStory).ToVertex;
    }

    public FlowGraph AddVertex(FlowVertex vertex, params FlowEdge[] edges)
    {
        if (Vertices.ContainsKey(vertex.Index))
        {
            throw new ArgumentException("Cannot add a vertex that is already there");
        }

        if (StartEdges is [{ ToVertex: FinalVertex }])
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

    public FlowGraph SetVertex(long index, FlowVertex newVertex)
    {
        Debug.Assert(Vertices.ContainsKey(index));

        return this with
        {
            Vertices = Vertices.SetItem(index, newVertex),
        };
    }

    public FlowGraph Append(FlowGraph graph)
    {
        // this method generates a huge amount of waste - can we optimize this?

        MutEdges tempEdges = OutgoingEdges.ToDictionary(p => p.Key, p => p.Value.ToList());
        Dictionary<long, FlowVertex> tempVertices = Vertices.ToDictionary(p => p.Key, p => p.Value);

        foreach ((long currentVertex, List<FlowEdge> edges) in tempEdges)
        {
            FlowEdgeKind outgoingKind = FlowEdgeKind.None;

            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i].ToVertex == FinalVertex)
                {
                    outgoingKind = edges[i].Kind;
                    edges.RemoveAt(i);
                    break;
                }
            }

            Debug.Assert(edges.All(e => e.ToVertex != FinalVertex));

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

                // we keep the old start edge kind - i think that's correct?
                edges.Add(startEdge with { Kind = newKind });
            }
        }

        foreach ((long currentVertex, ImmutableList<FlowEdge> edges) in graph.OutgoingEdges)
        {
            if (tempEdges.ContainsKey(currentVertex))
            {
                throw new InvalidOperationException("Duplicated vertex key");
            }

            tempEdges[currentVertex] = [.. edges];
            tempVertices[currentVertex] = graph.Vertices[currentVertex];
        }

        List<FlowEdge> newStartEdges = [.. StartEdges];

        if (newStartEdges.Any(e => e.ToVertex == FinalVertex))
        {
            newStartEdges.RemoveAll(e => e.ToVertex == FinalVertex);
            newStartEdges.AddRange(graph.StartEdges);
        }

        return this with
        {
            OutgoingEdges = tempEdges.ToImmutableDictionary(p => p.Key, p => p.Value.ToImmutableList()),
            Vertices = tempVertices.ToImmutableDictionary(),
            StartEdges = [.. newStartEdges],
        };
    }

    public FlowGraph AppendToVertex(long vertex, FlowGraph graph)
    {
        // this method generates a huge amount of waste - can we optimize this?

        if (!Vertices.ContainsKey(vertex))
        {
            throw new ArgumentException($"{nameof(vertex)} is not a vertex of this graph.");
        }

        MutEdges tempEdges = OutgoingEdges.ToDictionary(p => p.Key, p => p.Value.ToList());
        Dictionary<long, FlowVertex> tempVertices = Vertices.ToDictionary(p => p.Key, p => p.Value);

        // add 'graph' into this graph
        foreach ((long currentVertex, ImmutableList<FlowEdge> edges) in graph.OutgoingEdges)
        {
            if (tempEdges.ContainsKey(currentVertex))
            {
                throw new InvalidOperationException("Duplicated vertex key");
            }

            tempEdges[currentVertex] = [.. edges];
            tempVertices[currentVertex] = graph.Vertices[currentVertex];
        }

        tempEdges[vertex].RemoveAll(e => e.ToVertex == FinalVertex);

        foreach (FlowEdge edge in graph.StartEdges)
        {
            if (!tempEdges[vertex].Any(e => e.ToVertex == edge.ToVertex))
            {
                tempEdges[vertex].Add(edge);
            }
        }

        List<FlowEdge> newStartEdges = [.. StartEdges];

        if (newStartEdges.Any(e => e.ToVertex == FinalVertex))
        {
            newStartEdges.RemoveAll(e => e.ToVertex == FinalVertex);
            newStartEdges.AddRange(graph.StartEdges);
        }

        return this with
        {
            OutgoingEdges = tempEdges.ToImmutableDictionary(p => p.Key, p => p.Value.ToImmutableList()),
            Vertices = tempVertices.ToImmutableDictionary(),
            StartEdges = [.. newStartEdges],
        };
    }

    public FlowGraph Replace(int replacedVertex, FlowGraph graph)
    {
        // this method generates a huge amount of waste - can we optimize this?

        if (!Vertices.ContainsKey(replacedVertex))
        {
            throw new ArgumentException($"{nameof(replacedVertex)} is not a vertex of this graph.");
        }

        MutEdges tempEdges = OutgoingEdges.ToDictionary(p => p.Key, p => p.Value.ToList());
        Dictionary<long, FlowVertex> tempVertices = Vertices.ToDictionary(p => p.Key, p => p.Value);

        // add everything from 'graph' to our graph
        foreach ((long currentVertex, ImmutableList<FlowEdge> edges) in graph.OutgoingEdges)
        {
            if (Vertices.ContainsKey(currentVertex))
            {
                throw new InvalidOperationException("Duplicated vertex key");
            }

            tempEdges[currentVertex] = [.. edges];
            tempVertices[currentVertex] = graph.Vertices[currentVertex];
        }

        // turn every edge to 'vertex' into 'graph.StartEdges'
        foreach ((long currentVertex, List<FlowEdge> edges) in tempEdges)
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

        foreach ((long currentVertex, ImmutableList<FlowEdge> edges) in graph.OutgoingEdges)
        {
            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i].ToVertex == FinalVertex)
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

        foreach ((long vertex, ImmutableList<FlowEdge> edges) in OutgoingEdges)
        {
            foreach (FlowEdge edge in edges)
            {
                tempEdges.TryAdd(edge.ToVertex, []);
                tempEdges[edge.ToVertex].Add(edge with { ToVertex = vertex });

                if (edge.ToVertex == FinalVertex)
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
        Dictionary<long, FlowVertex> tempVertices = Vertices.ToDictionary(p => p.Key, p => p.Value);

        List<long> newStartVertices = [.. StartEdges.Where(e => e.IsStory).Select(e => e.ToVertex)];

        foreach (FlowVertex vertex in Vertices.Values)
        {
            if (vertex.IsVisible)
            {
                continue;
            }

            tempVertices.Remove(vertex.Index);
            tempEdges.Remove(vertex.Index);

            ImmutableList<FlowEdge> outgoingEdges = OutgoingEdges[vertex.Index];

            foreach (long otherVertex in tempVertices.Keys)
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

    public IEnumerable<long> TopologicalSort()
    {
        Dictionary<long, bool> marked = [];

        foreach (long vertex in Vertices.Keys)
        {
            marked[vertex] = false;
        }

        Stack<long> postOrder = [];

        foreach (long vertex in Vertices.Keys)
        {
            if (!marked[vertex])
            {
                DepthFirstSearch(vertex);
            }
        }

        return postOrder;

        void DepthFirstSearch(long vertex)
        {
            marked[vertex] = true;

            // without a reverse here, the way the stack works, vertices on the same level will always be the wrong way around
            foreach (FlowEdge edge in OutgoingEdges[vertex].Reverse())
            {
                if (edge.IsSemantic && edge.ToVertex != FinalVertex && !marked[edge.ToVertex])
                {
                    DepthFirstSearch(edge.ToVertex);
                }
            }

            postOrder.Push(vertex);
        }
    }
}
