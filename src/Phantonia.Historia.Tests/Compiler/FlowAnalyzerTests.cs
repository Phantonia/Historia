using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;
using Phantonia.Historia.Language.LexicalAnalysis;
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

    [TestMethod]
    public void TestSwitchFlow()
    {
        string code = """
                      scene main
                      {
                          switch (4)
                          {
                              option (5)
                              {
                                  output 6;
                              }

                              option (7)
                              {
                                  output 8;
                                  output 9;
                              }
                          }

                          output 10;
                      }
                      """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        FlowAnalyzer flowAnalyzer = new(parser.Parse());

        FlowGraph flowGraph = flowAnalyzer.GenerateMainFlowGraph();

        Assert.AreEqual(5, flowGraph.Vertices.Count);
        Assert.AreEqual(5, flowGraph.OutgoingEdges.Count);

        // switch (4)
        int startVertex = flowGraph.StartVertex;
        Assert.IsTrue(flowGraph.Vertices[startVertex].OutputExpression is IntegerLiteralExpressionNode { Value: 4 });
        Assert.AreEqual(2, flowGraph.OutgoingEdges[flowGraph.StartVertex].Count);

        // output 6
        int output6Vertex = flowGraph.OutgoingEdges[startVertex][0];
        Assert.IsTrue(flowGraph.Vertices[output6Vertex].OutputExpression is IntegerLiteralExpressionNode { Value: 6 });

        // output 8
        int output8Vertex = flowGraph.OutgoingEdges[startVertex][1];
        Assert.IsTrue(flowGraph.Vertices[output8Vertex].OutputExpression is IntegerLiteralExpressionNode { Value: 8 });
        Assert.AreEqual(1, flowGraph.OutgoingEdges[output8Vertex].Count);

        // output 9
        int output9Vertex = flowGraph.OutgoingEdges[output8Vertex][0];
        Assert.IsTrue(flowGraph.Vertices[output9Vertex].OutputExpression is IntegerLiteralExpressionNode { Value: 9 });

        // output 10
        Assert.AreEqual(flowGraph.OutgoingEdges[output6Vertex][0], flowGraph.OutgoingEdges[output9Vertex][0]);
        int output10Vertex = flowGraph.OutgoingEdges[output6Vertex][0];

        Assert.AreEqual(1, flowGraph.OutgoingEdges[output10Vertex].Count);
        Assert.AreEqual(FlowGraph.EmptyVertex, flowGraph.OutgoingEdges[output10Vertex][0]);
    }
}
