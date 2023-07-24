﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language;
using Phantonia.Historia.Language.GrammaticalAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;
using Phantonia.Historia.Language.GrammaticalAnalysis.Statements;
using Phantonia.Historia.Language.GrammaticalAnalysis.TopLevel;
using Phantonia.Historia.Language.GrammaticalAnalysis.Types;
using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
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

        Assert.AreEqual(1, story.TopLevelNodes.Length);
        SceneSymbolDeclarationNode? scene = story.TopLevelNodes[0] as SceneSymbolDeclarationNode;
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

        List<Error> errors = new();

        parser.ErrorFound += errors.Add;

        _ = parser.Parse();

        Assert.AreEqual(2, errors.Count);

        Error firstError = Errors.UnexpectedToken(new Token
        {
            Kind = TokenKind.Identifier,
            Text = "hello",
            Index = code.IndexOf("hello!"),
        });

        Error secondError = Errors.UnexpectedToken(new Token
        {
            Kind = TokenKind.Unknown,
            Text = "!",
            Index = code.IndexOf("!"),
        });

        Assert.AreEqual(firstError, errors[0]);
        Assert.AreEqual(secondError, errors[1]);
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
            Error expectedError = Errors.UnexpectedEndOfFile(new Token
            {
                Kind = TokenKind.EndOfFile,
                Index = code.Length,
                Text = "",
            });

            Assert.AreEqual(expectedError, e);
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
        parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        StoryNode story = parser.Parse();

        // trust me, this is the cleanest way to write that down
        // actually that's very cool, it's just that we have a lot of nesting
        if (story is not
            {
                TopLevelNodes:
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
                                        SwitchOptionNode
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
                                        SwitchOptionNode
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
    public void TestNamedSwitchParsing()
    {
        string code =
            """
            scene main
            {
                switch MySwitch (4)
                {
                    option MyOptionA (5)
                    {
                        output 6;
                    }

                    option MyOptionB (7)
                    {
                        output 8;
                        output 9;
                    }
                }
            }
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        StoryNode story = parser.Parse();

        Assert.AreEqual(1, story.TopLevelNodes.Length);

        SceneSymbolDeclarationNode? mainScene = story.TopLevelNodes[0] as SceneSymbolDeclarationNode;
        Assert.IsNotNull(mainScene);
        Assert.AreEqual("main", mainScene.Name);

        Assert.AreEqual(1, mainScene.Body.Statements.Length);

        SwitchStatementNode? switchStatement = mainScene.Body.Statements[0] as SwitchStatementNode;
        Assert.IsNotNull(switchStatement);

        Assert.AreEqual("MySwitch", switchStatement.Name);
        Assert.AreEqual(2, switchStatement.Options.Length);
        Assert.AreEqual("MyOptionA", switchStatement.Options[0].Name);
        Assert.AreEqual("MyOptionB", switchStatement.Options[1].Name);
    }

    [TestMethod]
    public void TestSettings()
    {
        string code =
            """
            setting OutputType: Int;
            setting OptionType: String;

            scene main { }
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        StoryNode story = parser.Parse();

        Assert.AreEqual(3, story.TopLevelNodes.Length);

        Assert.IsTrue(story.TopLevelNodes is
        [
            TypeSettingDirectiveNode
            {
                SettingName: nameof(Settings.OutputType),
                Type: IdentifierTypeNode
                {
                    Identifier: "Int",
                }
            },
            TypeSettingDirectiveNode
            {
                SettingName: nameof(Settings.OptionType),
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
        string code =
            """
            setting OutputType Int;
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());

        int errorCount = 0;

        parser.ErrorFound += e =>
        {
            errorCount++;

            Error expectedError = Errors.ExpectedToken(unexpectedToken: new Token
            {
                Kind = TokenKind.Identifier,
                Text = "Int",
                Index = code.IndexOf("Int"),
            }, TokenKind.Colon);

            Assert.AreEqual(expectedError, e);
            Assert.AreEqual(code.IndexOf("Int"), e.Index);
        };

        _ = parser.Parse();

        Assert.AreEqual(1, errorCount);
    }

    [TestMethod]
    public void TestSettingMissingTypeError()
    {
        string code =
            """
            setting OutputType: ;
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());

        int errorCount = 0;

        parser.ErrorFound += e =>
        {
            errorCount++;

            Error expectedError = Errors.UnexpectedToken(new Token
            {
                Kind = TokenKind.Semicolon,
                Text = ";",
                Index = code.IndexOf(";"),
            });

            Assert.AreEqual(expectedError, e);
            Assert.AreEqual(code.IndexOf(";"), e.Index);
        };

        _ = parser.Parse();

        Assert.AreEqual(1, errorCount);
    }

    [TestMethod]
    public void TestParseStringLiteralExpression()
    {
        string code =
            """
            scene main
            {
                output "String";
                output 'Another String';
            }
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        StoryNode story = parser.Parse();

        Assert.IsTrue(story is
        {
            TopLevelNodes:
            [
                SceneSymbolDeclarationNode
                {
                    Name: "main",
                    Body.Statements:
                    [
                        OutputStatementNode
                        {
                            OutputExpression: StringLiteralExpressionNode
                            {
                                StringLiteral: "\"String\"",
                            }
                        },
                        OutputStatementNode
                        {
                            OutputExpression: StringLiteralExpressionNode
                            {
                                StringLiteral: "'Another String'",
                            }
                        }
                    ]
                }
            ]
        });
    }

    [TestMethod]
    public void TestRecordDeclaration()
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
        parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        StoryNode story = parser.Parse();

        Assert.AreEqual(2, story.TopLevelNodes.Length);
        Assert.IsTrue(story.TopLevelNodes[0] is RecordSymbolDeclarationNode
        {
            Name: "Line",
            Properties:
            [
                {
                    Name: "Text",
                    Type: IdentifierTypeNode
                    {
                        Identifier: "String",
                    }
                },
                {
                    Name: "Character",
                    Type: IdentifierTypeNode
                    {
                        Identifier: "Int",
                    }
                },
            ]
        });
    }

    [TestMethod]
    public void TestRecordCreationExpressions()
    {
        string code =
            """
            scene main
            {
                output Line("Hello world!", Character = 4, Metadata(CameraPerspective = "Near", Effect = "None"));
            }
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        StoryNode story = parser.Parse();

        Assert.AreEqual(1, story.TopLevelNodes.Length);

        SceneSymbolDeclarationNode? mainScene = story.TopLevelNodes[0] as SceneSymbolDeclarationNode;
        Assert.IsNotNull(mainScene);
        Assert.AreEqual(1, mainScene.Body.Statements.Length);

        OutputStatementNode? outputStatement = mainScene.Body.Statements[0] as OutputStatementNode;
        Assert.IsNotNull(outputStatement);

        RecordCreationExpressionNode? recordCreation = outputStatement.OutputExpression as RecordCreationExpressionNode;
        Assert.IsNotNull(recordCreation);

        Assert.AreEqual(3, recordCreation.Arguments.Length);

        Assert.IsTrue(recordCreation.Arguments[0] is
        {
            Expression: StringLiteralExpressionNode,
            PropertyName: null,
        });

        Assert.IsTrue(recordCreation.Arguments[1] is
        {
            Expression: IntegerLiteralExpressionNode,
            PropertyName: "Character",
        });

        Assert.IsTrue(recordCreation.Arguments[2] is
        {
            Expression: RecordCreationExpressionNode
            {
                Arguments.Length: 2,
            },
            PropertyName: null,
        });
    }

    [TestMethod]
    public void TestCompleteNonsense()
    {
        string code =
            """
            6 ; = Hello scene output { ("Hey") setting { }
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());

        List<Error> errors = new();
        parser.ErrorFound += errors.Add;

        _ = parser.Parse();

        Assert.IsTrue(errors.Count > 0);
    }

    [TestMethod]
    public void TestBranchOnStatement()
    {
        string code =
            """
            scene main
            {
                branchon MyOutcome
                {
                    option A
                    {
                        output 2;
                    }

                    option B
                    {
                        output 3;
                    }

                    other
                    {
                        output 4;
                    }
                }
            }
            """;

        Parser parser = new(new Lexer(code).Lex());
        parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        StoryNode story = parser.Parse();

        SceneSymbolDeclarationNode mainScene = (SceneSymbolDeclarationNode)story.TopLevelNodes[0];

        BranchOnStatementNode? branchOnStatement = mainScene.Body.Statements[0] as BranchOnStatementNode;
        Assert.IsNotNull(branchOnStatement);

        Assert.AreEqual("MyOutcome", branchOnStatement.OutcomeName);
        Assert.AreEqual(3, branchOnStatement.Options.Length);

        Assert.IsTrue(branchOnStatement.Options[0] is NamedBranchOnOptionNode { OptionName: "A" });
        Assert.IsTrue(branchOnStatement.Options[1] is NamedBranchOnOptionNode { OptionName: "B" });
        Assert.IsTrue(branchOnStatement.Options[2] is OtherBranchOnOptionNode { Body.Statements.Length: 1 });
    }

    [TestMethod]
    public void TestWrongOtherInBranchon()
    {
        string code =
            """
            scene main
            {
                branchon MyOutcome
                {
                    option A
                    {
                        output 2;
                    }

                    option B
                    {
                        output 3;
                    }

                    other
                    {
                        output 4;
                    }

                    option C { }
                }
            }
            """;
        
        Parser parser = new(new Lexer(code).Lex());

        List<Error> errors = new();
        parser.ErrorFound += errors.Add;

        _ = parser.Parse();

        Error expectedError = Errors.BranchOnOnlyOneOtherLast(code.IndexOf("option C"));

        Assert.IsTrue(errors.Contains(expectedError));
    }
}
