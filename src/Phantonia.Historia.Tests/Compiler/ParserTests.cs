using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language;
using Phantonia.Historia.Language.GrammaticalAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;
using Phantonia.Historia.Language.GrammaticalAnalysis.Statements;
using Phantonia.Historia.Language.GrammaticalAnalysis.Symbols;
using Phantonia.Historia.Language.LexicalAnalysis;

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

        Assert.IsTrue(outputStatement is { Expression: IntegerLiteralExpressionNode { Value: 42 } });

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
                                    Expression: IntegerLiteralExpressionNode
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
                                                        Expression: IntegerLiteralExpressionNode
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
                                                        Expression: IntegerLiteralExpressionNode
                                                        {
                                                            Value: 8
                                                        }
                                                    },
                                                    OutputStatementNode
                                                    {
                                                        Expression: IntegerLiteralExpressionNode
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
}
