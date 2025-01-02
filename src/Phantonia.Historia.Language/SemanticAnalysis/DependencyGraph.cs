using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed class DependencyGraph
{
    public DependencyGraph() { }

    public required IReadOnlyDictionary<long, Symbol> Symbols { get; init; }

    public required IReadOnlyDictionary<long, IReadOnlySet<long>> Dependencies { get; init; }

    public bool IsCyclic([NotNullWhen(returnValue: true)] out IEnumerable<long>? cycle)
    {
        Stack<long>? cycleStack = null;
        Dictionary<long, VertexData> vertexData = [];

        foreach (long vertex in Symbols.Keys)
        {
            vertexData[vertex] = new VertexData();
        }

        foreach (long vertex in Symbols.Keys)
        {
            if (!vertexData[vertex].Marked)
            {
                DepthFirstSearch(vertex);
            }
        }

        cycle = cycleStack;
        return cycle is not null;

        void DepthFirstSearch(long vertex)
        {
            vertexData[vertex] = vertexData[vertex] with { OnStack = true, Marked = true };

            foreach (long adjacentVertex in Dependencies[vertex])
            {
                if (cycleStack is not null)
                {
                    return;
                }

                if (!vertexData[adjacentVertex].Marked)
                {
                    vertexData[adjacentVertex] = vertexData[adjacentVertex] with { EdgeTo = vertex };
                    DepthFirstSearch(adjacentVertex);
                }
                else if (vertexData[adjacentVertex].OnStack)
                {
                    cycleStack = new Stack<long>();

                    for (long x = vertex; x != adjacentVertex; x = vertexData[x].EdgeTo)
                    {
                        cycleStack.Push(x);
                    }

                    cycleStack.Push(adjacentVertex);
                    cycleStack.Push(vertex);
                }
            }

            vertexData[vertex] = vertexData[vertex] with { OnStack = false };
        }
    }

    public IEnumerable<long> TopologicalSort()
    {
        Debug.Assert(!IsCyclic(out _));

        Dictionary<long, bool> marked = [];

        foreach (long vertex in Symbols.Keys)
        {
            marked[vertex] = false;
        }

        Stack<long> postOrder = new();

        foreach (long vertex in Symbols.Keys)
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

            foreach (long adjacentVertex in Dependencies[vertex])
            {
                if (!marked[adjacentVertex])
                {
                    DepthFirstSearch(adjacentVertex);
                }
            }

            postOrder.Push(vertex);
        }
    }

    public IEnumerable<long> GetDependencyRespectingOrder()
    {
        IEnumerable<long> order = TopologicalSort();
        Debug.Assert(order is Stack<long>);

        Stack<long> stack = (Stack<long>)order;

        Stack<long> newOrder = new();

        // reverse stack
        while (stack.Count > 0)
        {
            newOrder.Push(stack.Pop());
        }

        return newOrder;
    }

    private readonly record struct VertexData
    {
        public VertexData() { }

        public bool Marked { get; init; } = false;
        public long EdgeTo { get; init; } = -1;
        public bool OnStack { get; init; } = false;
    }
}
