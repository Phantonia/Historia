using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language.Flow;
using System.Collections.Immutable;
using System.Linq;
using Edges = System.Collections.Immutable.ImmutableDictionary<
    int, System.Collections.Immutable.ImmutableList<int>>;
using MutEdges = System.Collections.Generic.Dictionary<
    int, System.Collections.Generic.List<int>>;

namespace Phantonia.Historia.Tests.Compiler;

[TestClass]
public sealed class FlowGraphTests
{
    [TestMethod]
    public void TestSimpleFlowGraph()
    {
        FlowGraph simpleGraph = FlowGraph.CreateSimpleFlowGraph(new FlowVertex { Index = 42 });

        Assert.AreEqual(42, simpleGraph.StartVertex);
        Assert.AreEqual(1, simpleGraph.OutgoingEdges.Count);
        Assert.IsTrue(simpleGraph.OutgoingEdges.ContainsKey(42));
        Assert.AreEqual(1, simpleGraph.OutgoingEdges[42].Count);
        Assert.AreEqual(FlowGraph.EmptyVertex, simpleGraph.OutgoingEdges[42][0]);
    }

    [TestMethod]
    public void TestSimpleAppend()
    {
        FlowGraph fgA = FlowGraph.CreateSimpleFlowGraph(new FlowVertex { Index = 42 });
        FlowGraph fgB = FlowGraph.CreateSimpleFlowGraph(new FlowVertex { Index = 64 });

        FlowGraph resultGraph = fgA.Append(fgB);

        Assert.AreEqual(42, resultGraph.StartVertex);
        Assert.AreEqual(2, resultGraph.OutgoingEdges.Count);

        Assert.AreEqual(1, resultGraph.OutgoingEdges[42].Count);
        Assert.AreEqual(64, resultGraph.OutgoingEdges[42][0]);

        Assert.AreEqual(1, resultGraph.OutgoingEdges[64].Count);
        Assert.AreEqual(FlowGraph.EmptyVertex, resultGraph.OutgoingEdges[64][0]);
    }

    [TestMethod]
    public void TestVertices()
    {
        FlowGraph graph = FlowGraph.CreateSimpleFlowGraph(new FlowVertex { Index = 0 });

        for (int i = 1; i < 8; i++)
        {
            graph = graph.Append(FlowGraph.CreateSimpleFlowGraph(new FlowVertex { Index = i }));
        }

        for (int i = 0; i < 8; i++)
        {
            Assert.AreEqual(i, graph.Vertices[i].Index);
        }
    }

    [TestMethod]
    public void TestMoreComplexGraph()
    {
        FlowGraph graph = FlowGraph.Empty.AddVertex(new FlowVertex { Index = 5 }, 6, 7)
                                         .AddVertex(new FlowVertex { Index = 6 }, 7, FlowGraph.EmptyVertex)
                                         .AddVertex(new FlowVertex { Index = 7 }, 8)
                                         .AddVertex(new FlowVertex { Index = 8 }, 6, FlowGraph.EmptyVertex);

        Assert.AreEqual(5, graph.StartVertex);

        Assert.AreEqual(4, graph.Vertices.Count);
        Assert.AreEqual(4, graph.OutgoingEdges.Count);

        foreach ((int leftKey, int rightKey) in graph.OutgoingEdges.Keys.OrderBy(k => k).Zip(graph.Vertices.Keys.OrderBy(k => k)))
        {
            Assert.AreEqual(leftKey, rightKey);
            Assert.AreEqual(rightKey, graph.Vertices[rightKey].Index);
        }

        Assert.AreEqual(2, graph.OutgoingEdges[5].Count);
        Assert.AreEqual(2, graph.OutgoingEdges[6].Count);
        Assert.AreEqual(1, graph.OutgoingEdges[7].Count);
        Assert.AreEqual(2, graph.OutgoingEdges[8].Count);

        Assert.AreEqual(6, graph.OutgoingEdges[5][0]);
        Assert.AreEqual(7, graph.OutgoingEdges[5][1]);
        Assert.AreEqual(7, graph.OutgoingEdges[6][0]);
        Assert.AreEqual(FlowGraph.EmptyVertex, graph.OutgoingEdges[6][1]);
        Assert.AreEqual(8, graph.OutgoingEdges[7][0]);
        Assert.AreEqual(6, graph.OutgoingEdges[8][0]);
        Assert.AreEqual(FlowGraph.EmptyVertex, graph.OutgoingEdges[8][1]);
    }

    [TestMethod]
    public void TestReplace()
    {
        FlowGraph graphA = FlowGraph.Empty.AddVertex(new FlowVertex { Index = 5 }, 6, 7)
                                          .AddVertex(new FlowVertex { Index = 6 }, 7, FlowGraph.EmptyVertex)
                                          .AddVertex(new FlowVertex { Index = 7 }, 8)
                                          .AddVertex(new FlowVertex { Index = 8 }, 6, FlowGraph.EmptyVertex);

        FlowGraph graphB = FlowGraph.Empty.AddVertex(new FlowVertex { Index = 9 }, 10, 11)
                                          .AddVertex(new FlowVertex { Index = 10 }, FlowGraph.EmptyVertex)
                                          .AddVertex(new FlowVertex { Index = 11 }, FlowGraph.EmptyVertex);

        FlowGraph graphC = graphA.Replace(5, graphB);

        Assert.AreEqual(6, graphC.Vertices.Count);
        Assert.AreEqual(6, graphC.OutgoingEdges.Count);

        foreach ((int leftKey, int rightKey) in graphC.OutgoingEdges.Keys.OrderBy(k => k).Zip(graphC.Vertices.Keys.OrderBy(k => k)))
        {
            Assert.AreEqual(leftKey, rightKey);
            Assert.AreEqual(rightKey, graphC.Vertices[rightKey].Index);
        }

        Assert.AreEqual(9, graphC.StartVertex);

        void AssertHasEdges(int vertex, params int[] edges)
        {
            Assert.IsTrue(edges.SequenceEqual(graphC.OutgoingEdges[vertex].OrderBy(v => v)));
        }

        AssertHasEdges(9, 10, 11);
        AssertHasEdges(10, 6, 7);
        AssertHasEdges(11, 6, 7);
        AssertHasEdges(6, FlowGraph.EmptyVertex, 7);
        AssertHasEdges(7, 8);
        AssertHasEdges(8, FlowGraph.EmptyVertex, 6);
    }
}
