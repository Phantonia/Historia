﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language;
using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using Phantonia.Historia.Language.SyntaxAnalysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;

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

        List<Error> errors = [];

        parser.ErrorFound += errors.Add;

        _ = parser.Parse();

        Assert.AreEqual(2, errors.Count);

        Error firstError = Errors.UnexpectedToken(new Token
        {
            Kind = TokenKind.Identifier,
            Text = "hello",
            Index = code.IndexOf("hello!"),
            PrecedingTrivia = Environment.NewLine,
        });

        Error secondError = Errors.UnexpectedToken(new Token
        {
            Kind = TokenKind.Unknown,
            Text = "!",
            Index = code.IndexOf('!'),
            PrecedingTrivia = "",
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
                PrecedingTrivia = Environment.NewLine,
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
                PrecedingTrivia = " ",
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
                Index = code.IndexOf(';'),
                PrecedingTrivia = " ",
            });

            Assert.AreEqual(expectedError, e);
            Assert.AreEqual(code.IndexOf(';'), e.Index);
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
                            StringLiteral: "String",
                        }
                    },
                        OutputStatementNode
                    {
                        OutputExpression: StringLiteralExpressionNode
                        {
                            StringLiteral: "Another String",
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
            record Line(Text: String, Character: Int,);

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

        List<Error> errors = [];
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

        List<Error> errors = [];
        parser.ErrorFound += errors.Add;

        _ = parser.Parse();

        Error expectedError = Errors.BranchOnOnlyOneOtherLast(code.IndexOf("option C"));

        Assert.IsTrue(errors.Contains(expectedError));
    }

    [TestMethod]
    public void TestOutcomeDeclaration()
    {
        string code =
            """
            scene main
            {
                outcome W (A);
                outcome X (A, B,);
                outcome Y (); // grammatically correct - semantically incorrect
                outcome Z (A, B, C, D, E, F) default A;
            }
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        StoryNode story = parser.Parse();

        SceneSymbolDeclarationNode mainScene = (SceneSymbolDeclarationNode)story.TopLevelNodes[0];

        static void AssertIsCorrectOutcomeDeclaration(StatementNode statement, string name, string[] options, string? defaultOption)
        {
            OutcomeDeclarationStatementNode? declaration = statement as OutcomeDeclarationStatementNode;

            Assert.IsNotNull(declaration);

            Assert.AreEqual(name, declaration.Name);
            Assert.IsTrue(options.SequenceEqual(declaration.Options));
            Assert.AreEqual(defaultOption, declaration.DefaultOption);
        }

        Assert.AreEqual(4, mainScene.Body.Statements.Length);

        AssertIsCorrectOutcomeDeclaration(mainScene.Body.Statements[0], "W", ["A"], null);
        AssertIsCorrectOutcomeDeclaration(mainScene.Body.Statements[1], "X", ["A", "B"], null);
        AssertIsCorrectOutcomeDeclaration(mainScene.Body.Statements[2], "Y", [], null);
        AssertIsCorrectOutcomeDeclaration(mainScene.Body.Statements[3], "Z", ["A", "B", "C", "D", "E", "F"], "A");
    }

    [TestMethod]
    public void TestAssignment()
    {
        string code =
            """
            scene main
            {
                // this will not pass the binder at all but we don't care
                x = 4;
                y = "abc";
                z = A;
            }
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        StoryNode story = parser.Parse();

        SceneSymbolDeclarationNode mainScene = (SceneSymbolDeclarationNode)story.TopLevelNodes[0];

        static void AssertIsCorrectAssignment<ExpressionType>(StatementNode statement)
            where ExpressionType : ExpressionNode
        {
            AssignmentStatementNode? assignment = statement as AssignmentStatementNode;
            Assert.IsNotNull(assignment);

            Assert.IsInstanceOfType(assignment.AssignedExpression, typeof(ExpressionType));
        }

        AssertIsCorrectAssignment<IntegerLiteralExpressionNode>(mainScene.Body.Statements[0]);
        AssertIsCorrectAssignment<StringLiteralExpressionNode>(mainScene.Body.Statements[1]);
        AssertIsCorrectAssignment<IdentifierExpressionNode>(mainScene.Body.Statements[2]);
    }

    [TestMethod]
    public void TestUnionDeclaration()
    {
        string code =
            """
            union X (Int, String, Y);
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        StoryNode story = parser.Parse();

        Assert.AreEqual(1, story.TopLevelNodes.Length);

        UnionSymbolDeclarationNode? unionDeclaration = story.TopLevelNodes[0] as UnionSymbolDeclarationNode;

        Assert.IsNotNull(unionDeclaration);

        Assert.AreEqual("X", unionDeclaration.Name);
        Assert.IsTrue(unionDeclaration.Subtypes.Select(s => (s as IdentifierTypeNode)?.Identifier).SequenceEqual(["Int", "String", "Y"]));
    }

    [TestMethod]
    public void TestSpectrumDeclarations()
    {
        string code =
            """
            scene main
            {
                spectrum X (A <= 1/2, B);
                spectrum Y (A <= 13/37, B < 17/19, C);
                spectrum Z (A < 1/3, B < 2/3, C) default B;
            }
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        StoryNode story = parser.Parse();

        SceneSymbolDeclarationNode mainScene = (SceneSymbolDeclarationNode)story.TopLevelNodes[0];

        Assert.AreEqual(3, mainScene.Body.Statements.Length);

        List<SpectrumDeclarationStatementNode> spectrumDeclarations = [];

        foreach (StatementNode statement in mainScene.Body.Statements)
        {
            Assert.IsTrue(statement is SpectrumDeclarationStatementNode);

            spectrumDeclarations.Add((SpectrumDeclarationStatementNode)statement);
        }

        // spectrum X
        SpectrumDeclarationStatementNode x = spectrumDeclarations[0];
        Assert.AreEqual("X", x.Name);
        Assert.AreEqual(2, x.Options.Length);
        Assert.IsNull(x.DefaultOption);

        SpectrumOptionNode xa = x.Options[0];
        Assert.AreEqual("A", xa.Name);
        Assert.IsTrue(xa.Inclusive);
        Assert.AreEqual(1, xa.Numerator);
        Assert.AreEqual(2, xa.Denominator);

        SpectrumOptionNode xb = x.Options[1];
        Assert.AreEqual("B", xb.Name);
        Assert.IsTrue(xb.Inclusive);
        Assert.AreEqual(1, xb.Numerator);
        Assert.AreEqual(1, xb.Denominator);

        // spectrum Y
        SpectrumDeclarationStatementNode y = spectrumDeclarations[1];
        Assert.AreEqual("Y", y.Name);
        Assert.AreEqual(3, y.Options.Length);
        Assert.IsNull(y.DefaultOption);

        SpectrumOptionNode ya = y.Options[0];
        Assert.AreEqual("A", ya.Name);
        Assert.IsTrue(ya.Inclusive);
        Assert.AreEqual(13, ya.Numerator);
        Assert.AreEqual(37, ya.Denominator);

        SpectrumOptionNode yb = y.Options[1];
        Assert.AreEqual("B", yb.Name);
        Assert.IsFalse(yb.Inclusive);
        Assert.AreEqual(17, yb.Numerator);
        Assert.AreEqual(19, yb.Denominator);

        SpectrumOptionNode yc = y.Options[2];
        Assert.AreEqual("C", yc.Name);
        Assert.IsTrue(yc.Inclusive);
        Assert.AreEqual(1, yc.Numerator);
        Assert.AreEqual(1, yc.Denominator);

        // spectrum Z
        SpectrumDeclarationStatementNode z = spectrumDeclarations[2];
        Assert.AreEqual("Z", z.Name);
        Assert.AreEqual(3, z.Options.Length);
        Assert.AreEqual("B", z.DefaultOption);

        SpectrumOptionNode za = z.Options[0];
        Assert.AreEqual("A", za.Name);
        Assert.IsFalse(za.Inclusive);
        Assert.AreEqual(1, za.Numerator);
        Assert.AreEqual(3, za.Denominator);

        SpectrumOptionNode zb = z.Options[1];
        Assert.AreEqual("B", zb.Name);
        Assert.IsFalse(zb.Inclusive);
        Assert.AreEqual(2, zb.Numerator);
        Assert.AreEqual(3, zb.Denominator);

        SpectrumOptionNode zc = z.Options[2];
        Assert.AreEqual("C", zc.Name);
        Assert.IsTrue(zc.Inclusive);
        Assert.AreEqual(1, zc.Numerator);
        Assert.AreEqual(1, zc.Denominator);
    }

    [TestMethod]
    public void TestStrengthenAndWeaken()
    {
        string code =
            """
            scene main
            {
                strengthen X by 1;
                weaken Y by (4);
            }
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        StoryNode story = parser.Parse();

        SceneSymbolDeclarationNode mainScene = (SceneSymbolDeclarationNode)story.TopLevelNodes[0];

        Assert.AreEqual(2, mainScene.Body.Statements.Length);

        SpectrumAdjustmentStatementNode strengthenStatement = (SpectrumAdjustmentStatementNode)mainScene.Body.Statements[0];

        Assert.IsTrue(strengthenStatement.Strengthens);
        Assert.IsFalse(strengthenStatement.Weakens);
        Assert.AreEqual("X", strengthenStatement.SpectrumName);
        Assert.IsTrue(strengthenStatement.AdjustmentAmount is IntegerLiteralExpressionNode { Value: 1 });

        SpectrumAdjustmentStatementNode weakenStatement = (SpectrumAdjustmentStatementNode)mainScene.Body.Statements[1];

        Assert.IsTrue(weakenStatement.Weakens);
        Assert.IsFalse(weakenStatement.Strengthens);
        Assert.AreEqual("Y", weakenStatement.SpectrumName);
        Assert.IsTrue(weakenStatement.AdjustmentAmount is IntegerLiteralExpressionNode { Value: 4 });
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
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        StoryNode story = parser.Parse();

        for (int i = 0; i < 3; i++)
        {
            SceneSymbolDeclarationNode? scene = story.TopLevelNodes[i] as SceneSymbolDeclarationNode;
            Assert.IsNotNull(scene);

            Assert.AreEqual(((char)(i + 'A')).ToString(), scene.Name);
            Assert.AreEqual(1, scene.Body.Statements.Length);

            OutputStatementNode? outputStatement = scene.Body.Statements[0] as OutputStatementNode;
            Assert.IsNotNull(outputStatement);
            Assert.IsTrue(outputStatement.OutputExpression is IntegerLiteralExpressionNode { Value: int value } && value == i);
        }
    }

    [TestMethod]
    public void TestCallStatement()
    {
        string code =
            """
            scene main
            {
                call A;
            }
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        StoryNode story = parser.Parse();

        SceneSymbolDeclarationNode? mainScene = story.TopLevelNodes[0] as SceneSymbolDeclarationNode;
        Assert.IsTrue(mainScene is
        {
            Body.Statements: [CallStatementNode { SceneName: "A" }],
        });
    }

    [TestMethod]
    public void TestEnumDeclaration()
    {
        string code =
            """
            enum Character (Alice, Beverly, Charlotte);
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        StoryNode story = parser.Parse();

        Assert.AreEqual(1, story.TopLevelNodes.Length);

        EnumSymbolDeclarationNode? enumDeclaration = story.TopLevelNodes[0] as EnumSymbolDeclarationNode;

        Assert.IsNotNull(enumDeclaration);
        Assert.AreEqual("Character", enumDeclaration.Name);
        Assert.AreEqual(3, enumDeclaration.Options.Length);
        Assert.AreEqual("Alice", enumDeclaration.Options[0]);
        Assert.AreEqual("Beverly", enumDeclaration.Options[1]);
        Assert.AreEqual("Charlotte", enumDeclaration.Options[2]);
        Assert.AreEqual(code.IndexOf("enum"), enumDeclaration.Index);
    }

    [TestMethod]
    public void TestEnumOptionExpression()
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

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        StoryNode story = parser.Parse();

        SceneSymbolDeclarationNode? mainScene = story.TopLevelNodes[^1] as SceneSymbolDeclarationNode;
        Assert.IsNotNull(mainScene);

        string[] optionNames = ["Alice", "Beverly", "Charlotte"];

        for (int i = 0; i < 3; i++)
        {
            OutputStatementNode? outputStatement = mainScene.Body.Statements[i] as OutputStatementNode;
            Assert.IsNotNull(outputStatement);

            EnumOptionExpressionNode? enumExpression = outputStatement.OutputExpression as EnumOptionExpressionNode;
            Assert.IsNotNull(enumExpression);

            Assert.AreEqual("Character", enumExpression.EnumName);
            Assert.AreEqual(optionNames[i], enumExpression.OptionName);
        }
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

                    loop option (2)
                    {
                        output 2;
                    }

                    final option (3)
                    {
                        output 3;
                    }
                }
            }
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        StoryNode story = parser.Parse();

        SceneSymbolDeclarationNode? mainScene = (SceneSymbolDeclarationNode)story.TopLevelNodes[^1];

        Assert.AreEqual(1, mainScene.Body.Statements.Length);

        LoopSwitchStatementNode? loopSwitch = mainScene.Body.Statements[0] as LoopSwitchStatementNode;
        Assert.IsNotNull(loopSwitch);

        Assert.AreEqual(3, loopSwitch.Options.Length);

        Assert.AreEqual(LoopSwitchOptionKind.None, loopSwitch.Options[0].Kind);
        Assert.AreEqual(LoopSwitchOptionKind.Loop, loopSwitch.Options[1].Kind);
        Assert.AreEqual(LoopSwitchOptionKind.Final, loopSwitch.Options[2].Kind);
    }

    [TestMethod]
    public void TestValidCheckpoints()
    {
        string code =
            """
            scene main
            {
                checkpoint output 0;
            
                checkpoint switch (1)
                {
                    option (1) { }
                    option (1) { }
                }
            
                checkpoint loop switch (2)
                {
                    option (2) { }
                    final option (2) { }
                }

                output 3;
            
                switch (4)
                {
                    option (4) { }
                    option (4) { }
                }
            
                loop switch (5)
                {
                    option (5) { }
                    final option (5) { }
                }
            }
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        StoryNode story = parser.Parse();
        SceneSymbolDeclarationNode mainScene = (SceneSymbolDeclarationNode)story.TopLevelNodes[0];

        Assert.AreEqual(6, mainScene.Body.Statements.Length);

        Assert.IsTrue(mainScene.Body.Statements[0] is OutputStatementNode { IsCheckpoint: true });
        Assert.IsTrue(mainScene.Body.Statements[1] is SwitchStatementNode { IsCheckpoint: true });
        Assert.IsTrue(mainScene.Body.Statements[2] is LoopSwitchStatementNode { IsCheckpoint: true });
        Assert.IsTrue(mainScene.Body.Statements[3] is OutputStatementNode { IsCheckpoint: false });
        Assert.IsTrue(mainScene.Body.Statements[4] is SwitchStatementNode { IsCheckpoint: false });
        Assert.IsTrue(mainScene.Body.Statements[5] is LoopSwitchStatementNode { IsCheckpoint: false });
    }

    [TestMethod]
    public void TestInvalidCheckpoints()
    {
        string code =
            """
            scene main
            {
                checkpoint outcome X(A, B); // error: can't have outcome after checkpoint
            }
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());

        List<Error> errors = [];
        parser.ErrorFound += errors.Add;

        _ = parser.Parse();

        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual(Errors.ExpectedVisibleStatementAsCheckpoint(new Token
        {
            Index = code.IndexOf("outcome"),
            Kind = TokenKind.OutcomeKeyword,
            Text = "outcome",
            PrecedingTrivia = " ",
        }), errors[0]);
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

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        StoryNode story = parser.Parse();

        Assert.AreEqual(5, story.TopLevelNodes.Length);

        InterfaceSymbolDeclarationNode? intface = story.TopLevelNodes[2] as InterfaceSymbolDeclarationNode;
        Assert.IsNotNull(intface);

        Assert.AreEqual("ICharacter", intface.Name);
        Assert.AreEqual(2, intface.Methods.Length);

        Assert.AreEqual(InterfaceMethodKind.Action, intface.Methods[0].Kind);
        Assert.AreEqual("Say", intface.Methods[0].Name);
        Assert.AreEqual(1, intface.Methods[0].Parameters.Length);
        Assert.AreEqual("line", intface.Methods[0].Parameters[0].Name);

        Assert.AreEqual(InterfaceMethodKind.Choice, intface.Methods[1].Kind);
        Assert.AreEqual("Choose", intface.Methods[1].Name);
        Assert.AreEqual(1, intface.Methods[1].Parameters.Length);
        Assert.AreEqual("prompt", intface.Methods[1].Parameters[0].Name);

        ReferenceSymbolDeclarationNode? reference = story.TopLevelNodes[3] as ReferenceSymbolDeclarationNode;
        Assert.IsNotNull(reference);

        Assert.AreEqual("Character", reference.Name);
        Assert.AreEqual("ICharacter", reference.InterfaceName);

        SceneSymbolDeclarationNode? mainScene = story.TopLevelNodes[4] as SceneSymbolDeclarationNode;
        Assert.IsNotNull(mainScene);

        Assert.AreEqual(2, mainScene.Body.Statements.Length);

        RunStatementNode? runStatement = mainScene.Body.Statements[0] as RunStatementNode;
        Assert.IsNotNull(runStatement);
        Assert.AreEqual("Character", runStatement.ReferenceName);
        Assert.AreEqual("Say", runStatement.MethodName);
        Assert.AreEqual(1, runStatement.Arguments.Length);

        ChooseStatementNode? chooseStatement = mainScene.Body.Statements[1] as ChooseStatementNode;
        Assert.IsNotNull(chooseStatement);
        Assert.AreEqual("Character", chooseStatement.ReferenceName);
        Assert.AreEqual("Choose", chooseStatement.MethodName);
        Assert.AreEqual(1, chooseStatement.Arguments.Length);
        Assert.AreEqual(2, chooseStatement.Options.Length);
    }

    [TestMethod]
    public void TestSwitchAndChooseWithoutOption()
    {
        string code =
            """
            interface I(choice C(x: Int));
            reference R: I;

            scene main
            {
                switch (0) { } // switch
                choose R.C(1) { } // choose
            }
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());

        List<Error> errors = [];
        parser.ErrorFound += errors.Add;

        _ = parser.Parse();

        Error firstError = Errors.MustHaveAtLeastOneOption(code.IndexOf("} // switch"));
        Error secondError = Errors.MustHaveAtLeastOneOption(code.IndexOf("} // choose"));

        Assert.AreEqual(2, errors.Count);
        Assert.AreEqual(firstError, errors[0]);
        Assert.AreEqual(secondError, errors[1]);
    }

    [TestMethod]
    public void TestInterfaceWithParameterlessMethod()
    {
        string code =
            """
            interface I
            (
                action X(),
                choice Y(),
            );

            reference R: I;

            scene main
            {
                run R.X();

                choose R.Y()
                {
                    option (0) { }
                }
            }
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        // we just assert that this goes through without errors or exceptions
        _ = parser.Parse();
    }

    [TestMethod]
    public void TestConditions()
    {
        string code =
            """
            // since there is no valid language construct that accepts conditions
            // we just cheat and parse something that the binder will spit out

            setting Namespace: A is B and C is D or E is F and G is H and (I is J or K is L);
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        StoryNode story = parser.Parse();

        ExpressionNode expression = ((ExpressionSettingDirectiveNode)story.TopLevelNodes[0]).Expression;

        OrExpressionNode? abcdefghijkl = expression as OrExpressionNode;
        Assert.IsNotNull(abcdefghijkl);

        AndExpressionNode? abcd = abcdefghijkl.LeftExpression as AndExpressionNode;
        Assert.IsNotNull(abcd);

        IsExpressionNode? ab = abcd.LeftExpression as IsExpressionNode;
        Assert.IsNotNull(ab);
        Assert.AreEqual("A", ab.OutcomeName);
        Assert.AreEqual("B", ab.OptionName);

        IsExpressionNode? cd = abcd.LeftExpression as IsExpressionNode;
        Assert.IsNotNull(cd);

        AndExpressionNode? efghijkl = abcdefghijkl.RightExpression as AndExpressionNode;
        Assert.IsNotNull(efghijkl);

        IsExpressionNode? ef = efghijkl.LeftExpression as IsExpressionNode;
        Assert.IsNotNull(ef);

        AndExpressionNode? ghijkl = efghijkl.RightExpression as AndExpressionNode;
        Assert.IsNotNull(ghijkl);

        IsExpressionNode? gh = ghijkl.LeftExpression as IsExpressionNode;
        Assert.IsNotNull(gh);

        OrExpressionNode? ijkl = ghijkl.RightExpression as OrExpressionNode;
        Assert.IsNotNull(ijkl);

        IsExpressionNode? ij = ijkl.LeftExpression as IsExpressionNode;
        Assert.IsNotNull(ij);

        IsExpressionNode? kl = ijkl.RightExpression as IsExpressionNode;
        Assert.IsNotNull(kl);
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

                if X is A
                {
                    output 0;
                }
                else if X is B
                {
                    output 1;
                }
                else
                {
                    output 2;
                }
            }
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail(Errors.GenerateFullMessage(code, e));

        StoryNode story = parser.Parse();

        SceneSymbolDeclarationNode mainScene = (SceneSymbolDeclarationNode)story.TopLevelNodes[1];

        Assert.AreEqual(2, mainScene.Body.Statements.Length);

        IfStatementNode? ifStatement = mainScene.Body.Statements[1] as IfStatementNode;
        Assert.IsNotNull(ifStatement);

        IsExpressionNode? isExpression = ifStatement.Condition as IsExpressionNode;
        Assert.IsNotNull(isExpression);
        Assert.AreEqual("X", isExpression.OutcomeName);
        Assert.AreEqual("A", isExpression.OptionName);

        OutputStatementNode? output0Statement = ifStatement.ThenBlock.Statements[0] as OutputStatementNode;
        Assert.IsNotNull(output0Statement);

        IfStatementNode? elseIfStatement = ifStatement.ElseBlock?.Statements[0] as IfStatementNode;
        Assert.IsNotNull(elseIfStatement);
    }
}
