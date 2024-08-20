using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed class DependencyGraph
{
    public DependencyGraph() { }

    public required IReadOnlyDictionary<int, Symbol> Symbols { get; init; }

    public required IReadOnlyDictionary<int, IReadOnlyList<int>> Dependencies { get; init; }

    public bool IsCyclic([NotNullWhen(returnValue: true)] out IEnumerable<int>? cycle)
    {
        Stack<int>? cycleStack = null;
        Dictionary<int, VertexData> vertexData = [];

        foreach (int vertex in Symbols.Keys)
        {
            vertexData[vertex] = new VertexData();
        }

        foreach (int vertex in Symbols.Keys)
        {
            if (!vertexData[vertex].Marked)
            {
                DepthFirstSearch(vertex);
            }
        }

        cycle = cycleStack;
        return cycle is not null;

        void DepthFirstSearch(int vertex)
        {
            vertexData[vertex] = vertexData[vertex] with { OnStack = true, Marked = true };

            foreach (int adjacentVertex in Dependencies[vertex])
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
                    cycleStack = new Stack<int>();

                    for (int x = vertex; x != adjacentVertex; x = vertexData[x].EdgeTo)
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

    public IEnumerable<int> TopologicalSort()
    {
        Debug.Assert(!IsCyclic(out _));

        Dictionary<int, bool> marked = [];

        foreach (int vertex in Symbols.Keys)
        {
            marked[vertex] = false;
        }

        Stack<int> postOrder = new();

        foreach (int vertex in Symbols.Keys)
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

            foreach (int adjacentVertex in Dependencies[vertex])
            {
                if (!marked[adjacentVertex])
                {
                    DepthFirstSearch(adjacentVertex);
                }
            }

            postOrder.Push(vertex);
        }
    }

    public IEnumerable<int> GetDependencyRespectingOrder()
    {
        IEnumerable<int> order = TopologicalSort();
        Debug.Assert(order is Stack<int>);

        Stack<int> stack = (Stack<int>)order;

        Stack<int> newOrder = new();

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
        public int EdgeTo { get; init; } = -1;
        public bool OnStack { get; init; } = false;
    }
}
