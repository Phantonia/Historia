using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language;
using Phantonia.Historia.Language.GrammaticalAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis.Symbols;
using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Phantonia.Historia.Tests.Compiler;

[TestClass]
public sealed class BinderTests
{
    [TestMethod]
    public void TestNoMain()
    {
        string code =
            """
            option OutputType: String;
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        Binder binder = new(parser.Parse());

        int errorCount = 0;

        binder.ErrorFound += e =>
        {
            errorCount++;

            const string ErrorMessage = """
                                        Error: A story needs a main scene
                                        option OutputType: String;
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

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        Binder binder = new(parser.Parse());

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
        ImmutableArray<Setting> settings = new[]
        {
            new Setting { Kind = SettingKind.TypeArgument, Name = SettingName.OptionType },
            new Setting { Kind = SettingKind.TypeArgument, Name = SettingName.OutputType },
        }.ToImmutableArray();

        string code =
            """
            setting OutputType: String;
            setting OptionType: Int;
            scene main { }
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex(), settings);
        parser.ErrorFound += e => Assert.Fail($"Error: {e.ErrorMessage}");

        Binder binder = new(parser.Parse());

        BindingResult result = binder.Bind();
        Assert.IsTrue(result.IsValid);
        (StoryNode? boundStory, SymbolTable? symbolTable) = result;

        Assert.AreEqual(3, boundStory!.Symbols.Length);

        Assert.IsTrue(boundStory.Symbols[0] is TypeSettingDeclarationNode
        {
            Type: BoundTypeNode
            {
                Symbol: BuiltinTypeSymbol
                {
                    Type: BuiltinType.String,
                }
            }
        });

        Assert.IsTrue(boundStory.Symbols[1] is TypeSettingDeclarationNode
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

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail($"Error: {e.ErrorMessage}");

        Binder binder = new(parser.Parse());

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

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail($"Error: {e.ErrorMessage}");

        Binder binder = new(parser.Parse());
        binder.ErrorFound += e => Assert.Fail($"Error: {e.ErrorMessage}");

        BindingResult result = binder.Bind();
        Assert.IsTrue(result.IsValid);
        (StoryNode? boundStory, SymbolTable? symbolTable) = result;

        Assert.AreEqual(2, boundStory!.Symbols.Length);

        Assert.IsTrue(boundStory.Symbols[0] is BoundSymbolDeclarationNode
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

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail($"Error: {e.ErrorMessage}");

        Binder binder = new(parser.Parse());

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

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail($"Error: {e.ErrorMessage}");

        Binder binder = new(parser.Parse());

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

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail($"Error: {e.ErrorMessage}");

        Binder binder = new(parser.Parse());

        binder.ErrorFound += e => Assert.Fail($"Error: {e.ErrorMessage}");

        BindingResult result = binder.Bind();
        Assert.IsTrue(result.IsValid);

        Assert.AreEqual(4, result.BoundStory.Symbols.Length);

        Assert.IsTrue(result.BoundStory.Symbols[1] is BoundSymbolDeclarationNode
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

        Assert.IsTrue(result.BoundStory.Symbols[2] is BoundSymbolDeclarationNode
        {
            Declaration: RecordSymbolDeclarationNode,
            Symbol: RecordTypeSymbol,
        });

        Assert.IsTrue(result.BoundStory.Symbols[3] is BoundSymbolDeclarationNode
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
}
