using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Edges = System.Collections.Immutable.ImmutableDictionary<
    int, System.Collections.Immutable.ImmutableList<int>>;
using MutEdges = System.Collections.Generic.Dictionary<
    int, System.Collections.Generic.List<int>>;

namespace Phantonia.Historia.Language.FlowAnalysis;

public sealed record FlowGraph
{
    public const int EmptyVertex = -1;

    public static FlowGraph Empty { get; } = new();

    public static FlowGraph CreateSimpleFlowGraph(FlowVertex vertex) => Empty with
    {
        StartVertex = vertex.Index,
        OutgoingEdges = Empty.OutgoingEdges.Add(vertex.Index, ImmutableList.Create(EmptyVertex)),
        Vertices = Empty.Vertices.Add(vertex.Index, vertex),
    };

    private FlowGraph() { }

    public Edges OutgoingEdges { get; init; } = Edges.Empty;

    public int StartVertex { get; init; } = EmptyVertex;

    public ImmutableDictionary<int, FlowVertex> Vertices { get; init; } = ImmutableDictionary<int, FlowVertex>.Empty;

    public FlowGraph AddVertex(FlowVertex vertex, params int[] pointedVertices)
    {
        if (Vertices.ContainsKey(vertex.Index))
        {
            throw new ArgumentException("Cannot add a vertex that is already there");
        }

        return this with
        {
            Vertices = Vertices.Add(vertex.Index, vertex),
            OutgoingEdges = OutgoingEdges.Add(vertex.Index, pointedVertices.ToImmutableList()),
            StartVertex = StartVertex == EmptyVertex ? vertex.Index : StartVertex,
        };
    }

    public FlowGraph Append(FlowGraph graph)
    {
        // this method generates a huge amount of waste - can we optimize this?

        MutEdges tempEdges = OutgoingEdges.ToDictionary(p => p.Key, p => p.Value.ToList());
        Dictionary<int, FlowVertex> tempVertices = Vertices.ToDictionary(p => p.Key, p => p.Value);

        foreach ((int currentVertex, List<int> pointedVertices) in tempEdges)
        {
            for (int i = 0; i < pointedVertices.Count; i++)
            {
                if (pointedVertices[i] == EmptyVertex)
                {
                    pointedVertices[i] = graph.StartVertex;
                }
            }
        }

        foreach ((int key, ImmutableList<int> value) in graph.OutgoingEdges)
        {
            if (tempEdges.ContainsKey(key))
            {
                throw new InvalidOperationException("Duplicated vertex key");
            }

            tempEdges[key] = value.ToList();
            tempVertices[key] = graph.Vertices[key];
        }

        return this with
        {
            OutgoingEdges = tempEdges.ToImmutableDictionary(p => p.Key, p => p.Value.ToImmutableList()),
            Vertices = tempVertices.ToImmutableDictionary(),
            StartVertex = StartVertex == EmptyVertex ? graph.StartVertex : StartVertex,
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

        foreach ((int key, ImmutableList<int> value) in graph.OutgoingEdges)
        {
            if (tempEdges.ContainsKey(key))
            {
                throw new InvalidOperationException("Duplicated vertex key");
            }

            tempEdges[key] = value.ToList();
            tempVertices[key] = graph.Vertices[key];
        }

        tempEdges[vertex].Add(graph.StartVertex);

        return this with
        {
            OutgoingEdges = tempEdges.ToImmutableDictionary(p => p.Key, p => p.Value.ToImmutableList()),
            Vertices = tempVertices.ToImmutableDictionary(),
            StartVertex = StartVertex == EmptyVertex ? graph.StartVertex : StartVertex,
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
        foreach ((int currentVertex, ImmutableList<int> pointedVertices) in graph.OutgoingEdges)
        {
            if (Vertices.ContainsKey(currentVertex))
            {
                throw new InvalidOperationException("Duplicated vertex key");
            }

            tempEdges[currentVertex] = pointedVertices.ToList();
            tempVertices[currentVertex] = graph.Vertices[currentVertex];
        }

        // redirect all edges to 'vertex' to 'graph.StartVertex'
        foreach ((int currentVertex, List<int> pointedVertices) in tempEdges)
        {
            for (int i = 0; i < pointedVertices.Count; i++)
            {
                if (pointedVertices[i] == replacedVertex)
                {
                    pointedVertices[i] = graph.StartVertex;
                }
            }
        }

        // redirect all edges to 'graph's EmptyVertex to every vertex that 'replacedVertex' points to
        ImmutableList<int> replacedVertexPointedVertices = OutgoingEdges[replacedVertex];

        foreach ((int currentVertex, ImmutableList<int> pointedVertices) in graph.OutgoingEdges)
        {
            for (int i = 0; i < pointedVertices.Count; i++)
            {
                if (pointedVertices[i] == EmptyVertex)
                {
                    tempEdges[currentVertex].RemoveAt(i);

                    for (int j = 0; j < replacedVertexPointedVertices.Count; j++)
                    {
                        tempEdges[currentVertex].Add(replacedVertexPointedVertices[j]);
                    }
                }
            }
        }

        tempVertices.Remove(replacedVertex);
        tempEdges.Remove(replacedVertex);

        return this with
        {
            OutgoingEdges = tempEdges.ToImmutableDictionary(p => p.Key, p => p.Value.ToImmutableList()),
            Vertices = tempVertices.ToImmutableDictionary(),
            StartVertex = StartVertex == replacedVertex ? graph.StartVertex : StartVertex,
        };
    }

    public FlowGraph Reverse()
    {
        MutEdges edges = new();

        foreach ((int vertex, ImmutableList<int> pointedVertices) in OutgoingEdges)
        {
            foreach (int pointedVertex in pointedVertices)
            {
                if (!edges.ContainsKey(pointedVertex))
                {
                    edges[pointedVertex] = new List<int>();
                }

                edges[pointedVertex].Add(vertex);
            }
        }

        return new FlowGraph
        {
            Vertices = Vertices,
            OutgoingEdges = edges.ToImmutableDictionary(p => p.Key, p => p.Value.ToImmutableList()),
            StartVertex = StartVertex,
        };
    }

    public IEnumerable<int> TopologicalSort()
    {
        Dictionary<int, bool> marked = new();

        foreach (int vertex in Vertices.Keys)
        {
            marked[vertex] = false;
        }

        Stack<int> postOrder = new();

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

            foreach (int adjacentVertex in OutgoingEdges[vertex])
            {
                if (adjacentVertex != EmptyVertex && !marked[adjacentVertex])
                {
                    DepthFirstSearch(adjacentVertex);
                }
            }

            postOrder.Push(vertex);
        }
    }
}
