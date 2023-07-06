using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language;
using Phantonia.Historia.Language.Flow;
using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Tests.Compiler;

[TestClass]
public sealed class FlowAnalyzerTests
{
    [TestMethod]
    public void TestLinearFlow()
    {
        string code = """
                      scene main
                      {
                          output 0;
                          output 1;
                          output 2;
                          output 3;
                      }
                      """;

        FlowAnalyzer flowAnalyzer = new(new Parser(new Lexer(code).Lex()).Parse());

        FlowGraph graph = flowAnalyzer.GenerateMainFlowGraph();

        Assert.AreEqual(4, graph.Vertices.Count);

        int[] vertices = graph.Vertices.Keys.ToArray();

        Assert.AreEqual(vertices[0], graph.StartVertex);

        foreach ((_, ImmutableList<int> pointedVertices) in graph.OutgoingEdges)
        {
            Assert.AreEqual(1, pointedVertices.Count);
        }

        Assert.AreEqual(vertices[1], graph.OutgoingEdges[vertices[0]][0]);
        Assert.AreEqual(vertices[2], graph.OutgoingEdges[vertices[1]][0]);
        Assert.AreEqual(vertices[3], graph.OutgoingEdges[vertices[2]][0]);
        Assert.AreEqual(FlowGraph.EmptyVertex, graph.OutgoingEdges[vertices[3]][0]);
    }
}
