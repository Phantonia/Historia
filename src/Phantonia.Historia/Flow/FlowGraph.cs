using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using Edges = System.Collections.Immutable.ImmutableDictionary<
    int, System.Collections.Immutable.ImmutableList<int>>;
using MutEdges = System.Collections.Generic.Dictionary<
    int, System.Collections.Generic.List<int>>;

namespace Phantonia.Historia.Language.Flow;

public sealed record FlowGraph
{
    public const int EmptyNode = -1;

    public static FlowGraph Empty { get; } = new();

    public static FlowGraph CreateSimpleFlowGraph(FlowVertex vertex) => Empty with
    {
        StartVertex = vertex.Index,
        OutgoingEdges = Empty.OutgoingEdges.Add(vertex.Index, ImmutableList.Create(EmptyNode)),
        Vertices = Empty.Vertices.Add(vertex.Index, vertex),
    };

    private FlowGraph() { }

    public Edges OutgoingEdges { get; init; } = Edges.Empty;

    public int StartVertex { get; init; } = EmptyNode;

    public ImmutableDictionary<int, FlowVertex> Vertices { get; init; } = ImmutableDictionary<int, FlowVertex>.Empty;

    public FlowGraph Append(FlowGraph graph)
    {
        // this method generates a huge amount of waste - can we optimize this?

        MutEdges tempEdges = OutgoingEdges.ToDictionary(p => p.Key, p => p.Value.ToList());
        Dictionary<int, FlowVertex> tempVertices = Vertices.ToDictionary(p => p.Key, p => p.Value);

        foreach ((int key, List<int> value) in tempEdges)
        {
            for (int i = 0; i < value.Count; i++)
            {
                if (value[i] == EmptyNode)
                {
                    value[i] = graph.StartVertex;
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
        };
    }
}
