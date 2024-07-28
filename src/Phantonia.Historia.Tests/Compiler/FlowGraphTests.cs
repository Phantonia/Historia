using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        public override IEnumerable<SyntaxNode> Children => Enumerable.Empty<SyntaxNode>();

        protected override string GetDebuggerDisplay() => "stub";
    }

    [TestMethod]
    public void TestSimpleFlowGraph()
    {
        FlowGraph simpleGraph = FlowGraph.CreateSimpleFlowGraph(new FlowVertex { Index = 42, AssociatedStatement = new Stub(), IsVisible = true });

        Assert.AreEqual(42, simpleGraph.StartVertex);
        Assert.AreEqual(1, simpleGraph.OutgoingEdges.Count);
        Assert.IsTrue(simpleGraph.OutgoingEdges.ContainsKey(42));
        Assert.AreEqual(1, simpleGraph.OutgoingEdges[42].Count);
        Assert.AreEqual(FlowGraph.FinalVertex, simpleGraph.OutgoingEdges[42][0].ToVertex);
    }

    [TestMethod]
    public void TestSimpleAppend()
    {
        FlowGraph fgA = FlowGraph.CreateSimpleFlowGraph(new FlowVertex { Index = 42, AssociatedStatement = new Stub(), IsVisible = true });
        FlowGraph fgB = FlowGraph.CreateSimpleFlowGraph(new FlowVertex { Index = 64, AssociatedStatement = new Stub(), IsVisible = true });

        FlowGraph resultGraph = fgA.Append(fgB);

        Assert.AreEqual(42, resultGraph.StartVertex);
        Assert.AreEqual(2, resultGraph.OutgoingEdges.Count);

        Assert.AreEqual(1, resultGraph.OutgoingEdges[42].Count);
        Assert.AreEqual(64, resultGraph.OutgoingEdges[42][0].ToVertex);

        Assert.AreEqual(1, resultGraph.OutgoingEdges[64].Count);
        Assert.AreEqual(FlowGraph.FinalVertex, resultGraph.OutgoingEdges[64][0].ToVertex);
    }

    [TestMethod]
    public void TestEmptyAppend()
    {
        FlowGraph graph = FlowGraph.CreateSimpleFlowGraph(new FlowVertex { Index = 42, AssociatedStatement = new Stub(), IsVisible = true });

        Assert.AreEqual(FlowGraph.FinalVertex, FlowGraph.Empty.StartVertex);

        FlowGraph otherGraph = FlowGraph.Empty.Append(graph);

        Assert.AreEqual(42, otherGraph.StartVertex);

        Assert.AreEqual(1, otherGraph.Vertices.Count);
        Assert.AreEqual(1, otherGraph.OutgoingEdges.Count);

        Assert.AreEqual(1, otherGraph.OutgoingEdges[42].Count);
        Assert.AreEqual(FlowGraph.FinalVertex, otherGraph.OutgoingEdges[42][0].ToVertex);
    }

    [TestMethod]
    public void TestVertices()
    {
        FlowGraph graph = FlowGraph.CreateSimpleFlowGraph(new FlowVertex { Index = 0, AssociatedStatement = new Stub(), IsVisible = true });

        for (int i = 1; i < 8; i++)
        {
            graph = graph.Append(FlowGraph.CreateSimpleFlowGraph(new FlowVertex { Index = i, AssociatedStatement = new Stub(), IsVisible = true }));
        }

        for (int i = 0; i < 8; i++)
        {
            Assert.AreEqual(i, graph.Vertices[i].Index);
        }
    }

    [TestMethod]
    public void TestMoreComplexGraph()
    {
        FlowGraph graph = FlowGraph.Empty.AddVertex(new FlowVertex { Index = 5, AssociatedStatement = new Stub(), IsVisible = true }, FlowEdge.CreateTo(6), FlowEdge.CreateTo(7))
                                         .AddVertex(new FlowVertex { Index = 6, AssociatedStatement = new Stub(), IsVisible = true }, FlowEdge.CreateTo(7), FlowGraph.FinalEdge)
                                         .AddVertex(new FlowVertex { Index = 7, AssociatedStatement = new Stub(), IsVisible = true }, FlowEdge.CreateTo(8))
                                         .AddVertex(new FlowVertex { Index = 8, AssociatedStatement = new Stub(), IsVisible = true }, FlowEdge.CreateTo(6), FlowGraph.FinalEdge);

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

        Assert.AreEqual(6, graph.OutgoingEdges[5][0].ToVertex);
        Assert.AreEqual(7, graph.OutgoingEdges[5][1].ToVertex);
        Assert.AreEqual(7, graph.OutgoingEdges[6][0].ToVertex);
        Assert.AreEqual(FlowGraph.FinalVertex, graph.OutgoingEdges[6][1].ToVertex);
        Assert.AreEqual(8, graph.OutgoingEdges[7][0].ToVertex);
        Assert.AreEqual(6, graph.OutgoingEdges[8][0].ToVertex);
        Assert.AreEqual(FlowGraph.FinalVertex, graph.OutgoingEdges[8][1].ToVertex);
    }

    [TestMethod]
    public void TestReplace()
    {
        FlowGraph graphA = FlowGraph.Empty.AddVertex(new FlowVertex { Index = 5, AssociatedStatement = new Stub(), IsVisible = true }, FlowEdge.CreateTo(6), FlowEdge.CreateTo(7))
                                          .AddVertex(new FlowVertex { Index = 6, AssociatedStatement = new Stub(), IsVisible = true }, FlowEdge.CreateTo(7), FlowGraph.FinalEdge)
                                          .AddVertex(new FlowVertex { Index = 7, AssociatedStatement = new Stub(), IsVisible = true }, FlowEdge.CreateTo(8))
                                          .AddVertex(new FlowVertex { Index = 8, AssociatedStatement = new Stub(), IsVisible = true }, FlowEdge.CreateTo(6), FlowGraph.FinalEdge);

        FlowGraph graphB = FlowGraph.Empty.AddVertex(new FlowVertex { Index = 9, AssociatedStatement = new Stub(), IsVisible = true }, FlowEdge.CreateTo(10), FlowEdge.CreateTo(11))
                                          .AddVertex(new FlowVertex { Index = 10, AssociatedStatement = new Stub(), IsVisible = true }, FlowGraph.FinalEdge)
                                          .AddVertex(new FlowVertex { Index = 11, AssociatedStatement = new Stub(), IsVisible = true }, FlowGraph.FinalEdge);

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
            Assert.IsTrue(edges.SequenceEqual(graphC.OutgoingEdges[vertex].Select(e => e.ToVertex).OrderBy(v => v)));
        }

        AssertHasEdges(9, 10, 11);
        AssertHasEdges(10, 6, 7);
        AssertHasEdges(11, 6, 7);
        AssertHasEdges(6, FlowGraph.FinalVertex, 7);
        AssertHasEdges(7, 8);
        AssertHasEdges(8, FlowGraph.FinalVertex, 6);
    }

    [TestMethod]
    public void TestAppendToVertex()
    {
        FlowGraph flowGraph = FlowGraph.Empty.AddVertex(new FlowVertex { Index = 0, AssociatedStatement = new Stub(), IsVisible = true }, FlowEdge.CreateTo(1))
                                             .AddVertex(new FlowVertex { Index = 1, AssociatedStatement = new Stub(), IsVisible = true }, FlowGraph.FinalEdge);

        FlowGraph nestedGraph = FlowGraph.Empty.AddVertex(new FlowVertex { Index = 2, AssociatedStatement = new Stub(), IsVisible = true }, FlowEdge.CreateTo(3), FlowEdge.CreateTo(4))
                                               .AddVertex(new FlowVertex { Index = 3, AssociatedStatement = new Stub(), IsVisible = true }, FlowEdge.CreateTo(4))
                                               .AddVertex(new FlowVertex { Index = 4, AssociatedStatement = new Stub(), IsVisible = true }, FlowGraph.FinalEdge);

        FlowGraph resultGraph = flowGraph.AppendToVertex(0, nestedGraph);

        Assert.AreEqual(0, resultGraph.StartVertex);

        Assert.AreEqual(5, resultGraph.Vertices.Count);
        Assert.AreEqual(2, resultGraph.OutgoingEdges[0].Count);
        Assert.AreEqual(1, resultGraph.OutgoingEdges[0][0].ToVertex);
        Assert.AreEqual(2, resultGraph.OutgoingEdges[0][1].ToVertex);

        Assert.AreEqual(2, resultGraph.OutgoingEdges[2].Count);
        Assert.AreEqual(3, resultGraph.OutgoingEdges[2][0].ToVertex);
        Assert.AreEqual(4, resultGraph.OutgoingEdges[2][1].ToVertex);
    }

    [TestMethod]
    public void TestRemoveInvisible()
    {
        FlowGraph flowGraph = FlowGraph.Empty.AddVertex(new FlowVertex { Index = 0, AssociatedStatement = new Stub(), IsVisible = true }, FlowEdge.CreateTo(1), FlowEdge.CreateTo(2))
                                             .AddVertex(new FlowVertex { Index = 1, AssociatedStatement = new Stub(), IsVisible = true }, FlowEdge.CreateTo(2), FlowEdge.CreateTo(4))
                                             .AddVertex(new FlowVertex { Index = 2, AssociatedStatement = new Stub(), IsVisible = false }, FlowEdge.CreateTo(3))
                                             .AddVertex(new FlowVertex { Index = 3, AssociatedStatement = new Stub(), IsVisible = true }, FlowEdge.CreateTo(4))
                                             .AddVertex(new FlowVertex { Index = 4, AssociatedStatement = new Stub(), IsVisible = true }, FlowGraph.FinalEdge);

        FlowGraph visibleGraph = flowGraph.RemoveInvisible();

        Assert.AreEqual(4, visibleGraph.Vertices.Count);
        Assert.IsTrue(visibleGraph.Vertices.ContainsKey(0));
        Assert.IsTrue(visibleGraph.Vertices.ContainsKey(1));
        Assert.IsFalse(visibleGraph.Vertices.ContainsKey(2));
        Assert.IsTrue(visibleGraph.Vertices.ContainsKey(3));
        Assert.IsTrue(visibleGraph.Vertices.ContainsKey(4));

        Assert.AreEqual(2, visibleGraph.OutgoingEdges[0].Count);
        Assert.IsTrue(visibleGraph.OutgoingEdges[0][0].ToVertex is 1 or 3);
        Assert.IsTrue(visibleGraph.OutgoingEdges[0][1].ToVertex is 1 or 3);
        Assert.AreNotEqual(visibleGraph.OutgoingEdges[0][0].ToVertex, visibleGraph.OutgoingEdges[0][1].ToVertex);

        Assert.AreEqual(2, visibleGraph.OutgoingEdges[1].Count);
        Assert.IsTrue(visibleGraph.OutgoingEdges[1][0].ToVertex is 3 or 4);
        Assert.IsTrue(visibleGraph.OutgoingEdges[1][1].ToVertex is 3 or 4);
        Assert.AreNotEqual(visibleGraph.OutgoingEdges[1][0].ToVertex, visibleGraph.OutgoingEdges[1][1].ToVertex);

        Assert.AreEqual(1, visibleGraph.OutgoingEdges[3].Count);
        Assert.AreEqual(4, visibleGraph.OutgoingEdges[3][0].ToVertex);

        Assert.AreEqual(1, visibleGraph.OutgoingEdges[4].Count);
        Assert.AreEqual(FlowGraph.FinalVertex, visibleGraph.OutgoingEdges[4][0].ToVertex);
    }
}
