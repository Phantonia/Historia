﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Edges = System.Collections.Immutable.ImmutableDictionary<
    int, System.Collections.Immutable.ImmutableList<
        Phantonia.Historia.Language.FlowAnalysis.FlowEdge>>;
using MutEdges = System.Collections.Generic.Dictionary<
    int, System.Collections.Generic.List<
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

    public ImmutableDictionary<int, FlowVertex> Vertices { get; init; } = ImmutableDictionary<int, FlowVertex>.Empty;

    // a flow graph is conformable if it only has a single start edge which is a story edge
    // what used to be StartVertex
    public bool IsConformable => StartEdges.Where(e => e.IsStory).Count() == 1;

    public int GetStoryStartVertex()
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

    public FlowGraph Append(FlowGraph graph)
    {
        // this method generates a huge amount of waste - can we optimize this?

        MutEdges tempEdges = OutgoingEdges.ToDictionary(p => p.Key, p => p.Value.ToList());
        Dictionary<int, FlowVertex> tempVertices = Vertices.ToDictionary(p => p.Key, p => p.Value);

        foreach ((int currentVertex, List<FlowEdge> edges) in tempEdges)
        {
            FlowEdgeKind kind = FlowEdgeKind.None;

            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i].ToVertex == FinalVertex)
                {
                    kind = edges[i].Kind;
                    edges.RemoveAt(i);
                    break;
                }
            }

            Debug.Assert(edges.All(e => e.ToVertex != FinalVertex));

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

        foreach ((int currentVertex, ImmutableList<FlowEdge> edges) in graph.OutgoingEdges)
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

    public FlowGraph AppendToVertex(int vertex, FlowGraph graph)
    {
        // this method generates a huge amount of waste - can we optimize this?

        if (!Vertices.ContainsKey(vertex))
        {
            throw new ArgumentException($"{nameof(vertex)} is not a vertex of this graph.");
        }

        MutEdges tempEdges = OutgoingEdges.ToDictionary(p => p.Key, p => p.Value.ToList());
        Dictionary<int, FlowVertex> tempVertices = Vertices.ToDictionary(p => p.Key, p => p.Value);

        // add 'graph' into this graph
        foreach ((int currentVertex, ImmutableList<FlowEdge> edges) in graph.OutgoingEdges)
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
        Dictionary<int, FlowVertex> tempVertices = Vertices.ToDictionary(p => p.Key, p => p.Value);

        // add everything from 'graph' to our graph
        foreach ((int currentVertex, ImmutableList<FlowEdge> edges) in graph.OutgoingEdges)
        {
            if (Vertices.ContainsKey(currentVertex))
            {
                throw new InvalidOperationException("Duplicated vertex key");
            }

            tempEdges[currentVertex] = [.. edges];
            tempVertices[currentVertex] = graph.Vertices[currentVertex];
        }

        // turn every edge to 'vertex' into 'graph.StartEdges'
        foreach ((int currentVertex, List<FlowEdge> edges) in tempEdges)
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

        foreach ((int currentVertex, ImmutableList<FlowEdge> edges) in graph.OutgoingEdges)
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

        foreach ((int vertex, ImmutableList<FlowEdge> edges) in OutgoingEdges)
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
        MutEdges tempEdges = OutgoingEdges.ToDictionary(p => p.Key, p => p.Value.ToList());
        Dictionary<int, FlowVertex> tempVertices = Vertices.ToDictionary(p => p.Key, p => p.Value);

        List<int> newStartVertices = [.. StartEdges.Where(e => e.IsStory).Select(e => e.ToVertex)];

        foreach (FlowVertex vertex in Vertices.Values)
        {
            if (vertex.IsVisible)
            {
                continue;
            }

            tempVertices.Remove(vertex.Index);
            tempEdges.Remove(vertex.Index);

            ImmutableList<FlowEdge> outgoingEdges = OutgoingEdges[vertex.Index];

            foreach (int otherVertex in tempVertices.Keys)
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

    public IEnumerable<int> TopologicalSort()
    {
        Dictionary<int, bool> marked = [];

        foreach (int vertex in Vertices.Keys)
        {
            marked[vertex] = false;
        }

        Stack<int> postOrder = [];

        foreach (int vertex in Vertices.Keys)
        {
            if (!marked[vertex])
            {
                DepthFirstSearch(vertex);
            }
        }

        return postOrder;

        void DepthFirstSearch(int vertex)
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
