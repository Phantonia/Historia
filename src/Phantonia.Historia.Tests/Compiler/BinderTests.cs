using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language;
using Phantonia.Historia.Language.GrammaticalAnalysis;
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
        (StoryNode? boundStory, SymbolTable? symbolTable) = result;

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
        (StoryNode? boundStory, SymbolTable? symbolTable) = result;

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
                A: A;
            }
            """;

        Binder binder = PrepareBinder(code);

        List<Error> errors = new();
        binder.ErrorFound += errors.Add;

        BindingResult result = binder.Bind();
        Assert.IsFalse(result.IsValid);

        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual("Cyclic record definition", errors[0].ErrorMessage);
        Assert.IsTrue(errors[0].Index == code.IndexOf("record A") || errors[0].Index == code.IndexOf("record B"));
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

        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual("Cyclic record definition", errors[0].ErrorMessage);
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

        BindingResult result = binder.Bind();

        // TODO: actually test this
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

        //Assert.IsFalse(bindingResult.IsValid);
        //Assert.IsNull(bindingResult.BoundStory);
        //Assert.IsNull(bindingResult.SymbolTable);

        //Assert.IsTrue(errors.Count > 0);

        // TODO: actually test this
    }
}
