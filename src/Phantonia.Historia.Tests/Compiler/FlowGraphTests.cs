using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis.Statements;
using System.Diagnostics.CodeAnalysis;
using System;
using System.Linq;

namespace Phantonia.Historia.Tests.Compiler;

[TestClass]
public sealed class FlowGraphTests
{
    private sealed record Stub : StatementNode
    {
        [SetsRequiredMembers]
        public Stub()
        {
            Index = 0;
        }
    }

    [TestMethod]
    public void TestSimpleFlowGraph()
    {
        FlowGraph simpleGraph = FlowGraph.CreateSimpleFlowGraph(new FlowVertex { Index = 42, AssociatedStatement = new Stub() });

        Assert.AreEqual(42, simpleGraph.StartVertex);
        Assert.AreEqual(1, simpleGraph.OutgoingEdges.Count);
        Assert.IsTrue(simpleGraph.OutgoingEdges.ContainsKey(42));
        Assert.AreEqual(1, simpleGraph.OutgoingEdges[42].Count);
        Assert.AreEqual(FlowGraph.EmptyVertex, simpleGraph.OutgoingEdges[42][0]);
    }

    [TestMethod]
    public void TestSimpleAppend()
    {
        FlowGraph fgA = FlowGraph.CreateSimpleFlowGraph(new FlowVertex { Index = 42, AssociatedStatement = new Stub() });
        FlowGraph fgB = FlowGraph.CreateSimpleFlowGraph(new FlowVertex { Index = 64, AssociatedStatement = new Stub() });

        FlowGraph resultGraph = fgA.Append(fgB);

        Assert.AreEqual(42, resultGraph.StartVertex);
        Assert.AreEqual(2, resultGraph.OutgoingEdges.Count);

        Assert.AreEqual(1, resultGraph.OutgoingEdges[42].Count);
        Assert.AreEqual(64, resultGraph.OutgoingEdges[42][0]);

        Assert.AreEqual(1, resultGraph.OutgoingEdges[64].Count);
        Assert.AreEqual(FlowGraph.EmptyVertex, resultGraph.OutgoingEdges[64][0]);
    }

    [TestMethod]
    public void TestEmptyAppend()
    {
        FlowGraph graph = FlowGraph.CreateSimpleFlowGraph(new FlowVertex { Index = 42 , AssociatedStatement = new Stub() });

        Assert.AreEqual(FlowGraph.EmptyVertex, FlowGraph.Empty.StartVertex);

        FlowGraph otherGraph = FlowGraph.Empty.Append(graph);

        Assert.AreEqual(42, otherGraph.StartVertex);

        Assert.AreEqual(1, otherGraph.Vertices.Count);
        Assert.AreEqual(1, otherGraph.OutgoingEdges.Count);

        Assert.AreEqual(1, otherGraph.OutgoingEdges[42].Count);
        Assert.AreEqual(FlowGraph.EmptyVertex, otherGraph.OutgoingEdges[42][0]);
    }

    [TestMethod]
    public void TestVertices()
    {
        FlowGraph graph = FlowGraph.CreateSimpleFlowGraph(new FlowVertex { Index = 0 , AssociatedStatement = new Stub() });

        for (int i = 1; i < 8; i++)
        {
            graph = graph.Append(FlowGraph.CreateSimpleFlowGraph(new FlowVertex { Index = i, AssociatedStatement = new Stub() }));
        }

        for (int i = 0; i < 8; i++)
        {
            Assert.AreEqual(i, graph.Vertices[i].Index);
        }
    }

    [TestMethod]
    public void TestMoreComplexGraph()
    {
        FlowGraph graph = FlowGraph.Empty.AddVertex(new FlowVertex { Index = 5, AssociatedStatement = new Stub() }, 6, 7)
                                         .AddVertex(new FlowVertex { Index = 6, AssociatedStatement = new Stub() }, 7, FlowGraph.EmptyVertex)
                                         .AddVertex(new FlowVertex { Index = 7 , AssociatedStatement = new Stub() }, 8)
                                         .AddVertex(new FlowVertex { Index = 8 , AssociatedStatement = new Stub() }, 6, FlowGraph.EmptyVertex);

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
        FlowGraph graphA = FlowGraph.Empty.AddVertex(new FlowVertex { Index = 5 , AssociatedStatement = new Stub() }, 6, 7)
                                          .AddVertex(new FlowVertex { Index = 6 , AssociatedStatement = new Stub() }, 7, FlowGraph.EmptyVertex)
                                          .AddVertex(new FlowVertex { Index = 7 , AssociatedStatement = new Stub() }, 8)
                                          .AddVertex(new FlowVertex { Index = 8 , AssociatedStatement = new Stub() }, 6, FlowGraph.EmptyVertex);

        FlowGraph graphB = FlowGraph.Empty.AddVertex(new FlowVertex { Index = 9 , AssociatedStatement = new Stub() }, 10, 11)
                                          .AddVertex(new FlowVertex { Index = 10 , AssociatedStatement = new Stub() }, FlowGraph.EmptyVertex)
                                          .AddVertex(new FlowVertex { Index = 11 , AssociatedStatement = new Stub() }, FlowGraph.EmptyVertex);

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

    [TestMethod]
    public void TestAppendToVertex()
    {
        FlowGraph flowGraph = FlowGraph.Empty.AddVertex(new FlowVertex { Index = 0 , AssociatedStatement = new Stub() }, 1)
                                             .AddVertex(new FlowVertex { Index = 1 , AssociatedStatement = new Stub() }, FlowGraph.EmptyVertex);

        FlowGraph nestedGraph = FlowGraph.Empty.AddVertex(new FlowVertex { Index = 2 , AssociatedStatement = new Stub() }, 3, 4)
                                               .AddVertex(new FlowVertex { Index = 3 , AssociatedStatement = new Stub() }, 4)
                                               .AddVertex(new FlowVertex { Index = 4 , AssociatedStatement = new Stub() }, FlowGraph.EmptyVertex);

        FlowGraph resultGraph = flowGraph.AppendToVertex(0, nestedGraph);

        Assert.AreEqual(0, resultGraph.StartVertex);

        Assert.AreEqual(5, resultGraph.Vertices.Count);
        Assert.AreEqual(2, resultGraph.OutgoingEdges[0].Count);
        Assert.AreEqual(1, resultGraph.OutgoingEdges[0][0]);
        Assert.AreEqual(2, resultGraph.OutgoingEdges[0][1]);

        Assert.AreEqual(2, resultGraph.OutgoingEdges[2].Count);
        Assert.AreEqual(3, resultGraph.OutgoingEdges[2][0]);
        Assert.AreEqual(4, resultGraph.OutgoingEdges[2][1]);
    }
}
