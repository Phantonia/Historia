using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language;
using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Tests.Compiler;

[TestClass]
public sealed class FlowAnalyzerTests
{
    private FlowAnalyzer PrepareFlowAnalyzer(string code)
    {
        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        Binder binder = new(parser.Parse());
        binder.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        BindingResult result = binder.Bind();
        FlowAnalyzer flowAnalyzer = new(result.BoundStory!, result.SymbolTable!);
        return flowAnalyzer;
    }

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

        FlowAnalyzer flowAnalyzer = PrepareFlowAnalyzer(code);

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

        FlowAnalyzer flowAnalyzer = PrepareFlowAnalyzer(code);

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

        FlowAnalyzer flowAnalyzer = PrepareFlowAnalyzer(code);

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

    [TestMethod]
    public void TestOutcomeNotDefinitelyAssigned()
    {
        string code =
           """
            scene main
            {
                outcome X (A, B);

                switch (0)
                {
                    option (0)
                    {
                        X = A;
                    }

                    option (1)
                    {
                        
                    }
                }

                branchon X // error: outcome X not definitely assigned
                {
                    option A { }
                    option B { }
                }
            }
            """;

        FlowAnalyzer flowAnalyzer = PrepareFlowAnalyzer(code);

        List<Error> errors = new();
        flowAnalyzer.ErrorFound += errors.Add;

        _ = flowAnalyzer.GenerateMainFlowGraph();

        Assert.AreEqual(1, errors.Count);

        Error expectedError = Errors.OutcomeNotDefinitelyAssigned("X", code.IndexOf("branchon"));

        Assert.IsTrue(expectedError == errors[0] || expectedError == errors[1]);
    }

    [TestMethod]
    public void TestOutcomeDefinitelyAssigned()
    {
        string code =
            """
            scene main
            {
                outcome X (A, B);
            
                switch (0)
                {
                    option (0)
                    {
                        X = A;
                    }
            
                    option (1)
                    {
                        X = B;
                    }
                }
            
                branchon X // all good
                {
                    option A { }
                    option B { }
                }
            }
            """;

        FlowAnalyzer flowAnalyzer = PrepareFlowAnalyzer(code);
        flowAnalyzer.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        _ = flowAnalyzer.GenerateMainFlowGraph();
    }

    [TestMethod]
    public void TestOutcomeAssignedMoreThanOnce()
    {
        string code =
            """
            scene main
            {
                outcome X (A, B);

                // this is blatant
                X = A;
                X = B;
            }
            """;

        FlowAnalyzer flowAnalyzer = PrepareFlowAnalyzer(code);

        List<Error> errors = new();
        flowAnalyzer.ErrorFound += errors.Add;

        _ = flowAnalyzer.GenerateMainFlowGraph();

        Assert.AreEqual(1, errors.Count);

        Error expectedError = Errors.OutcomeMayBeAssignedMoreThanOnce("X", code.IndexOf("X = B"));

        Assert.IsTrue(expectedError == errors[0] || expectedError == errors[1]);
    }

    [TestMethod]
    public void TestOutcomeMightBeAssignedMoreThanOnce()
    {
        string code =
            """
            scene main
            {
                outcome X (A, B, C);

                switch (0)
                {
                    option (0) { X = A; }
                    option (1) { }
                }

                switch (1)
                {
                    option (0) { X = B; }
                    option (1) { X = C; }
                }
            }
            """;

        FlowAnalyzer flowAnalyzer = PrepareFlowAnalyzer(code);

        List<Error> errors = new();
        flowAnalyzer.ErrorFound += errors.Add;

        _ = flowAnalyzer.GenerateMainFlowGraph();

        Assert.AreEqual(2, errors.Count);

        Error expectedError0 = Errors.OutcomeMayBeAssignedMoreThanOnce("X", code.IndexOf("X = B"));
        Assert.IsTrue(expectedError0 == errors[0] || expectedError0 == errors[1]);

        Error expectedError1 = Errors.OutcomeMayBeAssignedMoreThanOnce("X", code.IndexOf("X = C"));
        Assert.IsTrue(expectedError1 == errors[0] || expectedError1 == errors[1]);
    }

    [TestMethod]
    public void TestMultipleOutcomes()
    {
        string code =
            """
            scene main
            {
                outcome X (A, B);
                outcome Y (A, B);
                outcome Z (A, B) default A;

                switch (0)
                {
                    option (1)
                    {
                        X = A;
                    }

                    option (2)
                    {
                        Y = A;
                    }
                }

                X = B; // error: X might already be assigned

                branchon Y // error: Y might not be assigned
                {
                    option A { }
                    option B { }
                }

                branchon Z // okay: Z is not assigned but has a default value
                {
                    option A { }
                    option B { }
                }
            }
            """;

        FlowAnalyzer flowAnalyzer = PrepareFlowAnalyzer(code);

        List<Error> errors = new();
        flowAnalyzer.ErrorFound += errors.Add;

        _ = flowAnalyzer.GenerateMainFlowGraph();

        Assert.AreEqual(2, errors.Count);

        Error expectedFirstError = Errors.OutcomeMayBeAssignedMoreThanOnce("X", code.IndexOf("X = B"));
        Error expectedSecondError = Errors.OutcomeNotDefinitelyAssigned("Y", code.IndexOf("branchon Y"));

        Assert.IsTrue(expectedFirstError == errors[0] || expectedFirstError == errors[1]);
        Assert.IsTrue(expectedSecondError == errors[0] || expectedSecondError == errors[1]);
    }

    [TestMethod]
    public void TestSpectrumAssignment()
    {
        string code =
            """
            scene main
            {
                spectrum X (A <= 1/2, B);
                spectrum Y (A <= 1/2, B) default A;

                switch (0)
                {
                    option (1)
                    {
                        strengthen X by 1;
                    }

                    option (2)
                    {
                        weaken Y by 2;
                    }
                }

                branchon X
                {
                    option A { }
                    option B { }
                }

                branchon Y
                {
                    option A { }
                    option B { }
                }
            }
            """;

        FlowAnalyzer analyzer = PrepareFlowAnalyzer(code);

        List<Error> errors = new();
        analyzer.ErrorFound += errors.Add;

        _ = analyzer.GenerateMainFlowGraph();

        Assert.AreEqual(1, errors.Count);

        Error expectedError = Errors.SpectrumNotDefinitelyAssigned("X", code.IndexOf("branchon X"));

        Assert.AreEqual(expectedError, errors[0]);
    }

    [TestMethod]
    public void TestCyclicScenes()
    {
        string code =
            """
            scene A
            {
                call B;
            }

            scene B
            {
                call A;
            }

            scene main { }
            """;

        FlowAnalyzer flowAnalyzer = PrepareFlowAnalyzer(code);

        List<Error> errors = new();
        flowAnalyzer.ErrorFound += errors.Add;

        _ = flowAnalyzer.GenerateMainFlowGraph();

        Assert.AreEqual(1, errors.Count);

        Error expectedError = Errors.CyclicSceneDefinition(new[] { "B", "A", "B" }, code.IndexOf("scene B"));

        Assert.AreEqual(expectedError, errors[0]);
    }
}
