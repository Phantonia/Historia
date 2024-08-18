using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia;

public sealed class StoryGraph<TOutput, TOption>(IReadOnlyDictionary<int, StoryVertex<TOutput, TOption>> vertices, IReadOnlyList<StoryEdge> startEdges)
{
    public const int FinalVertex = -1;

    public IReadOnlyDictionary<int, StoryVertex<TOutput, TOption>> Vertices { get; } = vertices;

    public IReadOnlyList<StoryEdge> StartEdges { get; } = startEdges;

    public bool ContainsEdge(int start, int end)
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

    public IEnumerable<int> TopologicalSort()
    {
        // adapted from FlowGraph equivalent
        Dictionary<int, bool> marked = [];

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
        Dictionary<int, T> values = [];

        IEnumerable<int> topologicalSort = TopologicalSort();

        List<T> predecessorValues = [];

        foreach (int vertex in topologicalSort)
        {
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
