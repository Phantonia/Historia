using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia;

public sealed class StoryGraph<TOutput, TOption>(IReadOnlyDictionary<long, StoryVertex<TOutput, TOption>> vertices, IReadOnlyList<StoryEdge> startEdges)
{
    public const long StartVertex = -2;
    public const long FinalVertex = -1;

    public IReadOnlyDictionary<long, StoryVertex<TOutput, TOption>> Vertices { get; } = vertices;

    public IReadOnlyList<StoryEdge> StartEdges { get; } = startEdges;

    public bool ContainsEdge(long start, long end)
    {
        bool result = false;

        foreach (StoryEdge edge in Vertices[start].OutgoingEdges)
        {
            if (edge.ToVertex == end)
            {
                result = true;
                break;
            }
        }

        Debug.Assert(result == Vertices[end].IncomingEdges.Any(e => e.FromVertex == start));

        return result;
    }

    public IEnumerable<long> TopologicalSort()
    {
        // adapted from FlowGraph equivalent
        Dictionary<long, bool> marked = [];

        foreach (long vertex in Vertices.Keys)
        {
            marked[vertex] = false;
        }

        Stack<long> postOrder = new();

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

            foreach (StoryEdge edge in Vertices[vertex].OutgoingEdges.Reverse())
            {
                if (!edge.IsWeak && edge.ToVertex != FinalVertex && !marked[edge.ToVertex])
                {
                    DepthFirstSearch(edge.ToVertex);
                }
            }

            postOrder.Push(vertex);
        }
    }

    public T Induce<T>(Func<IEnumerable<T>, T> inductor, T baseValue)
    {
        Dictionary<long, T> values = [];

        foreach (StoryEdge edge in StartEdges)
        {
            values[edge.ToVertex] = baseValue;
        }

        IEnumerable<long> topologicalSort = TopologicalSort();

        List<T> predecessorValues = [];

        foreach (long vertex in topologicalSort)
        {
            if (values.ContainsKey(vertex))
            {
                continue;
            }

            foreach (StoryEdge incomingEdge in Vertices[vertex].IncomingEdges)
            {
                predecessorValues.Add(values[incomingEdge.FromVertex]);
            }

            values[vertex] = inductor(predecessorValues);

            predecessorValues.Clear();
        }

        foreach (StoryEdge incomingEdge in Vertices[FinalVertex].IncomingEdges)
        {
            predecessorValues.Add(values[incomingEdge.FromVertex]);
        }

        return inductor(predecessorValues);
    }
}
