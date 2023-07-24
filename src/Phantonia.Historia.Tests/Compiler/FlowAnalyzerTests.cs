using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language;
using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;
using Phantonia.Historia.Language.GrammaticalAnalysis.Statements;
using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis;
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

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        Binder binder = new(parser.Parse());
        binder.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        FlowAnalyzer flowAnalyzer = new(binder.Bind().BoundStory!);

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
        parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        Binder binder = new(parser.Parse());
        binder.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        FlowAnalyzer flowAnalyzer = new(binder.Bind().BoundStory!);

        FlowGraph flowGraph = flowAnalyzer.GenerateMainFlowGraph();

        Assert.AreEqual(5, flowGraph.Vertices.Count);
        Assert.AreEqual(5, flowGraph.OutgoingEdges.Count);

        void AssertHasOutputValue(int vertex, int value)
        {
            switch (flowGraph.Vertices[vertex].AssociatedStatement)
            {
                case OutputStatementNode
                {
                    OutputExpression: TypedExpressionNode
                    {
                        Expression: IntegerLiteralExpressionNode { Value: var val }
                    }
                }:
                    Assert.AreEqual(value, val);
                    break;
                case SwitchStatementNode
                {
                    OutputExpression: TypedExpressionNode
                    {
                        Expression: IntegerLiteralExpressionNode { Value: var val }
                    }
                }:
                    Assert.AreEqual(value, val);
                    break;
                default:
                    Assert.Fail();
                    break;
            }
        }

        // switch (4)
        int startVertex = flowGraph.StartVertex;
        AssertHasOutputValue(startVertex, 4);
        Assert.AreEqual(2, flowGraph.OutgoingEdges[flowGraph.StartVertex].Count);

        // output 6
        int output6Vertex = flowGraph.OutgoingEdges[startVertex][0];
        AssertHasOutputValue(output6Vertex, 6);

        // output 8
        int output8Vertex = flowGraph.OutgoingEdges[startVertex][1];
        AssertHasOutputValue(output8Vertex, 8);
        Assert.AreEqual(1, flowGraph.OutgoingEdges[output8Vertex].Count);

        // output 9
        int output9Vertex = flowGraph.OutgoingEdges[output8Vertex][0];
        AssertHasOutputValue(output9Vertex, 9);

        // output 10
        Assert.AreEqual(flowGraph.OutgoingEdges[output6Vertex][0], flowGraph.OutgoingEdges[output9Vertex][0]);
        int output10Vertex = flowGraph.OutgoingEdges[output6Vertex][0];

        Assert.AreEqual(1, flowGraph.OutgoingEdges[output10Vertex].Count);
        Assert.AreEqual(FlowGraph.EmptyVertex, flowGraph.OutgoingEdges[output10Vertex][0]);
    }

    [TestMethod]
    public void TestBranchOnFlow()
    {
        string code =
            """
            scene main
            {
                switch MySwitch (0)
                {
                    option A (3) { output (3); }
                    option B (4) { output (4); }
                    option C (5) { output (5); }
                }

                output (0);

                branchon MySwitch
                {
                    option A { output (9); }
                    option B { output (16); }
                    option C { output (25); }
                }
            }
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        Binder binder = new(parser.Parse());
        binder.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        FlowAnalyzer flowAnalyzer = new(binder.Bind().BoundStory!);

        FlowGraph graph = flowAnalyzer.GenerateMainFlowGraph();

        int output0Index = code.IndexOf("output (0)");

        Assert.IsTrue(graph.OutgoingEdges.ContainsKey(output0Index));
        Assert.AreEqual(1, graph.OutgoingEdges[output0Index].Count);

        int branchOnIndex = code.IndexOf("branchon");

        Assert.AreEqual(branchOnIndex, graph.OutgoingEdges[output0Index][0]);

        Assert.IsTrue(graph.OutgoingEdges.ContainsKey(branchOnIndex));
        Assert.AreEqual(3, graph.OutgoingEdges[branchOnIndex].Count);

        Assert.AreEqual(code.IndexOf("output (9)"), graph.OutgoingEdges[branchOnIndex][0]);
        Assert.AreEqual(code.IndexOf("output (16)"), graph.OutgoingEdges[branchOnIndex][1]);
        Assert.AreEqual(code.IndexOf("output (25)"), graph.OutgoingEdges[branchOnIndex][2]);
    }
}
