using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language.Flow;

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
        Assert.AreEqual(FlowGraph.EmptyNode, simpleGraph.OutgoingEdges[42][0]);
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
        Assert.AreEqual(FlowGraph.EmptyNode, resultGraph.OutgoingEdges[64][0]);
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
}
