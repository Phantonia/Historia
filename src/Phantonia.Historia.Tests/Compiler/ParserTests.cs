using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language;
using Phantonia.Historia.Language.GrammaticalAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;
using Phantonia.Historia.Language.GrammaticalAnalysis.Statements;
using Phantonia.Historia.Language.GrammaticalAnalysis.Symbols;
using Phantonia.Historia.Language.GrammaticalAnalysis.Types;
using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Immutable;

namespace Phantonia.Historia.Tests.Compiler;

[TestClass]
public sealed class ParserTests
{
    [TestMethod]
    public void TestSimpleProgram()
    {
        string code = """
                      scene main
                      {
                          output (42);
                          output 64;
                      }
                      """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());

        StoryNode story = parser.Parse();

        Assert.AreEqual(1, story.Symbols.Length);
        SceneSymbolDeclarationNode? scene = story.Symbols[0] as SceneSymbolDeclarationNode;
        Assert.IsNotNull(scene);

        Assert.AreEqual(2, scene.Body.Statements.Length);
        OutputStatementNode? outputStatement = scene.Body.Statements[0] as OutputStatementNode;
        Assert.IsNotNull(outputStatement);

        Assert.IsTrue(outputStatement is { OutputExpression: IntegerLiteralExpressionNode { Value: 42 } });

        Assert.IsTrue(scene.Body.Statements[1] is OutputStatementNode);
    }

    [TestMethod]
    public void TestError()
    {
        string code = """
                      scene main { }
                      hello!
                      """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());

        bool foundError = false;

        parser.ErrorFound += e =>
        {
            if (!foundError)
            {
                Assert.AreEqual("Error: Unexpected token 'hello'\r\nhello!\r\n^", Errors.GenerateFullMessage(code, e));
            }
            else
            {
                Assert.AreEqual("Error: Unexpected token '!'\r\nhello!\r\n     ^", Errors.GenerateFullMessage(code, e));
            }

            foundError = true;
        };

        _ = parser.Parse();

        Assert.IsTrue(foundError);
    }

    [TestMethod]
    public void TestEarlyExit()
    {
        string code = """
                      scene main
                      {
                          output 0;
                      """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());

        int errorCount = 0;

        parser.ErrorFound += e =>
        {
            Assert.AreEqual("Error: Unexpected end of file\r\n    output 0;\r\n             ^", Errors.GenerateFullMessage(code, e));
            errorCount++;
        };

        StoryNode story = parser.Parse(); // assert this does not throw

        Assert.AreEqual(1, errorCount);
    }

    [TestMethod]
    public void TestSwitchParsing()
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
                      }
                      """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail();

        StoryNode story = parser.Parse();

        // trust me, this is the cleanest way to write that down
        // actually that's very cool, it's just that we have a lot of nesting
        if (story is not
            {
                Symbols:
                [
                    SceneSymbolDeclarationNode
                {
                    Name: "main",
                    Body: StatementBodyNode
                    {
                        Statements:
                            [
                                SwitchStatementNode
                            {
                                OutputExpression: IntegerLiteralExpressionNode
                                {
                                    Value: 4
                                },
                                Options:
                                    [
                                        OptionNode
                                    {
                                        Expression: IntegerLiteralExpressionNode
                                        {
                                            Value: 5
                                        },
                                        Body: StatementBodyNode
                                        {
                                            Statements:
                                                [
                                                    OutputStatementNode
                                                {
                                                    OutputExpression: IntegerLiteralExpressionNode
                                                    {
                                                        Value: 6
                                                    }
                                                }
                                                ]
                                        }
                                    },
                                        OptionNode
                                    {
                                        Expression: IntegerLiteralExpressionNode
                                        {
                                            Value: 7
                                        },
                                        Body: StatementBodyNode
                                        {
                                            Statements:
                                                [
                                                    OutputStatementNode
                                                {
                                                    OutputExpression: IntegerLiteralExpressionNode
                                                    {
                                                        Value: 8
                                                    }
                                                },
                                                    OutputStatementNode
                                                {
                                                    OutputExpression: IntegerLiteralExpressionNode
                                                    {
                                                        Value: 9
                                                    }
                                                }
                                                ]
                                        }
                                    }
                                    ]
                            }
                            ]
                    }
                }
                ]
            })
        {
            Assert.Fail();
        }
    }

    [TestMethod]
    public void TestSettings()
    {
        ImmutableArray<Setting> settings = new[]
        {
            new Setting { Kind = SettingKind.TypeArgument, Name = SettingName.OutputType },
            new Setting { Kind = SettingKind.TypeArgument, Name = SettingName.OptionType },
        }.ToImmutableArray();

        string code =
            """
            setting OutputType: Int;
            setting OptionType: String;

            scene main { }
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex(), settings);
        parser.ErrorFound += e => Assert.Fail();

        StoryNode story = parser.Parse();

        Assert.AreEqual(3, story.Symbols.Length);

        Assert.IsTrue(story.Symbols is
        [
            TypeSettingDeclarationNode
            {
                SettingName: SettingName.OutputType,
                Type: IdentifierTypeNode
                {
                    Identifier: "Int",
                }
            },
            TypeSettingDeclarationNode
            {
                SettingName: SettingName.OptionType,
                Type: IdentifierTypeNode
                {
                    Identifier: "String",
                }
            },
            SceneSymbolDeclarationNode
        ]);
    }

    [TestMethod]
    public void TestSettingMissingColonErrors()
    {
        ImmutableArray<Setting> settings = new[]
        {
            new Setting { Kind = SettingKind.TypeArgument, Name = SettingName.OutputType },
            new Setting { Kind = SettingKind.TypeArgument, Name = SettingName.OptionType },
        }.ToImmutableArray();

        string code =
            """
            setting OutputType Int;
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex(), settings);

        int errorCount = 0;

        parser.ErrorFound += e =>
        {
            errorCount++;
            Assert.AreEqual("Expected a Colon", e.ErrorMessage);
            Assert.AreEqual(code.IndexOf("Int"), e.Index);
        };

        _ = parser.Parse();

        Assert.AreEqual(1, errorCount);
    }

    [TestMethod]
    public void TestSettingMissingTypeError()
    {
        ImmutableArray<Setting> settings = new[]
        {
            new Setting { Kind = SettingKind.TypeArgument, Name = SettingName.OutputType },
            new Setting { Kind = SettingKind.TypeArgument, Name = SettingName.OptionType },
        }.ToImmutableArray();

        string code =
            """
            setting OutputType: ;
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex(), settings);

        int errorCount = 0;

        parser.ErrorFound += e =>
        {
            errorCount++;
            Assert.AreEqual("Unexpected token ';'", e.ErrorMessage);
            Assert.AreEqual(code.IndexOf(";"), e.Index);
        };

        _ = parser.Parse();

        Assert.AreEqual(1, errorCount);
    }
}
