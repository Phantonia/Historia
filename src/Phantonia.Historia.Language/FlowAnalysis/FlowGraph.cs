using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

    public static readonly FlowEdge FinalEdge = new()
    {
        ToVertex = FinalVertex,
        Weak = false,
    };

    public static FlowGraph Empty { get; } = new();

    public static FlowGraph CreateSimpleFlowGraph(FlowVertex vertex) => Empty with
    {
        StartVertex = vertex.Index,
        OutgoingEdges = Empty.OutgoingEdges.Add(vertex.Index, ImmutableList.Create(FinalEdge)),
        Vertices = Empty.Vertices.Add(vertex.Index, vertex),
    };

    private FlowGraph() { }

    public Edges OutgoingEdges { get; init; } = Edges.Empty;

    public int StartVertex { get; init; } = FinalVertex;

    public ImmutableDictionary<int, FlowVertex> Vertices { get; init; } = ImmutableDictionary<int, FlowVertex>.Empty;

    public FlowGraph AddVertex(FlowVertex vertex, params FlowEdge[] edges)
    {
        if (Vertices.ContainsKey(vertex.Index))
        {
            throw new ArgumentException("Cannot add a vertex that is already there");
        }

        return this with
        {
            Vertices = Vertices.Add(vertex.Index, vertex),
            OutgoingEdges = OutgoingEdges.Add(vertex.Index, edges.ToImmutableList()),
            StartVertex = StartVertex == FinalVertex ? vertex.Index : StartVertex,
        };
    }

    public FlowGraph Append(FlowGraph graph)
    {
        // this method generates a huge amount of waste - can we optimize this?

        MutEdges tempEdges = OutgoingEdges.ToDictionary(p => p.Key, p => p.Value.ToList());
        Dictionary<int, FlowVertex> tempVertices = Vertices.ToDictionary(p => p.Key, p => p.Value);

        foreach ((int currentVertex, List<FlowEdge> edges) in tempEdges)
        {
            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i].ToVertex == FinalVertex)
                {
                    edges[i] = FlowEdge.CreateTo(graph.StartVertex);
                }
            }
        }

        foreach ((int currentVertex, ImmutableList<FlowEdge> edges) in graph.OutgoingEdges)
        {
            if (tempEdges.ContainsKey(currentVertex))
            {
                throw new InvalidOperationException("Duplicated vertex key");
            }

            tempEdges[currentVertex] = edges.ToList();
            tempVertices[currentVertex] = graph.Vertices[currentVertex];
        }

        return this with
        {
            OutgoingEdges = tempEdges.ToImmutableDictionary(p => p.Key, p => p.Value.ToImmutableList()),
            Vertices = tempVertices.ToImmutableDictionary(),
            StartVertex = StartVertex == FinalVertex ? graph.StartVertex : StartVertex,
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

        foreach ((int currentVertex, ImmutableList<FlowEdge> edges) in graph.OutgoingEdges)
        {
            if (tempEdges.ContainsKey(currentVertex))
            {
                throw new InvalidOperationException("Duplicated vertex key");
            }

            tempEdges[currentVertex] = edges.ToList();
            tempVertices[currentVertex] = graph.Vertices[currentVertex];
        }

        tempEdges[vertex].Add(FlowEdge.CreateTo(graph.StartVertex));

        return this with
        {
            OutgoingEdges = tempEdges.ToImmutableDictionary(p => p.Key, p => p.Value.ToImmutableList()),
            Vertices = tempVertices.ToImmutableDictionary(),
            StartVertex = StartVertex == FinalVertex ? graph.StartVertex : StartVertex,
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

            tempEdges[currentVertex] = edges.ToList();
            tempVertices[currentVertex] = graph.Vertices[currentVertex];
        }

        // redirect all edges to 'vertex' to 'graph.StartVertex'
        foreach ((int currentVertex, List<FlowEdge> edges) in tempEdges)
        {
            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i].ToVertex == replacedVertex)
                {
                    edges[i] = FlowEdge.CreateTo(graph.StartVertex);
                }
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
        MutEdges tempEdges = new();

        foreach ((int vertex, ImmutableList<FlowEdge> edges) in OutgoingEdges)
        {
            foreach (FlowEdge edge in edges)
            {
                if (!tempEdges.ContainsKey(edge.ToVertex))
                {
                    tempEdges[edge.ToVertex] = new List<FlowEdge>();
                }

                tempEdges[edge.ToVertex].Add(new FlowEdge { ToVertex = vertex, Weak = edge.Weak });
            }
        }

        return new FlowGraph
        {
            Vertices = Vertices,
            OutgoingEdges = tempEdges.ToImmutableDictionary(p => p.Key, p => p.Value.ToImmutableList()),
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

            foreach (FlowEdge edge in OutgoingEdges[vertex])
            {
                if (!edge.Weak && edge.ToVertex != FinalVertex && !marked[edge.ToVertex])
                {
                    DepthFirstSearch(edge.ToVertex);
                }
            }

            postOrder.Push(vertex);
        }
    }
}
