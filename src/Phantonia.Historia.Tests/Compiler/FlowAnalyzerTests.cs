﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language;
using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Tests.Compiler;

[TestClass]
public sealed class FlowAnalyzerTests
{
    private static FlowAnalyzer PrepareFlowAnalyzer(string code)
    {
        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        StoryNode tree = parser.Parse();
        NodeAssert.ReconstructWorks(code, tree);

        Binder binder = new(tree);
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
        flowAnalyzer.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        FlowGraph? graph = flowAnalyzer.PerformFlowAnalysis().MainFlowGraph;
        Assert.IsNotNull(graph);

        Assert.AreEqual(4, graph.Vertices.Count);

        long[] vertices = graph.Vertices.Keys.ToArray();

        Assert.AreEqual(1, graph.StartEdges.Length);
        Assert.AreEqual(vertices[0], graph.StartEdges[0].ToVertex);

        foreach ((_, ImmutableList<FlowEdge> pointedVertices) in graph.OutgoingEdges)
        {
            Assert.AreEqual(1, pointedVertices.Count);
        }

        Assert.AreEqual(vertices[1], graph.OutgoingEdges[vertices[0]][0].ToVertex);
        Assert.AreEqual(vertices[2], graph.OutgoingEdges[vertices[1]][0].ToVertex);
        Assert.AreEqual(vertices[3], graph.OutgoingEdges[vertices[2]][0].ToVertex);
        Assert.AreEqual(FlowGraph.FinalVertex, graph.OutgoingEdges[vertices[3]][0].ToVertex);
    }

    [TestMethod]
    public void TestSwitchFlow()
    {
        string code = """
                      scene main
                      {
                          switch 4
                          {
                              option 5
                              {
                                  output 6;
                              }

                              option 7
                              {
                                  output 8;
                                  output 9;
                              }
                          }

                          output 10;
                      }
                      """;

        FlowAnalyzer flowAnalyzer = PrepareFlowAnalyzer(code);
        flowAnalyzer.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        FlowGraph? flowGraph = flowAnalyzer.PerformFlowAnalysis().MainFlowGraph;
        Assert.IsNotNull(flowGraph);

        Assert.AreEqual(5, flowGraph.Vertices.Count);
        Assert.AreEqual(5, flowGraph.OutgoingEdges.Count);

        void AssertHasOutputValue(long vertex, int value)
        {
            switch (flowGraph.Vertices[vertex].AssociatedStatement)
            {
                case OutputStatementNode
                {
                    OutputExpression: TypedExpressionNode
                    {
                        Original: IntegerLiteralExpressionNode { Value: var val }
                    }
                }:
                    Assert.AreEqual(value, val);
                    break;
                case FlowBranchingStatementNode
                { 
                    Original: SwitchStatementNode
                    {
                        OutputExpression: TypedExpressionNode
                        {
                            Original: IntegerLiteralExpressionNode { Value: var val }
                        }
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
        long startVertex = flowGraph.GetStoryStartVertex();
        AssertHasOutputValue(startVertex, 4);
        Assert.AreEqual(2, flowGraph.OutgoingEdges[startVertex].Count);

        // output 6
        long output6Vertex = flowGraph.OutgoingEdges[startVertex][0].ToVertex;
        AssertHasOutputValue(output6Vertex, 6);

        // output 8
        long output8Vertex = flowGraph.OutgoingEdges[startVertex][1].ToVertex;
        AssertHasOutputValue(output8Vertex, 8);
        Assert.AreEqual(1, flowGraph.OutgoingEdges[output8Vertex].Count);

        // output 9
        long output9Vertex = flowGraph.OutgoingEdges[output8Vertex][0].ToVertex;
        AssertHasOutputValue(output9Vertex, 9);

        // output 10
        Assert.AreEqual(flowGraph.OutgoingEdges[output6Vertex][0].ToVertex, flowGraph.OutgoingEdges[output9Vertex][0].ToVertex);
        long output10Vertex = flowGraph.OutgoingEdges[output6Vertex][0].ToVertex;

        Assert.AreEqual(1, flowGraph.OutgoingEdges[output10Vertex].Count);
        Assert.AreEqual(FlowGraph.FinalVertex, flowGraph.OutgoingEdges[output10Vertex][0].ToVertex);
    }

    [TestMethod]
    public void TestBranchOnFlow()
    {
        string code =
            """
            scene main
            {
                outcome MySwitch (A, B, C);

                switch (0)
                {
                    option (3)
                    {
                        MySwitch = A;
                        output (3);
                    }

                    option (4)
                    {
                        MySwitch = B;
                        output (4);
                    }

                    option (5)
                    {
                        MySwitch = C;
                        output (5);
                    }
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
        flowAnalyzer.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        FlowGraph? graph = flowAnalyzer.PerformFlowAnalysis().MainFlowGraph;
        Assert.IsNotNull(graph);

        long output0Index = code.IndexOf("output (0)");

        Assert.IsTrue(graph.OutgoingEdges.ContainsKey(output0Index));
        Assert.AreEqual(1, graph.OutgoingEdges[output0Index].Count);

        long branchOnIndex = code.IndexOf("branchon");

        Assert.AreEqual(branchOnIndex, graph.OutgoingEdges[output0Index][0].ToVertex);

        Assert.IsTrue(graph.OutgoingEdges.ContainsKey(branchOnIndex));
        Assert.AreEqual(3, graph.OutgoingEdges[branchOnIndex].Count);

        Assert.AreEqual(code.IndexOf("output (9)"), graph.OutgoingEdges[branchOnIndex][0].ToVertex);
        Assert.AreEqual(code.IndexOf("output (16)"), graph.OutgoingEdges[branchOnIndex][1].ToVertex);
        Assert.AreEqual(code.IndexOf("output (25)"), graph.OutgoingEdges[branchOnIndex][2].ToVertex);
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

        List<Error> errors = [];
        flowAnalyzer.ErrorFound += errors.Add;

        _ = flowAnalyzer.PerformFlowAnalysis();

        Assert.AreEqual(1, errors.Count);

        Error expectedError = Errors.OutcomeNotDefinitelyAssigned("X", ["main"], code.IndexOf("branchon"));

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

        _ = flowAnalyzer.PerformFlowAnalysis();
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

        List<Error> errors = [];
        flowAnalyzer.ErrorFound += errors.Add;

        _ = flowAnalyzer.PerformFlowAnalysis();

        Assert.AreEqual(1, errors.Count);

        Error expectedError = Errors.OutcomeMightBeAssignedMoreThanOnce("X", ["main"], code.IndexOf("X = B"));

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

        List<Error> errors = [];
        flowAnalyzer.ErrorFound += errors.Add;

        _ = flowAnalyzer.PerformFlowAnalysis();

        Assert.AreEqual(2, errors.Count);

        Error expectedError0 = Errors.OutcomeMightBeAssignedMoreThanOnce("X", ["main"], code.IndexOf("X = B"));
        Assert.IsTrue(expectedError0 == errors[0] || expectedError0 == errors[1]);

        Error expectedError1 = Errors.OutcomeMightBeAssignedMoreThanOnce("X", ["main"], code.IndexOf("X = C"));
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

        List<Error> errors = [];
        flowAnalyzer.ErrorFound += errors.Add;

        _ = flowAnalyzer.PerformFlowAnalysis();

        Assert.AreEqual(2, errors.Count);

        Error expectedFirstError = Errors.OutcomeMightBeAssignedMoreThanOnce("X", ["main"], code.IndexOf("X = B"));
        Error expectedSecondError = Errors.OutcomeNotDefinitelyAssigned("Y", ["main"], code.IndexOf("branchon Y"));

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

        List<Error> errors = [];
        analyzer.ErrorFound += errors.Add;

        _ = analyzer.PerformFlowAnalysis();

        Assert.AreEqual(1, errors.Count);

        Error expectedError = Errors.SpectrumNotDefinitelyAssigned("X", ["main"], code.IndexOf("branchon X"));

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

        List<Error> errors = [];
        flowAnalyzer.ErrorFound += errors.Add;

        _ = flowAnalyzer.PerformFlowAnalysis();

        Assert.AreEqual(1, errors.Count);

        Error expectedError = Errors.CyclicSceneDefinition(["B", "A", "B"], code.IndexOf("scene B"));

        Assert.AreEqual(expectedError, errors[0]);
    }

    [TestMethod]
    public void TestSingleReferenceSceneMerging()
    {
        string code =
            """
            scene main
            {
                call A;

                switch 1 // s1
                {
                    option 0
                    {
                        call B;
                    }

                    option 0
                    {
                        call C;
                    }
                }
            }

            scene A
            {
                output 2; // s2
            }

            scene B
            {
                output 3; // s3
                output 4; // s4
            }

            scene C
            {
                switch 5 // s5
                {
                    option 0
                    {
                        output 6; // s6
                    }

                    option 0
                    {
                        output 7; // s7
                    }
                }
            }
            """;

        FlowAnalyzer flowAnalyzer = PrepareFlowAnalyzer(code);
        flowAnalyzer.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        FlowGraph? mainFlowGraph = flowAnalyzer.PerformFlowAnalysis().MainFlowGraph;
        Assert.IsNotNull(mainFlowGraph);

        Assert.AreEqual(7, mainFlowGraph.Vertices.Count);

        long s1 = code.IndexOf("switch 1"); // S1 nach Oranienburg
        long s2 = code.IndexOf("output 2"); // S2 nach Bernau
        long s3 = code.IndexOf("output 3"); // S3 nach Erkner
        long s4 = code.IndexOf("output 4"); // S46 nach Königs Wusterhausen (close)
        long s5 = code.IndexOf("switch 5"); // S5 nach Strausberg Nord
        long s6 = code.IndexOf("output 6"); // sadly no S6 in Berlin :(
        long s7 = code.IndexOf("output 7"); // S7 nach Potsdam Hauptbahnhof

        void AssertIsOutput(long index, int expectedOutputValue)
        {
            StatementNode statement = mainFlowGraph.Vertices[index].AssociatedStatement;

            Assert.IsTrue(statement is OutputStatementNode
            {
                OutputExpression: TypedExpressionNode
                {
                    Original: IntegerLiteralExpressionNode
                    {
                        Value: int value,
                    }
                }
            } && value == expectedOutputValue);
        }

        void AssertIsSwitch(long index, int expectedOutputValue)
        {
            StatementNode statement = mainFlowGraph.Vertices[index].AssociatedStatement;

            Assert.IsTrue(statement is FlowBranchingStatementNode
            {
                Original: SwitchStatementNode
                {
                    OutputExpression: TypedExpressionNode
                    {
                        Original: IntegerLiteralExpressionNode
                        {
                            Value: int value,
                        }
                    }
                }
            } && value == expectedOutputValue);
        }

        AssertIsSwitch(s1, 1);
        AssertIsOutput(s2, 2);
        AssertIsOutput(s3, 3);
        AssertIsOutput(s4, 4);
        AssertIsSwitch(s5, 5);
        AssertIsOutput(s6, 6);
        AssertIsOutput(s7, 7);

        // -> s2
        Assert.AreEqual(1, mainFlowGraph.StartEdges.Length);
        Assert.AreEqual(s2, mainFlowGraph.StartEdges[0].ToVertex);

        // s2 -> s1
        Assert.AreEqual(s1, mainFlowGraph.OutgoingEdges[s2].Single().ToVertex);

        // s1 -> s3
        //   \-> s5
        Assert.AreEqual(2, mainFlowGraph.OutgoingEdges[s1].Count);
        Assert.IsTrue(new[] { s3, s5 }.SequenceEqual(mainFlowGraph.OutgoingEdges[s1].Select(e => e.ToVertex).Order()));

        // s3 -> s4
        Assert.AreEqual(s4, mainFlowGraph.OutgoingEdges[s3].Single().ToVertex);

        // s4 ->
        Assert.AreEqual(FlowGraph.FinalVertex, mainFlowGraph.OutgoingEdges[s4].Single().ToVertex);

        // s5 -> s6
        //   \-> s7
        Assert.AreEqual(2, mainFlowGraph.OutgoingEdges[s5].Count);
        Assert.IsTrue(new[] { s6, s7 }.SequenceEqual(mainFlowGraph.OutgoingEdges[s5].Select(e => e.ToVertex).Order()));

        // s6 ->
        Assert.AreEqual(FlowGraph.FinalVertex, mainFlowGraph.OutgoingEdges[s6].Single().ToVertex);

        // s7 ->
        Assert.AreEqual(FlowGraph.FinalVertex, mainFlowGraph.OutgoingEdges[s7].Single().ToVertex);
    }

    [TestMethod]
    public void TestSceneMerging()
    {
        string code =
            """
            scene main
            {
                output 0; // o0
                call A; // 0 // t0
                output 1; // o1
                call B;
                output 2; // o2
            }

            scene A
            {
                output 10; // o10
                // rA
            }

            scene B
            {
                output 20; // o20
                call A; // 1 // t1
            }
            """;

        FlowAnalyzer flowAnalyzer = PrepareFlowAnalyzer(code);
        flowAnalyzer.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        FlowGraph? graph = flowAnalyzer.PerformFlowAnalysis().MainFlowGraph;
        Assert.IsNotNull(graph);

        long o0 = code.IndexOf("output 0");
        long o1 = code.IndexOf("output 1");
        long o2 = code.IndexOf("output 2");
        long o10 = code.IndexOf("output 10");
        long o20 = code.IndexOf("output 20");

        long t0 = code.IndexOf("call A; // 0");
        long t1 = code.IndexOf("call A; // 1");

        long rA = code.IndexOf("scene A") + 2;

        Assert.IsTrue(new[] { o0, o1, o2, o10, o20, t0, t1, rA }.Order().SequenceEqual(graph.Vertices.Keys.Order()));

        // -> o0
        Assert.AreEqual(o0, graph.GetStoryStartVertex());

        // o0 -> t0
        Assert.AreEqual(t0, graph.OutgoingEdges[o0].Single().ToVertex);

        // t0 -> o10
        Assert.AreEqual(o10, graph.OutgoingEdges[t0].Single().ToVertex);

        // o10 -> rA
        Assert.AreEqual(rA, graph.OutgoingEdges[o10].Single().ToVertex);

        // rA -> o1
        //   \-> o2
        Assert.AreEqual(2, graph.OutgoingEdges[rA].Count);
        Assert.IsTrue(new[] { o1, o2 }.SequenceEqual(graph.OutgoingEdges[rA].Select(e => e.ToVertex).Order()));

        // o1 -> o20
        Assert.AreEqual(o20, graph.OutgoingEdges[o1].Single().ToVertex);

        // o20 -> t1
        Assert.AreEqual(t1, graph.OutgoingEdges[o20].Single().ToVertex);

        // t1 -> o10
        Assert.AreEqual(o10, graph.OutgoingEdges[t1].Single().ToVertex);

        // o2 ->
        Assert.AreEqual(FlowGraph.FinalVertex, graph.OutgoingEdges[o2].Single().ToVertex);
    }

    [TestMethod]
    public void TestPossibleAssignmentThroughScenes()
    {
        string code =
            """
            outcome X(A, B);

            scene main
            {
                switch (0)
                {
                    option (1)
                    {
                        X = A;
                        call A;
                    }

                    option (2)
                    {
                        call B;
                    }
                }
                
                branchon X
                {
                    option A { }
                    option B { }
                }
            }

            scene A
            {
                X = A; //
            }

            scene B
            {
                X = B;
                call A;
            }
            """;

        FlowAnalyzer analyzer = PrepareFlowAnalyzer(code);

        List<Error> errors = [];
        analyzer.ErrorFound += errors.Add;

        _ = analyzer.PerformFlowAnalysis();

        Assert.AreEqual(2, errors.Count);

        Error firstError = Errors.OutcomeMightBeAssignedMoreThanOnce("X", ["A", "B", "main"], code.IndexOf("X = A; //"));
        Error secondError = Errors.OutcomeMightBeAssignedMoreThanOnce("X", ["A", "main"], code.IndexOf("X = A; //"));

        Assert.IsTrue(errors[0] == firstError || errors[0] == secondError);
        Assert.IsTrue(errors[1] == firstError || errors[1] == secondError);
        Assert.AreNotEqual(errors[0], errors[1]);
    }

    [TestMethod]
    public void TestDefiniteAssignmentThroughScenes()
    {
        string code =
            """
            outcome X(A, B);

            scene main
            {
                switch (0)
                {
                    option (1)
                    {
                        call A;
                    }

                    option (2)
                    {
                        call B;
                    }
                }
            }

            scene A
            {
                X = A;
                call C;
            }

            scene B
            {
                call C;
            }

            scene C
            {
                branchon X
                {
                    option A { }
                    option B { }
                }
            }
            """;

        FlowAnalyzer analyzer = PrepareFlowAnalyzer(code);

        List<Error> errors = [];
        analyzer.ErrorFound += errors.Add;

        _ = analyzer.PerformFlowAnalysis();

        Error expectedError = Errors.OutcomeNotDefinitelyAssigned("X", ["C", "B", "main"], code.IndexOf("branchon X"));

        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual(expectedError, errors[0]);
    }

    [TestMethod]
    public void TestLoopSwitch()
    {
        string code =
            """
            scene main
            {
                loop switch (0)
                {
                    option (1)
                    {
                        output 1;
                    }

                    option (2)
                    {
                        output 2;
                    }

                    loop option (3)
                    {
                        output 3;
                    }

                    final option (4)
                    {
                        output 4;
                    }

                    final option (5)
                    {
                        output 5;
                    }
                }
            }
            """;

        FlowAnalyzer analyzer = PrepareFlowAnalyzer(code);
        analyzer.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        // assert this terminates
        _ = analyzer.PerformFlowAnalysis();
    }

    [TestMethod]
    public void TestLoopSwitchDefiniteAssignment()
    {
        // problem: the 'X = A' fails even though it shouldn't
        return;

        //string code =
        //    """
        //    scene main
        //    {
        //        outcome X(A, B);
        //        outcome Y(A, B);

        //        loop switch (0)
        //        {
        //            option (1)
        //            {
        //                X = A;
        //            }

        //            final option (2)
        //            {
        //                Y = A;
        //            }
        //        }

        //        branchon X // error: X not definitely assigned
        //        {
        //            option A { }
        //            option B { }
        //        }

        //        branchon Y // no error: final option definitely hit
        //        {
        //            option A { }
        //            option B { }
        //        }
        //    }
        //    """;

        //FlowAnalyzer analyzer = PrepareFlowAnalyzer(code);

        //List<Error> errors = [];
        //analyzer.ErrorFound += errors.Add;

        //_ = analyzer.PerformFlowAnalysis();

        //Assert.AreEqual(1, errors.Count);

        //Error expectedError = Errors.OutcomeNotDefinitelyAssigned("X", ["main"], code.IndexOf("branchon X"));

        //Assert.AreEqual(expectedError, errors[0]);
    }

    [TestMethod]
    public void TestSceneCalledTwiceInScene()
    {
        string code =
            """
            scene main
            {
                call A;
                call A;
            }

            scene A
            {
                output 0;
            }
            """;

        FlowAnalyzer analyzer = PrepareFlowAnalyzer(code);
        analyzer.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        // we assert that this method runs, especially w/o Debug.Assert firing
        _ = analyzer.PerformFlowAnalysis();
    }

    [TestMethod]
    public void TestOutcome()
    {
        string code =
            """
            scene main
            {
                outcome X(A, B);

                branchon X
                {
                    option A
                    {

                    }

                    option B
                    {

                    }
                }
            }
            """;

        FlowAnalyzer analyzer = PrepareFlowAnalyzer(code);

        List<Error> errors = [];
        analyzer.ErrorFound += errors.Add;

        _ = analyzer.PerformFlowAnalysis();

        Assert.AreEqual(1, errors.Count);
    }

    //[TestMethod]
    //public void TestLoopSwitchAssignment()
    //{
    //    string code =
    //        """
    //        scene main
    //        {
    //            outcome X(A, B);

    //            loop switch (0)
    //            {
    //                option (1)
    //                {
    //                    X = A;
    //                }

    //                option (2)
    //                {
    //                    X = B;
    //                }
    //            }

    //            branchon X
    //            {
    //                option A { }
    //                option B { }
    //            }
    //        }
    //        """;

    //    FlowAnalyzer analyzer = PrepareFlowAnalyzer(code);

    //    List<Error> errors = new();
    //    analyzer.ErrorFound += errors.Add;

    //    _ = analyzer.PerformFlowAnalysis();

    //    errors.Sort((e1, e2) => e1.Index - e2.Index);

    //    Error firstError = Errors.OutcomeMightBeAssignedMoreThanOnce("X", new[] { "main" }, code.IndexOf("X = A"));
    //    Error secondError = Errors.OutcomeMightBeAssignedMoreThanOnce("X", new[] { "main" }, code.IndexOf("X = B"));
    //}

    [TestMethod]
    public void TestFinalLoopSwitchAssignment()
    {
        string code =
            """
            scene main
            {
                outcome X(A, B);
            
                loop switch (0)
                {
                    option (1)
                    {
                        X = A;
                    }
            
                    option (2)
                    {
                        X = B;
                    }
            
                    final option (3)
                    {
                        output 4;
                    }
                }
            
                branchon X
                {
                    option A { }
                    option B { }
                }
            }
            """;

        FlowAnalyzer analyzer = PrepareFlowAnalyzer(code);

        List<Error> errors = [];
        analyzer.ErrorFound += errors.Add;

        _ = analyzer.PerformFlowAnalysis();

        errors.Sort((x, y) => (int)(x.Index - y.Index));

        Error firstError = Errors.OutcomeMightBeAssignedMoreThanOnce("X", ["main"], code.IndexOf("X = A;"));
        Error secondError = Errors.OutcomeMightBeAssignedMoreThanOnce("X", ["main"], code.IndexOf("X = B;"));
        Error thirdError = Errors.OutcomeNotDefinitelyAssigned("X", ["main"], code.IndexOf("branchon X"));

        Assert.AreEqual(3, errors.Count);
        Assert.AreEqual(firstError, errors[0]);
        Assert.AreEqual(secondError, errors[1]);
        Assert.AreEqual(thirdError, errors[2]);
    }

    [TestMethod]
    public void TestCheckpointLocking()
    {
        string code =
            """
            scene main
            {
                outcome X(A, B);

                checkpoint output 0;
            
                X = A;
            
                branchon X // #0 => okay
                {
                    option A { }
                    option B { }
                }
            
                checkpoint output 1;

                branchon X // #1 => not okay, X got locked by checkpoint output 1
                {
                    option A { }
                    option B { }
                }
            }
            """;

        FlowAnalyzer flowAnalyzer = PrepareFlowAnalyzer(code);

        List<Error> errors = [];
        flowAnalyzer.ErrorFound += errors.Add;

        _ = flowAnalyzer.PerformFlowAnalysis();

        Error expectedError = Errors.OutcomeIsLocked("X", ["main"], code.IndexOf("branchon X // #1"));

        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual(expectedError, errors[0]);
    }

    [TestMethod]
    public void TestCheckpointsAndLoopSwitches()
    {
        string code =
            """
            scene main
            {
                outcome X(A, B);

                X = A;

                loop switch (0)
                {
                    option (100)
                    {
                        branchon X
                        {
                            option A { }
                            option B { }
                        }
                    }

                    option (101)
                    {
                        checkpoint output 2;
                    }

                    final option (102)
                    {
                        // do nothing
                    }
                }
            }
            """;

        FlowAnalyzer flowAnalyzer = PrepareFlowAnalyzer(code);

        List<Error> errors = [];
        flowAnalyzer.ErrorFound += errors.Add;

        _ = flowAnalyzer.PerformFlowAnalysis();

        Error expectedError = Errors.OutcomeIsLocked("X", ["main"], code.IndexOf("branchon X"));

        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual(expectedError, errors[0]);
    }

    [TestMethod]
    public void TestReferencesAndFlow()
    {
        string code =
            """
            setting OutputType: String;
            setting OptionType: String;
            
            interface ICharacter
            (
                action Say(line: String),
                choice Choose(prompt: String),
            );
            
            reference Character: ICharacter;
            
            scene main
            {
                run Character.Say("Hello world");
            
                choose Character.Choose("What to do?")
                {
                    option ("Jump")
                    {
                        output "I jumped!";
                    }
            
                    option ("Crouch")
                    {
                        output "I crouched!";
                    }
                }
            }
            """;

        FlowAnalyzer flowAnalyzer = PrepareFlowAnalyzer(code);

        FlowAnalysisResult result = flowAnalyzer.PerformFlowAnalysis();
        Assert.IsTrue(result.IsValid);

        int iRun = code.IndexOf("run");
        int iChoose = code.IndexOf("choose");
        int ioJump = code.IndexOf("""output "I jumped!";""");
        int ioCrouch = code.IndexOf("""output "I crouched!";""");

        Assert.AreEqual(4, result.MainFlowGraph.Vertices.Count);

        Assert.IsTrue(result.MainFlowGraph.StartEdges.Select(e => e.ToVertex).Order().SequenceEqual([iRun]));

        Assert.IsTrue(result.MainFlowGraph.Vertices.ContainsKey(iRun));
        Assert.IsTrue(result.MainFlowGraph.OutgoingEdges[iRun].Select(e => e.ToVertex).Order().SequenceEqual([iChoose]));

        Assert.IsTrue(result.MainFlowGraph.Vertices.ContainsKey(iChoose));
        Assert.IsTrue(result.MainFlowGraph.OutgoingEdges[iChoose].Select(e => e.ToVertex).Order().SequenceEqual([ioJump, ioCrouch]));

        Assert.IsTrue(result.MainFlowGraph.Vertices.ContainsKey(ioJump));
        Assert.IsTrue(result.MainFlowGraph.OutgoingEdges[ioJump].Select(e => e.ToVertex).Order().SequenceEqual([FlowGraph.FinalVertex]));

        Assert.IsTrue(result.MainFlowGraph.Vertices.ContainsKey(ioCrouch));
        Assert.IsTrue(result.MainFlowGraph.OutgoingEdges[ioCrouch].Select(e => e.ToVertex).Order().SequenceEqual([FlowGraph.FinalVertex]));
    }

    [TestMethod]
    public void TestIfStatements()
    {
        string code =
            """
            outcome X(A, B, C);

            scene main
            {
                X = A;

                if X is A or X is B
                {
                    output 0;
                }
                else if not X is C
                {
                    output 1;
                }
            }
            """;

        FlowAnalyzer analyzer = PrepareFlowAnalyzer(code);
        analyzer.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        FlowAnalysisResult result = analyzer.PerformFlowAnalysis();
        Assert.IsNotNull(result.MainFlowGraph);

        long ifIndex = code.IndexOf("if X");
        long output0Index = code.IndexOf("output 0");
        long elseIfIndex = code.IndexOf("if not X");
        long output1Index = code.IndexOf("output 1");

        Assert.IsTrue(result.MainFlowGraph.OutgoingEdges[ifIndex] is [{ ToVertex: long ifToA }, { ToVertex: long ifToB }] && ifToA == output0Index && ifToB == elseIfIndex);
        Assert.IsTrue(result.MainFlowGraph.OutgoingEdges[output0Index] is [{ ToVertex: FlowGraph.FinalVertex }]);
        Assert.IsTrue(result.MainFlowGraph.OutgoingEdges[elseIfIndex] is [{ ToVertex: long elseIfToA }, { ToVertex: FlowGraph.FinalVertex }] && elseIfToA == output1Index);
        Assert.IsTrue(result.MainFlowGraph.OutgoingEdges[output1Index] is [{ ToVertex: FlowGraph.FinalVertex }]);
    }

    [TestMethod]
    public void TestIsAndDefiniteAssignment()
    {
        string code =
            """
            outcome X(A, B);
            outcome Y(A, B);

            scene main
            {
                X = A;

                // very blatant
                if X is A or Y is B { }
            }
            """;
        
        FlowAnalyzer flowAnalyzer = PrepareFlowAnalyzer(code);

        List<Error> errors = [];
        flowAnalyzer.ErrorFound += errors.Add;

        _ = flowAnalyzer.PerformFlowAnalysis();

        Error expectedError = Errors.OutcomeNotDefinitelyAssigned("Y", ["main"], code.IndexOf("Y is B"));

        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual(errors[0], expectedError);
    }

    [TestMethod]
    public void TestNeverCalledScene()
    {
        string code =
            """
            scene main
            {
                output 0;
            }

            scene A
            {

            }
            """;

        FlowAnalyzer analyzer = PrepareFlowAnalyzer(code);
        analyzer.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        FlowAnalysisResult result = analyzer.PerformFlowAnalysis();
        Assert.IsNotNull(result.MainFlowGraph);
    }

    [TestMethod]
    public void TestNestedLoopSwitches()
    {
        string code =
            """
            scene main
            {
                loop switch (0)
                {
                    option (1)
                    {
                        loop switch (2)
                        {
                            option (3)
                            {
                                output 4;
                            }

                            option (5)
                            {
                                output 6;
                            }
                        }
                    }

                    option (7)
                    {
                        output 8;
                    }
                }
            }
            """;

        FlowAnalyzer analyzer = PrepareFlowAnalyzer(code);
        analyzer.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        FlowAnalysisResult result = analyzer.PerformFlowAnalysis();
        Assert.IsNotNull(result.MainFlowGraph);
    }
}
