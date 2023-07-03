using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language.Flow;

namespace Phantonia.Historia.Tests.Compiler;

[TestClass]
public sealed class FlowGraphTests
{
    [TestMethod]
    public void TestSimpleFlowGraph()
    {
        FlowGraph simpleGraph = FlowGraph.CreateSimpleFlowGraph(42);

        Assert.AreEqual(42, simpleGraph.StartVertex);
        Assert.AreEqual(1, simpleGraph.OutgoingEdges.Count);
        Assert.IsTrue(simpleGraph.OutgoingEdges.ContainsKey(42));
        Assert.AreEqual(1, simpleGraph.OutgoingEdges[42].Count);
        Assert.AreEqual(FlowGraph.EmptyNode, simpleGraph.OutgoingEdges[42][0]);
    }

    [TestMethod]
    public void TestSimpleAppend()
    {
        FlowGraph fgA = FlowGraph.CreateSimpleFlowGraph(42);
        FlowGraph fgB = FlowGraph.CreateSimpleFlowGraph(64);

        FlowGraph resultGraph = fgA.Append(fgB);

        Assert.AreEqual(42, resultGraph.StartVertex);
        Assert.AreEqual(2, resultGraph.OutgoingEdges.Count);

        Assert.AreEqual(1, resultGraph.OutgoingEdges[42].Count);
        Assert.AreEqual(64, resultGraph.OutgoingEdges[42][0]);

        Assert.AreEqual(1, resultGraph.OutgoingEdges[64].Count);
        Assert.AreEqual(FlowGraph.EmptyNode, resultGraph.OutgoingEdges[64][0]);
    }
}
