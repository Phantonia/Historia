using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia;

/// <summary>
/// Represents the flow graph of a story.
/// </summary>
/// <typeparam name="TOutput">The output type.</typeparam>
/// <typeparam name="TOption">The option type.</typeparam>
/// <param name="vertices">The set of vertices.</param>
/// <param name="startEdges">The set of start edges.</param>
public sealed class StoryGraph<TOutput, TOption>(IReadOnlyDictionary<long, StoryVertex<TOutput, TOption>> vertices, IReadOnlyList<StoryEdge> startEdges)
{
    /// <summary>
    /// The imaginary start vertex.
    /// </summary>
    public const long StartVertex = -2;

    /// <summary>
    /// The imaginary final vertex.
    /// </summary>
    public const long FinalVertex = -1;

    /// <summary>
    /// The set of vertices, as a map from vertex index to vertex object.
    /// </summary>
    public IReadOnlyDictionary<long, StoryVertex<TOutput, TOption>> Vertices { get; } = vertices;

    /// <summary>
    /// The set of start edges, i.e. edges that eminate from <see cref="StartVertex"/>.
    /// </summary>
    public IReadOnlyList<StoryEdge> StartEdges { get; } = startEdges;

    /// <summary>
    /// Determines if the given edge exists in the graph.
    /// </summary>
    /// <param name="start">The start point of the edge.</param>
    /// <param name="end">The end point of the edge.</param>
    /// <returns>True or False.</returns>
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

    /// <summary>
    /// Sorts the vertices in a way, where vertex u comes before v in the order if and only if there is a path from u to v in the graph.
    /// </summary>
    /// <returns>A topological ordering.</returns>
    /// <remarks>Weak edges are ignored for this.</remarks>
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

    /// <summary>
    /// Runs a dynamic programming algorithm over the graph by calculating a value for each vertex that comes from all the value of its preceding vertices.
    /// </summary>
    /// <typeparam name="T">The type of these values.</typeparam>
    /// <param name="inductor">The function to calculate a vertex's value from the values of its predecessors.</param>
    /// <param name="baseValue">The value for all vertices that have one of the <see cref="StartEdges"/> pointing to it.</param>
    /// <returns>The combined value of all vertices that point to <see cref="FinalVertex"/>.</returns>
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
