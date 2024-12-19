using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language;
using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Tests.Compiler;

[TestClass]
public sealed class BinderTests
{
    private static Binder PrepareBinder(string code)
    {
        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        Binder binder = new(parser.Parse());
        return binder;
    }

    [TestMethod]
    public void TestNoMain()
    {
        string code =
            """
            setting OutputType: String;
            """;

        Binder binder = PrepareBinder(code);

        int errorCount = 0;

        binder.ErrorFound += e =>
        {
            errorCount++;

            const string ErrorMessage = """
                                        Error: A story needs a main scene
                                        setting OutputType: String;
                                        ^
                                        """;

            Assert.AreEqual(ErrorMessage, Errors.GenerateFullMessage(code, e));
        };

        _ = binder.Bind();

        Assert.AreEqual(1, errorCount);
    }

    [TestMethod]
    public void TestTooManyMains()
    {
        // so that we can distinguish the main declarations
        string code = """
                      scene main{}
                      scene main {}
                      scene main {}
                      """;

        Binder binder = PrepareBinder(code);

        int errorCount = 0;

        binder.ErrorFound += e =>
        {
            errorCount++;

            const string ErrorMessage = """
                                        Error: Duplicated symbol name 'main'
                                        scene main {}
                                        ^
                                        """;

            Assert.AreEqual(ErrorMessage, Errors.GenerateFullMessage(code, e));
        };

        _ = binder.Bind();

        Assert.AreEqual(2, errorCount);
    }

    [TestMethod]
    public void TestTypeSettings()
    {
        string code =
            """
            setting OutputType: String;
            setting OptionType: Int;
            scene main { }
            """;

        Binder binder = PrepareBinder(code);

        BindingResult result = binder.Bind();
        Assert.IsTrue(result.IsValid);
        (StoryNode? boundStory, _, SymbolTable? symbolTable) = result;

        Assert.AreEqual(3, boundStory!.TopLevelNodes.Length);

        Assert.IsTrue(boundStory.TopLevelNodes[0] is TypeSettingDirectiveNode
        {
            Type: BoundTypeNode
            {
                Symbol: BuiltinTypeSymbol
                {
                    Type: BuiltinType.String,
                }
            }
        });

        Assert.IsTrue(boundStory.TopLevelNodes[1] is TypeSettingDirectiveNode
        {
            Type: BoundTypeNode
            {
                Symbol: BuiltinTypeSymbol
                {
                    Type: BuiltinType.Int,
                }
            }
        });

        Assert.IsTrue(symbolTable!.IsDeclared("main"));
    }

    [TestMethod]
    public void TestNameClashes()
    {
        string code =
            """
            scene Abc { }
            scene Abc { }
            scene main { }
            """;

        Binder binder = PrepareBinder(code);

        int errorCount = 0;
        binder.ErrorFound += e =>
        {
            errorCount++;

            Assert.AreEqual(
                """
                Error: Duplicated symbol name 'Abc'
                scene Abc { }
                ^
                """, Errors.GenerateFullMessage(code, e));
        };

        _ = binder.Bind();
        Assert.AreEqual(1, errorCount);
    }

    [TestMethod]
    public void TestRecordDeclarations()
    {
        string code =
            """
            record Line(Text: String, Character: Int);

            scene main { }
            """;

        Binder binder = PrepareBinder(code);
        binder.ErrorFound += e => Assert.Fail($"Error: {e.ErrorMessage}");

        BindingResult result = binder.Bind();
        Assert.IsTrue(result.IsValid);
        (StoryNode? boundStory, _, SymbolTable? symbolTable) = result;

        Assert.AreEqual(2, boundStory!.TopLevelNodes.Length);

        Assert.IsTrue(boundStory.TopLevelNodes[0] is BoundSymbolDeclarationNode
        {
            Symbol: RecordTypeSymbol
            {
                Name: "Line",
                Properties:
                [
                    PropertySymbol
                {
                    Name: "Text",
                    Type: BuiltinTypeSymbol
                    {
                        Type: BuiltinType.String,
                    }
                },
                    PropertySymbol
                {
                    Name: "Character",
                    Type: BuiltinTypeSymbol
                    {
                        Type: BuiltinType.Int,
                    }
                }
                ]
            },
            Declaration: RecordSymbolDeclarationNode
            {
                Name: "Line",
                Properties:
                [
                    PropertyDeclarationNode
                {
                    Type: BoundTypeNode
                },
                    PropertyDeclarationNode
                {
                    Type: BoundTypeNode
                }
                ]
            }
        });
    }

    [TestMethod]
    public void TestCyclicRecords()
    {
        string code =
            """
            scene main { }

            record A(B: B);

            record B(C: C);

            record C(A: A);
            """;

        Binder binder = PrepareBinder(code);

        List<Error> errors = [];
        binder.ErrorFound += errors.Add;

        BindingResult result = binder.Bind();
        Assert.IsFalse(result.IsValid);

        Error expectedError = Errors.CyclicTypeDefinition(["C", "A", "B", "C"], code.IndexOf("record C"));

        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual(expectedError, errors[0]);
        Assert.IsTrue(errors[0].Index == code.IndexOf("record C"));
    }

    [TestMethod]
    public void TestSelfRecursiveRecord()
    {
        string code =
            """
            scene main { }

            record A(Self: A);
            """;

        Binder binder = PrepareBinder(code);

        List<Error> errors = [];
        binder.ErrorFound += errors.Add;

        BindingResult result = binder.Bind();
        Assert.IsFalse(result.IsValid);

        Error expectedError = Errors.CyclicTypeDefinition(["A", "A"], index: code.IndexOf("record A"));

        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual(expectedError, errors[0]);
        Assert.AreEqual(code.IndexOf("record A"), errors[0].Index);
    }

    [TestMethod]
    public void TestMoreComplexRecords()
    {
        string code =
            """
            scene main { }

            record Line(Text: String, Character: Int);

            record StageDirection(Direction: String, Character: Int);

            record Moment(Line: Line, StageDirection: StageDirection);
            """;

        Binder binder = PrepareBinder(code);

        binder.ErrorFound += e => Assert.Fail($"Error: {e.ErrorMessage}");

        BindingResult result = binder.Bind();
        Assert.IsTrue(result.IsValid);

        Assert.IsNotNull(result.BoundStory);

        Assert.AreEqual(4, result.BoundStory.TopLevelNodes.Length);

        Assert.IsTrue(result.BoundStory.TopLevelNodes[1] is BoundSymbolDeclarationNode
        {
            Declaration: RecordSymbolDeclarationNode
            {
                Name: "Line",
                Properties.Length: 2,
            },
            Symbol: RecordTypeSymbol
            {
                Name: "Line",
                Properties.Length: 2,
            }
        });

        Assert.IsTrue(result.BoundStory.TopLevelNodes[2] is BoundSymbolDeclarationNode
        {
            Declaration: RecordSymbolDeclarationNode,
            Symbol: RecordTypeSymbol,
        });

        Assert.IsTrue(result.BoundStory.TopLevelNodes[3] is BoundSymbolDeclarationNode
        {
            Declaration: RecordSymbolDeclarationNode
            {
                Name: "Moment",
                Properties:
                [
                    BoundPropertyDeclarationNode
                {
                    Name: "Line",
                    Symbol: PropertySymbol
                    {
                        Name: "Line",
                        Type: RecordTypeSymbol
                        {
                            Name: "Line",
                            Properties.Length: 2,
                        }
                    }
                },
                    BoundPropertyDeclarationNode
                {
                    Name: "StageDirection",
                    Symbol: PropertySymbol
                    {
                        Name: "StageDirection",
                        Type: RecordTypeSymbol
                        {
                            Name: "StageDirection",
                            Properties.Length: 2,
                        }
                    }
                }
                ]
            }
        });
    }

    [TestMethod]
    public void TestRecordsInWeirdOrder()
    {
        string code =
            """
            record F(V: D, W: E);
            record A(V: Int);
            record C(V: A);
            record B(V: Int);
            record D(V: C);
            record E(V: B);

            scene main { }
            """;

        Binder binder = PrepareBinder(code);
        binder.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        BindingResult result = binder.Bind();

        SymbolTable table = result.SymbolTable!;

        RecordTypeSymbol a = (RecordTypeSymbol)table["A"];
        RecordTypeSymbol b = (RecordTypeSymbol)table["B"];
        RecordTypeSymbol c = (RecordTypeSymbol)table["C"];
        RecordTypeSymbol d = (RecordTypeSymbol)table["D"];
        RecordTypeSymbol e = (RecordTypeSymbol)table["E"];
        RecordTypeSymbol f = (RecordTypeSymbol)table["F"];

        Assert.AreEqual(a, c.Properties[0].Type);
        Assert.AreEqual(c, d.Properties[0].Type);
        Assert.AreEqual(b, e.Properties[0].Type);
        Assert.AreEqual(d, f.Properties[0].Type);
        Assert.AreEqual(e, f.Properties[1].Type);
    }

    [TestMethod]
    public void TestBindingAndTypeChecking()
    {
        string code =
            """
            setting OutputType: Line;

            record Line(Text: String, Character: Int);

            scene main
            {
                output Line("Hello, is anybody there?", 1);
                output Line("Hello, echo!", 1);
                output Line("Echo, hello", 2);
            }
            """;

        Binder binder = PrepareBinder(code);
        binder.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        BindingResult result = binder.Bind();

        Assert.IsTrue(result.IsValid);
        Assert.IsNotNull(result.BoundStory);

        Assert.AreEqual(3, result.BoundStory.TopLevelNodes.Length);

        {
            SettingDirectiveNode? outputTypeSetting = result.BoundStory.TopLevelNodes[0] as SettingDirectiveNode;
            Assert.IsNotNull(outputTypeSetting);

            Assert.IsTrue(outputTypeSetting is TypeSettingDirectiveNode
            {
                SettingName: "OutputType",
                Type: BoundTypeNode
                {
                    Symbol: RecordTypeSymbol
                    {
                        Name: "Line",
                    }
                }
            });
        }

        {
            BoundSymbolDeclarationNode? recordDeclaration = result.BoundStory.TopLevelNodes[1] as BoundSymbolDeclarationNode;
            Assert.IsNotNull(recordDeclaration);

            Assert.IsTrue(recordDeclaration is
            {
                Symbol: RecordTypeSymbol
                {
                    Name: "Line",
                    Properties.Length: 2,
                },
                Declaration: RecordSymbolDeclarationNode
                {
                    Name: "Line",
                    Properties.Length: 2,
                }
            });
        }

        {
            BoundSymbolDeclarationNode? mainSceneDeclaration = result.BoundStory.TopLevelNodes[2] as BoundSymbolDeclarationNode;
            Assert.IsNotNull(mainSceneDeclaration);

            Assert.IsTrue(mainSceneDeclaration.Symbol is SceneSymbol { Name: "main" });

            SceneSymbolDeclarationNode? sceneDeclaration = mainSceneDeclaration.Declaration as SceneSymbolDeclarationNode;
            Assert.IsNotNull(sceneDeclaration);

            Assert.AreEqual(3, sceneDeclaration.Body.Statements.Length);

            OutputStatementNode? outputStatement = sceneDeclaration.Body.Statements[0] as OutputStatementNode;
            Assert.IsNotNull(outputStatement);

            TypedExpressionNode? typedExpression = outputStatement.OutputExpression as TypedExpressionNode;
            Assert.IsNotNull(typedExpression);

            Assert.IsTrue(typedExpression.SourceType is RecordTypeSymbol
            {
                Name: "Line",
                Properties.Length: 2,
            });

            Assert.IsTrue(typedExpression.Expression is BoundRecordCreationExpressionNode
            {
                Record: RecordTypeSymbol
                {
                    Name: "Line"
                },
                BoundArguments:
                [
                {
                    Expression: TypedExpressionNode
                    {
                        Expression: StringLiteralExpressionNode,
                        SourceType: BuiltinTypeSymbol
                        {
                            Type: BuiltinType.String,
                        }
                    }
                },
                    BoundArgumentNode,
                ]
            });
        }
    }

    [TestMethod]
    public void TestTypeErrors()
    {
        string code =
            """
            setting OutputType: Line;

            record Line(Text: String, Character: Int);

            scene main
            {
                output 2;
                output "xyz";
                output Line(2, 1);
                output Line("Hello");
            }
            """;

        Binder binder = PrepareBinder(code);
        List<Error> errors = [];
        binder.ErrorFound += errors.Add;

        BindingResult bindingResult = binder.Bind();

        BuiltinTypeSymbol intType = new()
        {
            Name = "Int",
            Index = Constants.IntTypeIndex,
            Type = BuiltinType.Int,
        };

        BuiltinTypeSymbol stringType = new()
        {
            Name = "String",
            Index = Constants.StringTypeIndex,
            Type = BuiltinType.String,
        };

        RecordTypeSymbol lineType = new()
        {
            Name = "Line",
            Index = code.IndexOf("record Line"),
            Properties = ImmutableArray<PropertySymbol>.Empty,
        };

        Assert.AreEqual(4, errors.Count);

        Error firstError = Errors.IncompatibleType(sourceType: intType, targetType: lineType, "output", code.IndexOf("2;"));
        Assert.AreEqual(firstError, errors[0]);

        Error secondError = Errors.IncompatibleType(sourceType: stringType, targetType: lineType, "output", code.IndexOf("\"xyz\""));
        Assert.AreEqual(secondError, errors[1]);

        Error thirdError = Errors.IncompatibleType(sourceType: intType, targetType: stringType, "property", code.IndexOf("2, 1"));
        Assert.AreEqual(thirdError, errors[2]);

        Error fourthError = Errors.WrongAmountOfArgumentsInRecordCreation("Line", givenAmount: 1, expectedAmount: 2, code.IndexOf("Line(\"Hello\");"));
        Assert.AreEqual(fourthError, errors[3]);
    }

    [TestMethod]
    public void TestInconsistentNamedSwitches()
    {
        string code =
            """
            scene main
            {
                switch MySwitch (4)
                {
                    option (5)
                    { }

                    option MyOption (6)
                    { }
                }

                switch (7)
                {
                    option MyOption (8)
                    { }

                    option (9)
                    { }
                }
            }
            """;

        Binder binder = PrepareBinder(code);

        List<Error> errors = [];
        binder.ErrorFound += errors.Add;

        _ = binder.Bind();

        Assert.AreEqual(2, errors.Count);

        Error firstError = Errors.InconsistentNamedSwitch(code.IndexOf("switch MySwitch (4)"));
        Error secondError = Errors.InconsistentUnnamedSwitch(code.IndexOf("switch (7)"));

        Assert.AreEqual(firstError, errors[0]);
        Assert.AreEqual(secondError, errors[1]);
    }

    [TestMethod]
    public void TestBranchOn()
    {
        string code =
            """
            scene main
            {
                switch MySwitch (4)
                {
                    option A (5) { }
                    option B (6) { }
                    option C (7) { }
                    option D (8) { }
                    option E (9) { }
                }

                branchon MySwitch
                {
                    option C { }
                    option A { }
                    option B { }
                    other { }
                }
            }
            """;

        Binder binder = PrepareBinder(code);
        binder.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        StoryNode boundStory = binder.Bind().BoundStory!;

        SceneSymbolDeclarationNode mainScene = (SceneSymbolDeclarationNode)((BoundSymbolDeclarationNode)boundStory.TopLevelNodes[0]).Declaration;
        BoundBranchOnStatementNode branchOnStatement = (BoundBranchOnStatementNode)mainScene.Body.Statements[1];

        Assert.AreEqual("MySwitch", branchOnStatement.OutcomeName);
        Assert.AreEqual("MySwitch", branchOnStatement.Outcome.Name);

        Assert.IsTrue(new[] { "C", "A", "B" }.SequenceEqual(branchOnStatement.Options.OfType<NamedBranchOnOptionNode>().Select(o => o.OptionName)));
    }

    [TestMethod]
    public void TestWrongBranchOns()
    {
        void TestForError(string code, Error expectedError)
        {
            Binder binder = PrepareBinder(code);

            List<Error> errors = new();
            binder.ErrorFound += errors.Add;

            _ = binder.Bind();

            Assert.AreEqual(1, errors.Count);
            Assert.AreEqual(expectedError, errors[0]);

            Debug.WriteLine(Errors.GenerateFullMessage(code, errors[0]));
        }

        string code0 =
            """
            scene main
            {
                branchon NonexistentOutcome
                {
                    option A { }
                    option B { }
                }
            }
            """;

        TestForError(code0, Errors.SymbolDoesNotExistInScope("NonexistentOutcome", code0.IndexOf("branchon")));

        string code1 =
            """
            record NotOutcome(Stuff: Int);

            scene main
            {
                branchon NotOutcome
                {
                    option A { }
                    option B { }
                }
            }
            """;

        TestForError(code1, Errors.SymbolIsNotOutcome("NotOutcome", code1.IndexOf("branchon")));

        string code2 =
            """
            scene main
            {
                switch Outcome (0)
                {
                    option A (0) { }
                    option B (0) { }
                }

                branchon Outcome
                {
                    option A { }
                    option B { }
                    option C { }
                }
            }
            """;

        TestForError(code2, Errors.OptionDoesNotExistInOutcome("Outcome", "C", code2.IndexOf("option C")));

        string code3 =
            """
            scene main
            {
                switch Outcome (0)
                {
                    option A (0) { }
                    option B (0) { }
                }
            
                branchon Outcome
                {
                    option A { }
                    option A{ }
                    option B { }
                }
            }
            """;

        TestForError(code3, Errors.BranchOnDuplicateOption("Outcome", "A", code3.IndexOf("option A{")));

        string code4 =
            """
            scene main
            {
                switch Outcome (0)
                {
                    option A (0) { }
                    option B (0) { }
                    option C (0) { }
                    option D (0) { }
                }
            
                branchon Outcome
                {
                    option A { }
                    option B { }
                }
            }
            """;

        TestForError(code4, Errors.BranchOnIsNotExhaustive("Outcome", ["C", "D"], code4.IndexOf("branchon")));

        string code5 =
            """
            scene main
            {
                switch Outcome (0)
                {
                    option A (0) { }
                    option B (0) { }
                }
            
                branchon Outcome
                {
                    option A { }
                    option B { }
                    other { }
                }
            }
            """;

        TestForError(code5, Errors.BranchOnIsExhaustiveAndHasOtherBranch("Outcome", code5.IndexOf("branchon")));
    }

    [TestMethod]
    public void TestOutcomeDeclarationStatements()
    {
        string code =
            """
            scene main
            {
                outcome WorldEnding (Yes, No);

                WorldEnding = Yes; // is that even a question?

                branchon WorldEnding
                {
                    option Yes { }
                    option No { }
                }
            }
            """;

        Binder binder = PrepareBinder(code);
        binder.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        StoryNode boundStory = binder.Bind().BoundStory!;

        SceneSymbolDeclarationNode mainScene = (SceneSymbolDeclarationNode)((BoundSymbolDeclarationNode)boundStory.TopLevelNodes[0]).Declaration;

        BoundOutcomeDeclarationStatementNode? boundOutcomeDeclaration = mainScene.Body.Statements[0] as BoundOutcomeDeclarationStatementNode;
        Assert.IsNotNull(boundOutcomeDeclaration);

        Assert.AreEqual("WorldEnding", boundOutcomeDeclaration.Outcome.Name);
        Assert.IsTrue(new[] { "Yes", "No" }.SequenceEqual(boundOutcomeDeclaration.Outcome.OptionNames));
        Assert.IsNull(boundOutcomeDeclaration.Outcome.DefaultOption);

        BoundOutcomeAssignmentStatementNode? boundAssignment = mainScene.Body.Statements[1] as BoundOutcomeAssignmentStatementNode;
        Assert.IsNotNull(boundAssignment);

        BoundBranchOnStatementNode? boundBranchOn = mainScene.Body.Statements[2] as BoundBranchOnStatementNode;
        Assert.IsNotNull(boundBranchOn);

        Assert.AreEqual(boundOutcomeDeclaration.Outcome, boundAssignment.Outcome);
        Assert.AreEqual(boundBranchOn.Outcome, boundOutcomeDeclaration.Outcome);
    }

    [TestMethod]
    public void TestWrongOutcomes()
    {
        void TestForError(string code, Error expectedError)
        {
            Binder binder = PrepareBinder(code);

            List<Error> errors = [];
            binder.ErrorFound += errors.Add;

            _ = binder.Bind();

            Assert.AreEqual(1, errors.Count);
            Assert.AreEqual(expectedError, errors[0]);

            Debug.WriteLine(Errors.GenerateFullMessage(code, errors[0]));
        }

        string code0 =
            """
            scene main
            {
                outcome X();
            }
            """;

        TestForError(code0, Errors.OutcomeWithZeroOptions("X", code0.IndexOf("outcome X")));

        string code1 =
            """
            scene main
            {
                outcome X(A, A);
            }
            """;

        TestForError(code1, Errors.DuplicatedOptionInOutcomeDeclaration("A", code1.IndexOf("outcome X")));

        string code2 =
            """
            scene main
            {
                outcome X(A, B) default C;
            }
            """;

        TestForError(code2, Errors.OutcomeDefaultOptionNotAnOption("X", code2.IndexOf("outcome X")));

        string code3 =
            """
            scene main
            {
                outcome X(A, B);
                X = C;
            }
            """;

        TestForError(code3, Errors.OptionDoesNotExistInOutcome("X", "C", code3.IndexOf("C;")));
    }

    [TestMethod]
    public void TestUnionTypes()
    {
        string code =
            """
            union Stuff (Int, String);
            setting OutputType: Stuff;

            scene main
            {
                output "String";
                output 2;
            }
            """;

        Binder binder = PrepareBinder(code);
        binder.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        BindingResult result = binder.Bind();

        Assert.IsTrue(result.IsValid);

        StoryNode? boundStory = result.BoundStory;
        Assert.IsNotNull(boundStory);

        Assert.AreEqual(3, boundStory.TopLevelNodes.Length);

        BoundSymbolDeclarationNode? boundUnionDeclaration = boundStory.TopLevelNodes[0] as BoundSymbolDeclarationNode;
        Assert.IsNotNull(boundUnionDeclaration);
        Assert.IsTrue(boundUnionDeclaration is
        {
            Declaration: UnionSymbolDeclarationNode { Name: "Stuff", Subtypes.Length: 2 },
            Symbol: UnionTypeSymbol { Name: "Stuff", Subtypes: [BuiltinTypeSymbol { Type: BuiltinType.Int }, BuiltinTypeSymbol { Type: BuiltinType.String }] },
        });

        BoundSymbolDeclarationNode? mainScene = boundStory.TopLevelNodes[2] as BoundSymbolDeclarationNode;
        SceneSymbolDeclarationNode? scene = mainScene?.Declaration as SceneSymbolDeclarationNode;
        Assert.IsNotNull(scene);

        Assert.AreEqual(2, scene.Body.Statements.Length);

        OutputStatementNode? outputStringStatement = scene.Body.Statements[0] as OutputStatementNode;
        Assert.IsNotNull(outputStringStatement);

        Assert.IsTrue(outputStringStatement is
        {
            OutputExpression: TypedExpressionNode
            {
                SourceType: BuiltinTypeSymbol { Type: BuiltinType.String },
                TargetType: UnionTypeSymbol { Name: "Stuff" },
            }
        });

        OutputStatementNode? outputIntStatement = scene.Body.Statements[0] as OutputStatementNode;
        Assert.IsNotNull(outputIntStatement);

        Assert.IsTrue(outputIntStatement is
        {
            OutputExpression: TypedExpressionNode
            {
                SourceType: BuiltinTypeSymbol { Type: BuiltinType.String },
                TargetType: UnionTypeSymbol { Name: "Stuff" },
            }
        });
    }

    [TestMethod]
    public void TestRecursiveUnion()
    {
        string code =
            """
            record X(A: Int, B: String);

            union Y (X, Int);
            union Z (Y, String);

            setting OutputType: Z;

            scene main
            {
                output X(4, "abc");
                output 4;
                output "xyz";
            }
            """;

        Binder binder = PrepareBinder(code);
        binder.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        _ = binder.Bind();
    }

    [TestMethod]
    public void TestUnionTypeError()
    {
        string code =
            """
            record X
            (A: Int, B: String);

            union Y (X, Int);

            setting OutputType: Y;

            scene main
            {
                output "String";
            }
            """;

        Binder binder = PrepareBinder(code);

        List<Error> errors = [];
        binder.ErrorFound += errors.Add;

        BindingResult result = binder.Bind();

        Assert.AreEqual(1, errors.Count);

        Error expectedError = Errors.IncompatibleType((TypeSymbol)result.SymbolTable!["String"], (TypeSymbol)result.SymbolTable!["Y"], "output", code.IndexOf("\"String\""));

        Assert.AreEqual(expectedError, errors[0]);
    }

    [TestMethod]
    public void TestCyclicUnionsAndRecords()
    {
        string code =
            """
            union A(B, C);

            record B(C: C);

            record C(A: A);

            scene main { }
            """;

        Binder binder = PrepareBinder(code);

        List<Error> errors = [];
        binder.ErrorFound += errors.Add;

        _ = binder.Bind();

        Assert.AreEqual(1, errors.Count);

        Error expectedError = Errors.CyclicTypeDefinition(["C", "A", "B", "C"], code.IndexOf("record C"));

        Assert.AreEqual(expectedError, errors[0]);
    }

    [TestMethod]
    public void TestUnionCollapsing()
    {
        string code =
            """
            union W (Int, String);
            union X (Int, W);
            record Y(A: Int);
            union Z (X, Y);
            scene main { }
            """;

        Binder binder = PrepareBinder(code);
        binder.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        BindingResult result = binder.Bind();

        SymbolTable table = result.SymbolTable!;

        UnionTypeSymbol z = (UnionTypeSymbol)table["Z"];

        Assert.AreEqual(3, z.Subtypes.Length);

        IEnumerable<string> subtypeNames = z.Subtypes.Select(s => s.Name);

        Assert.IsTrue(subtypeNames.Contains("Int"));
        Assert.IsTrue(subtypeNames.Contains("String"));
        Assert.IsTrue(subtypeNames.Contains("Y"));
    }

    [TestMethod]
    public void TestUnionDuplicatedSubtype()
    {
        string code =
            """
            union X (Int, Int, String);
            scene main { }
            """;

        Binder binder = PrepareBinder(code);

        List<Error> errors = [];
        binder.ErrorFound += errors.Add;

        _ = binder.Bind();

        Assert.AreEqual(1, errors.Count);

        Error expectedError = Errors.UnionHasDuplicateSubtype("X", "Int", 0);

        Assert.AreEqual(expectedError, errors[0]);
    }

    [TestMethod]
    public void TestSpectrumDeclarations()
    {
        string code =
            """
            scene main
            {
                spectrum Relationship (Apart < 6/14, Neutral <= 19/21, Close);
            }
            """;

        Binder binder = PrepareBinder(code);
        binder.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        BindingResult result = binder.Bind();

        SceneSymbolDeclarationNode mainScene = (SceneSymbolDeclarationNode)((BoundSymbolDeclarationNode)result.BoundStory!.TopLevelNodes[0]).Declaration;

        BoundSpectrumDeclarationStatementNode relationshipDeclaration = (BoundSpectrumDeclarationStatementNode)mainScene.Body.Statements[0];

        Assert.AreEqual("Relationship", relationshipDeclaration.Spectrum.Name);
        Assert.AreEqual(3, relationshipDeclaration.Spectrum.OptionNames.Length);
        Assert.AreEqual(new SpectrumInterval { Inclusive = false, UpperNumerator = 18, UpperDenominator = 42 }, relationshipDeclaration.Spectrum.Intervals["Apart"]);
        Assert.AreEqual(new SpectrumInterval { Inclusive = true, UpperNumerator = 38, UpperDenominator = 42 }, relationshipDeclaration.Spectrum.Intervals["Neutral"]);
        Assert.AreEqual(new SpectrumInterval { Inclusive = true, UpperNumerator = 42, UpperDenominator = 42 }, relationshipDeclaration.Spectrum.Intervals["Close"]);
    }

    [TestMethod]
    public void TestInvalidSpectrums()
    {
        string code =
            """
            scene main
            {
                spectrum O (A <= 1/2, B) default C; // error: default option is not an option
                spectrum P (A <= 1/0, B); // error: divide by 0
                spectrum Q (A < 2/3, B < 1/2, C); // error: decreasing
                spectrum R (A <= 2/3, B <= 2/3, C); // error: not increasing
                spectrum S (A <= 2/3, B < 2/3, C); // error: decreasing
                spectrum T (A < 2/3, B <= 2/3, C); // okay
                spectrum U (A < 3/2, B); // error: greater than 1
                spectrum W (A <= 1/1, B); // error: not increasing
                spectrum X (A < 1/1, B); // okay
                spectrum Y (A <= 1/2, A); // error: duplicated option name
                spectrum Z (); // error: no options
            }
            """;

        Binder binder = PrepareBinder(code);

        List<Error> errors = [];
        binder.ErrorFound += errors.Add;

        _ = binder.Bind();

        void AssertIsError(int index, Error error)
        {
            Assert.AreEqual(error, errors[index]);
        }

        AssertIsError(0, Errors.OutcomeDefaultOptionNotAnOption("O", code.IndexOf("spectrum O")));
        AssertIsError(1, Errors.SpectrumBoundDivisionByZero("P", "A", code.IndexOf("A <= 1/0")));
        AssertIsError(2, Errors.SpectrumNotIncreasing("Q", (2, 3), (1, 2), code.IndexOf("B < 1/2, C); // error: decreasing")));
        AssertIsError(3, Errors.SpectrumNotIncreasing("R", (2, 3), (2, 3), code.IndexOf("B <= 2/3, C); // error: not increasing")));
        AssertIsError(4, Errors.SpectrumNotIncreasing("S", (2, 3), (2, 3), code.IndexOf("B < 2/3, C); // error: decreasing")));
        AssertIsError(5, Errors.SpectrumBoundNotInRange("U", "A", code.IndexOf("A < 3/2")));
        AssertIsError(6, Errors.SpectrumNotIncreasing("W", (1, 1), (1, 1), code.IndexOf("B); // error: not increasing")));
        AssertIsError(7, Errors.DuplicatedOptionInOutcomeDeclaration("A", code.IndexOf("spectrum Y")));
        AssertIsError(8, Errors.OutcomeWithZeroOptions("Z", code.IndexOf("spectrum Z")));
    }

    [TestMethod]
    public void TestValidSpectrumAdjustment()
    {
        string code =
            """
            scene main
            {
                spectrum X (A <= 1/2, B);

                strengthen X by 2;
                weaken X by 3;
            }
            """;

        Binder binder = PrepareBinder(code);
        binder.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        BindingResult result = binder.Bind();

        StoryNode boundStory = result.BoundStory!;

        SceneSymbolDeclarationNode mainScene = (SceneSymbolDeclarationNode)((BoundSymbolDeclarationNode)result.BoundStory!.TopLevelNodes[0]).Declaration;

        BoundSpectrumDeclarationStatementNode? boundSpectrumDeclaration = mainScene.Body.Statements[0] as BoundSpectrumDeclarationStatementNode;
        Assert.IsNotNull(boundSpectrumDeclaration);

        SpectrumSymbol symbol = boundSpectrumDeclaration.Spectrum;

        BoundSpectrumAdjustmentStatementNode? boundStrengthen = mainScene.Body.Statements[1] as BoundSpectrumAdjustmentStatementNode;
        Assert.IsNotNull(boundStrengthen);

        BoundSpectrumAdjustmentStatementNode? boundWeaken = mainScene.Body.Statements[2] as BoundSpectrumAdjustmentStatementNode;
        Assert.IsNotNull(boundWeaken);

        Assert.AreEqual(symbol, boundStrengthen.Spectrum);
        Assert.AreEqual(symbol, boundWeaken.Spectrum);
    }

    [TestMethod]
    public void TestIllegalSpectrumAdjustments()
    {
        string code =
            """
            scene main
            {
                spectrum X (A <= 1/2, B);

                strengthen Y by 2; // error: Y does not exist
                X = A; // error: can't assign to spectrum
                strengthen X by "xyz";
            }
            """;

        Binder binder = PrepareBinder(code);

        List<Error> errors = [];
        binder.ErrorFound += errors.Add;

        BindingResult result = binder.Bind();

        void AssertIsError(int index, Error error)
        {
            Assert.AreEqual(error, errors[index]);
        }

        AssertIsError(0, Errors.SymbolDoesNotExistInScope("Y", code.IndexOf("strengthen Y")));
        AssertIsError(1, Errors.SymbolCannotBeAssignedTo("X", code.IndexOf("X = A;")));
        AssertIsError(2, Errors.IncompatibleType((TypeSymbol)result.SymbolTable!["String"], (TypeSymbol)result.SymbolTable!["Int"], "strengthen/weaken amount", code.IndexOf("\"xyz\"")));
    }

    [TestMethod]
    public void TestMultipleScenes()
    {
        string code =
            """
            scene A
            {
                output 0;
            }
            
            scene B
            {
                output 1;
            }
            
            scene C
            {
                output 2;
            }

            scene main { }
            """;

        Binder binder = PrepareBinder(code);
        binder.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        BindingResult result = binder.Bind();

        Assert.IsTrue(result.IsValid);

        SymbolTable table = result.SymbolTable;

        for (int i = 0; i < 3; i++)
        {
            string name = ((char)(i + 'A')).ToString();

            Assert.IsTrue(table.IsDeclared(name));

            SceneSymbol? scene = table[name] as SceneSymbol;
            Assert.IsNotNull(scene);

            Assert.AreEqual(name, scene.Name);

            BoundSymbolDeclarationNode? boundScene = result.BoundStory.TopLevelNodes[i] as BoundSymbolDeclarationNode;
            Assert.IsNotNull(boundScene);

            Assert.AreEqual(scene, boundScene.Symbol);

            SceneSymbolDeclarationNode? sceneDeclaration = boundScene.Declaration as SceneSymbolDeclarationNode;
            Assert.IsNotNull(sceneDeclaration);
        }
    }

    [TestMethod]
    public void TestValidCallStatement()
    {
        string code =
            """
            scene main
            {
                call A;
            }

            scene A { }
            """;

        Binder binder = PrepareBinder(code);
        binder.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        BindingResult result = binder.Bind();
        Assert.IsTrue(result.IsValid);

        SceneSymbolDeclarationNode mainScene = (SceneSymbolDeclarationNode)((BoundSymbolDeclarationNode)result.BoundStory.TopLevelNodes[0]).Declaration;

        Assert.AreEqual(1, mainScene.Body.Statements.Length);
        Assert.IsTrue(mainScene.Body.Statements[0] is BoundCallStatementNode
        {
            Scene.Name: "A",
            SceneName: "A",
        });
    }

    [TestMethod]
    public void TestGlobalOutcomes()
    {
        string code =
            """
            outcome Engaged(Yes, No);
            spectrum Relationship(Apart < 3/10, Neutral <= 7/10, Close);

            scene main
            {
                call A;

                branchon Engaged
                {
                    option Yes { }
                    option No { }
                }

                branchon Relationship
                {
                    option Apart { }
                    option Neutral { }
                    option Close { }
                }
            }

            scene A
            {
                Engaged = Yes;
                strengthen Relationship by 10;
            }
            """;

        Binder binder = PrepareBinder(code);
        binder.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        StoryNode story = binder.Bind().BoundStory!;

        Assert.IsTrue(story.TopLevelNodes[0] is BoundSymbolDeclarationNode
        {
            Symbol: OutcomeSymbol
            {
                Name: "Engaged",
                OptionNames: ["Yes", "No"],
                DefaultOption: null,
                AlwaysAssigned: false,
            },
            Declaration: OutcomeSymbolDeclarationNode
            {
                Name: "Engaged"
            }
        });

        Assert.IsTrue(story.TopLevelNodes[1] is BoundSymbolDeclarationNode
        {
            Symbol: SpectrumSymbol
            {
                Name: "Relationship",
                OptionNames: ["Apart", "Neutral", "Close"],
                DefaultOption: null,
            }
        });
    }

    [TestMethod]
    public void TestValidNamespace()
    {
        string code =
            """
            setting Namespace: "MyStory .    StoryLogic";

            scene main { }
            """;

        Binder binder = PrepareBinder(code);
        binder.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        BindingResult result = binder.Bind();

        Assert.IsNotNull(result.Settings);
        Assert.AreEqual("MyStory.StoryLogic", result.Settings.Namespace);
    }

    [TestMethod]
    public void TestInvalidNamespace()
    {
        string code =
            """
            setting Namespace: "Let's just write some nonsense :)";

            scene main { }
            """;

        Binder binder = PrepareBinder(code);

        List<Error> errors = [];
        binder.ErrorFound += errors.Add;

        _ = binder.Bind();

        Assert.AreEqual(1, errors.Count);

        Error expectedError = Errors.InvalidNamespaceFormat("Let's just write some nonsense :)", code.IndexOf("\"Let's"));
        Assert.AreEqual(expectedError, errors[0]);
    }

    [TestMethod]
    public void TestNamespaceNotString()
    {
        string code =
            """
            setting Namespace: 2;

            scene main { }
            """;

        Binder binder = PrepareBinder(code);

        List<Error> errors = [];
        binder.ErrorFound += errors.Add;

        BindingResult result = binder.Bind();

        Assert.AreEqual(1, errors.Count);

        Error expectedError = Errors.IncompatibleType((TypeSymbol)result.SymbolTable!["Int"], (TypeSymbol)result.SymbolTable!["String"], "setting", code.IndexOf('2'));
        Assert.AreEqual(expectedError, errors[0]);
    }

    [TestMethod]
    public void TestForbiddenNamespaces()
    {
        void Test(string forbiddenNamespace)
        {
            string code =
                $$"""
                setting Namespace: "{{forbiddenNamespace}}.Story";

                scene main { }
                """;

            Binder binder = PrepareBinder(code);

            List<Error> errors = [];
            binder.ErrorFound += errors.Add;

            _ = binder.Bind();

            Assert.AreEqual(1, errors.Count);

            Error expectedError = Errors.ForbiddenNamespace($"{forbiddenNamespace}.Story", code.IndexOf($"\"{forbiddenNamespace}"));
            Assert.AreEqual(expectedError, errors[0]);
        }

        Test(nameof(System));
        Test(nameof(Microsoft));
        Test(nameof(Phantonia));
    }

    [TestMethod]
    public void TestStoryName()
    {
        string code =
            """
            setting StoryName: "MyStory";

            scene main { }
            """;

        Binder binder = PrepareBinder(code);
        binder.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        BindingResult result = binder.Bind();

        Assert.IsNotNull(result.Settings);
        Assert.AreEqual("MyStory", result.Settings.StoryName);
    }

    [TestMethod]
    public void TestNonIdentifierStoryName()
    {
        string code =
            """
            setting StoryName: "Let's just write some nonsense :)";

            scene main { }
            """;

        Binder binder = PrepareBinder(code);

        List<Error> errors = [];
        binder.ErrorFound += errors.Add;

        _ = binder.Bind();

        Assert.AreEqual(1, errors.Count);

        Error expectedError = Errors.InvalidStoryName("Let's just write some nonsense :)", code.IndexOf("\"Let's"));
        Assert.AreEqual(expectedError, errors[0]);
    }

    [TestMethod]
    public void TestNonStringStoryName()
    {
        string code =
            """
            setting StoryName: 4;

            scene main { }
            """;

        Binder binder = PrepareBinder(code);

        List<Error> errors = [];
        binder.ErrorFound += errors.Add;

        BindingResult result = binder.Bind();

        Assert.AreEqual(1, errors.Count);

        Error expectedError = Errors.IncompatibleType((TypeSymbol)result.SymbolTable!["Int"], (TypeSymbol)result.SymbolTable!["String"], "setting", code.IndexOf('4'));
        Assert.AreEqual(expectedError, errors[0]);
    }

    [TestMethod]
    public void TestConflictingStoryName()
    {
        string code =
            """
            setting StoryName: "X";

            record X(Text: String);

            scene main { }
            """;

        Binder binder = PrepareBinder(code);

        List<Error> errors = [];
        binder.ErrorFound += errors.Add;

        _ = binder.Bind();

        Assert.AreEqual(1, errors.Count);

        Error expectedError = Errors.ConflictingStoryName("X", code.IndexOf("\"X\""));
        Assert.AreEqual(expectedError, errors[0]);
    }

    [TestMethod]
    public void TestConflictingNames()
    {
        // very normal code to write, i know
        string code =
            """
            record op_Equality(Value: Int);
            record XDiscriminator(Value: Int);
            record AsObject(Value: Int);

            // looks so weird getting the indices
            union X(op_Equality //
            , XDiscriminator //
            , AsObject //
            );

            record Y(MemberwiseClone: Int, Y: Int);

            scene main { }
            """;

        Binder binder = PrepareBinder(code);

        List<Error> errors = [];
        binder.ErrorFound += errors.Add;

        _ = binder.Bind();

        void AssertWrongSubtype(int i, string subtypeName)
        {
            Error expectedError = Errors.ConflictingUnionSubtype("X", subtypeName, code.IndexOf($"{subtypeName} //"));
            Assert.AreEqual(errors[i], expectedError);
        }

        void AssertWrongProperty(int i, string propertyName)
        {
            Error expectedError = Errors.ConflictingRecordProperty("Y", propertyName, code.IndexOf(propertyName + ':'));
            Assert.AreEqual(errors[i], expectedError);
        }

        AssertWrongSubtype(0, "op_Equality");
        AssertWrongSubtype(1, "XDiscriminator");
        AssertWrongSubtype(2, "AsObject");
        AssertWrongProperty(3, "MemberwiseClone");
        AssertWrongProperty(4, "Y");
    }

    [TestMethod]
    public void TestBindingMultipleTrees()
    {
        string codeA =
            """
            scene main
            {
                output Line("abc", 2);
                call A;
            }
            """;

        string codeB =
            """
            record Line(Text: String, Character: Int);

            setting OutputType: Line;
            """;

        string codeC =
            """
            scene A
            {
                output Line("xyz", 3);
            }
            """;

        int offsetB = codeA.Length;
        int offsetC = codeB.Length + offsetB;

        StoryNode Parse(string code, int offset)
        {
            Lexer lexer = new(code, offset);
            Parser parser = new(lexer.Lex());
            parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

            return parser.Parse();
        }

        StoryNode[] stories =
        [
            Parse(codeA, 0),
            Parse(codeB, offsetB),
            Parse(codeC, offsetC),
        ];

        int storyAMaxIndex = stories[0].FlattenHierarchie().Max(n => n.Index);
        int storyBMinIndex = stories[1].FlattenHierarchie().Min(n => n.Index);
        int storyBMaxIndex = stories[1].FlattenHierarchie().Max(n => n.Index);
        int storyCMinIndex = stories[2].FlattenHierarchie().Min(n => n.Index);

        Assert.IsTrue(storyAMaxIndex < storyBMinIndex);
        Assert.IsTrue(storyBMaxIndex < storyCMinIndex);

        Binder binder = new(stories);
        binder.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(codeA + codeB + codeC, e));

        // Assert this runs without errors or exceptions
        _ = binder.Bind();
    }

    [TestMethod]
    public void TestEnum()
    {
        string code =
            """
            enum Character (Alice, Beverly, Charlotte);
            setting OutputType: Character;

            scene main
            {
                output Character.Alice;
                output Character.Beverly;
                output Character.Charlotte;
            }
            """;

        Binder binder = PrepareBinder(code);
        binder.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        BindingResult result = binder.Bind();

        Assert.IsTrue(result.IsValid);

        EnumTypeSymbol characterType = (EnumTypeSymbol)result.SymbolTable["Character"];
        string[] optionNames = ["Alice", "Beverly", "Charlotte"];

        BoundSymbolDeclarationNode boundEnumDeclaration = (BoundSymbolDeclarationNode)result.BoundStory.TopLevelNodes[0];
        EnumSymbolDeclarationNode enumDeclaration = (EnumSymbolDeclarationNode)boundEnumDeclaration.Declaration;

        Assert.AreEqual("Character", enumDeclaration.Name);
        Assert.IsTrue(optionNames.SequenceEqual(enumDeclaration.Options));
        Assert.AreEqual(characterType, boundEnumDeclaration.Symbol);

        BoundSymbolDeclarationNode boundMainScene = (BoundSymbolDeclarationNode)result.BoundStory.TopLevelNodes[^1];
        SceneSymbolDeclarationNode mainScene = (SceneSymbolDeclarationNode)boundMainScene.Declaration;

        for (int i = 0; i < 3; i++)
        {
            OutputStatementNode outputStatement = (OutputStatementNode)mainScene.Body.Statements[i];
            TypedExpressionNode typedExpression = (TypedExpressionNode)outputStatement.OutputExpression;

            Assert.AreEqual(characterType, typedExpression.SourceType);
            Assert.AreEqual(characterType, typedExpression.TargetType);

            BoundEnumOptionExpressionNode enumOptionExpression = (BoundEnumOptionExpressionNode)typedExpression.Expression;

            Assert.AreEqual("Character", enumOptionExpression.EnumName);
            Assert.AreEqual(optionNames[i], enumOptionExpression.OptionName);
            Assert.AreEqual(characterType, enumOptionExpression.EnumSymbol);
        }
    }

    [TestMethod]
    public void TestLoopSwitch()
    {
        string code =
            """
            setting OutputType: String;
            setting OptionType: String;

            scene main
            {
                loop switch ("loop switch")
                {
                    option ("normal option")
                    {
                        output "output in normal option";
                    }

                    loop option ("loop option")
                    {
                        output "output in loop option";
                    }

                    final option ("final option")
                    {
                        output "output in final option";
                    }
                }
            }
            """;

        Binder binder = PrepareBinder(code);
        binder.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        _ = binder.Bind();
    }

    [TestMethod]
    public void TestInvalidLoopSwitch()
    {
        string code =
            """
            scene main
            {
                // will loop indefinitely
                loop switch (0)
                {
                    option (1) { }
                    loop option (2) { }
                }
            }
            """;

        Binder binder = PrepareBinder(code);

        List<Error> errors = [];
        binder.ErrorFound += errors.Add;

        _ = binder.Bind();

        Assert.AreEqual(1, errors.Count);

        Error expectedError = Errors.LoopSwitchHasToTerminate(code.IndexOf("loop switch"));
        Assert.AreEqual(expectedError, errors[0]);
    }

    [TestMethod]
    public void TestReferences()
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

        Binder binder = PrepareBinder(code);
        binder.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        BindingResult result = binder.Bind();

        Assert.IsTrue(result.IsValid);

        InterfaceSymbol? intface = result.SymbolTable["ICharacter"] as InterfaceSymbol;
        Assert.IsNotNull(intface);

        SceneSymbolDeclarationNode? mainScene = (result.BoundStory.TopLevelNodes[4] as BoundSymbolDeclarationNode)?.Declaration as SceneSymbolDeclarationNode;
        Assert.IsNotNull(mainScene);

        BoundRunStatementNode? runStatement = mainScene.Body.Statements[0] as BoundRunStatementNode;
        Assert.IsNotNull(runStatement);

        Assert.AreEqual(result.SymbolTable["Character"], runStatement.Reference);
        Assert.AreEqual(intface.Methods[0], runStatement.Method);
        Assert.AreEqual(1, runStatement.Arguments.Length);
        Assert.IsTrue(runStatement.Arguments[0].Expression is TypedExpressionNode { SourceType: BuiltinTypeSymbol { Type: BuiltinType.String } });

        BoundChooseStatementNode? chooseStatement = mainScene.Body.Statements[1] as BoundChooseStatementNode;
        Assert.IsNotNull(chooseStatement);

        Assert.AreEqual(result.SymbolTable["Character"], chooseStatement.Reference);
        Assert.AreEqual(intface.Methods[1], chooseStatement.Method);
        Assert.AreEqual(1, chooseStatement.Arguments.Length);
        Assert.IsTrue(chooseStatement.Arguments[0].Expression is TypedExpressionNode { SourceType: BuiltinTypeSymbol { Type: BuiltinType.String } });
    }

    [TestMethod]
    public void TestReferenceErrors()
    {
        string code =
            """
            interface I
            (
                action A(x: Int),
                choice B(y: Int),
            );

            reference R: X; // interface X does not exist
            reference S: String; // String is not an interface
            reference T: I;

            setting OptionType: Int;

            scene main
            {
                run X.A(0); // reference X does not exist
                run T.X(1); // method X does not exist
                run T.B(2); // method B is not an action
                // run T.A(); // method A takes 1 parameter
                run T.A(3, 4); // method A takes 1 parameter
                run T.A("Hello world"); // method A takes Int parameter

                choose X.B(5) { option (100) { } } // reference X does not exist
                choose T.X(6) { option (100) { } } // method X does not exist
                choose T.A(7) { option (100) { } } // method A is not a choice
                // choose T.B() { option (100) { } } // method B takes 1 parameter
                choose T.B(8, 9) { option (100) { } } // method B takes 1 parameter
                choose T.B("What's up world") { option (100) { } } // method B takes Int parameter
                choose T.B(10) { option ("Goodbye world") { } } // option type is Int
            }
            """;

        Binder binder = PrepareBinder(code);

        List<Error> errors = [];
        binder.ErrorFound += errors.Add;

        BindingResult result = binder.Bind();

        Assert.IsNotNull(result.SymbolTable);

        List<Error> expectedErrors =
        [
            // declarations
            Errors.SymbolDoesNotExistInScope("X", code.IndexOf("reference R")),
            Errors.SymbolIsNotInterface("String", code.IndexOf("reference S")),

            // run
            Errors.SymbolDoesNotExistInScope("X", code.IndexOf("run X.A(0);")),
            Errors.MethodDoesNotExistInInterface("T", "I", "X", code.IndexOf("run T.X(1);")),
            Errors.CannotRunChoiceMethod("I", "B", code.IndexOf("run T.B(2);")),
            Errors.WrongAmountOfArgumentsInMethodCall("I", "A", 2, 1, code.IndexOf("run T.A(3, 4);")),
            Errors.IncompatibleType((TypeSymbol)result.SymbolTable["String"], (TypeSymbol)result.SymbolTable["Int"], "parameter", code.IndexOf(@"""Hello world""")),

            // choose
            Errors.SymbolDoesNotExistInScope("X", code.IndexOf("choose X.B(5)")),
            Errors.MethodDoesNotExistInInterface("T", "I", "X", code.IndexOf("choose T.X(6)")),
            Errors.CannotChooseFromActionMethod("I", "A", code.IndexOf("choose T.A(7)")),
            Errors.WrongAmountOfArgumentsInMethodCall("I", "B", 2, 1, code.IndexOf("choose T.B(8, 9)")),
            Errors.IncompatibleType((TypeSymbol)result.SymbolTable["String"], (TypeSymbol)result.SymbolTable["Int"], "parameter", code.IndexOf(@"""What's up world""")),
            Errors.IncompatibleType((TypeSymbol)result.SymbolTable["String"], (TypeSymbol)result.SymbolTable["Int"], "option", code.IndexOf(@"""Goodbye world""")),
        ];

        Assert.IsTrue(errors.SequenceEqual(expectedErrors));
    }

    [TestMethod]
    public void TestIfStatement()
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
                else if X is C
                {
                    output 1;
                }
                else
                {
                    output 2;
                }
            }
            """;

        Binder binder = PrepareBinder(code);
        binder.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        BindingResult result = binder.Bind();

        TypeSymbol booleanType = (TypeSymbol)result.SymbolTable!["Boolean"];
        OutcomeSymbol xOutcome = (OutcomeSymbol)result.SymbolTable["X"];

        SceneSymbolDeclarationNode mainScene = (SceneSymbolDeclarationNode)((BoundSymbolDeclarationNode)result.BoundStory!.TopLevelNodes[1]).Declaration;

        IfStatementNode ifStatement = (IfStatementNode)mainScene.Body.Statements[1];

        if (ifStatement.Condition is not TypedExpressionNode
            {
                SourceType: TypeSymbol conditionType,
                Expression: LogicExpressionNode
                {
                    LeftExpression: ExpressionNode leftHandSide,
                    RightExpression: ExpressionNode rightHandSide,
                },
            })
        {
            Assert.Fail();
            return;
        }

        Assert.AreEqual(booleanType, conditionType);

        if (leftHandSide is not TypedExpressionNode
            {
                SourceType: TypeSymbol leftHandType,
                Expression: BoundIsExpressionNode
                {
                    Outcome: OutcomeSymbol leftHandOutcome,
                },
            })
        {
            Assert.Fail();
            return;
        }

        Assert.AreEqual(booleanType, leftHandType);
        Assert.AreEqual(xOutcome, leftHandOutcome);

        if (rightHandSide is not TypedExpressionNode
            {
                SourceType: TypeSymbol rightHandType,
                Expression: BoundIsExpressionNode
                {
                    Outcome: OutcomeSymbol rightHandOutcome,
                },
            })
        {
            Assert.Fail();
            return;
        }

        Assert.AreEqual(booleanType, rightHandType);
        Assert.AreEqual(xOutcome, rightHandOutcome);
    }
}
