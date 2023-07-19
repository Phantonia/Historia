using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language;
using Phantonia.Historia.Language.GrammaticalAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;
using Phantonia.Historia.Language.GrammaticalAnalysis.Statements;
using Phantonia.Historia.Language.GrammaticalAnalysis.TopLevel;
using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Phantonia.Historia.Tests.Compiler;

[TestClass]
public sealed class BinderTests
{
    private Binder PrepareBinder(string code)
    {
        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail($"Error: {e.ErrorMessage}");

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
            record Line
            {
                Text: String;
                Character: Int;
            }

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

            record A
            {
                B: B;
            }

            record B
            {
                C: C;
            }

            record C
            {
                A: A;
            }
            """;

        Binder binder = PrepareBinder(code);

        List<Error> errors = new();
        binder.ErrorFound += errors.Add;

        BindingResult result = binder.Bind();
        Assert.IsFalse(result.IsValid);

        Error expectedError = Errors.CyclicRecordDeclaration(new[] { "C", "A", "B", "C" }, code.IndexOf("record C"));

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

            record A
            {
                Self: A;
            }
            """;

        Binder binder = PrepareBinder(code);

        List<Error> errors = new();
        binder.ErrorFound += errors.Add;

        BindingResult result = binder.Bind();
        Assert.IsFalse(result.IsValid);

        Error expectedError = Errors.CyclicRecordDeclaration(new[] { "A", "A" }, index: code.IndexOf("record A"));

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

            record Line
            {
                Text: String;
                Character: Int;
            }

            record StageDirection
            {
                Direction: String;
                Character: Int;
            }

            record Moment
            {
                Line: Line;
                StageDirection: StageDirection;
            }
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
    public void TestBindingAndTypeChecking()
    {
        string code =
            """
            setting OutputType: Line;

            record Line
            {
                Text: String;
                Character: Int;
            }

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

            Assert.IsTrue(typedExpression.Type is RecordTypeSymbol
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
                            Type: BuiltinTypeSymbol
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

            record Line
            {
                Text: String;
                Character: Int;
            }

            scene main
            {
                output 2;
                output "xyz";
                output Line(2, 1);
                output Line("Hello");
            }
            """;

        Binder binder = PrepareBinder(code);
        List<Error> errors = new();
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

        Error fourthError = Errors.WrongAmountOfArguments("Line", givenAmount: 1, expectedAmount: 2, code.IndexOf("Line(\"Hello\");"));
        Assert.AreEqual(fourthError, errors[3]);
    }
}
