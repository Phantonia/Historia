using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.Refactorings;

public sealed class LineRefactoring : IRefactoring
{
    public StoryNode Refactor(StoryNode original)
    {
        return original with
        {
            CompilationUnits = original.CompilationUnits.Select(RefactorCompilationUnit).ToImmutableArray(),
        };
    }

    private CompilationUnitNode RefactorCompilationUnit(CompilationUnitNode unit)
    {
        return unit with
        {
            TopLevelNodes =
                unit.TopLevelNodes
                    .Select(n => n is BoundSymbolDeclarationNode { Original: SubroutineSymbolDeclarationNode subroutine } ? RefactorSubroutine(subroutine) : n)
                    .ToImmutableArray(),
        };
    }

    private SubroutineSymbolDeclarationNode RefactorSubroutine(SubroutineSymbolDeclarationNode subroutine)
    {
        return subroutine with
        {
            Body = RefactorBody(subroutine.Body),
        };
    }

    private StatementBodyNode RefactorBody(StatementBodyNode body)
    {
        return body with
        {
            Statements = body.Statements.Select(RefactorStatement).ToImmutableArray(),
        };
    }

    private StatementNode RefactorStatement(StatementNode statement)
    {
        switch (statement)
        {
            case OutputStatementNode
            {
                OutputExpression: TypedExpressionNode{
                    Original: BoundRecordCreationExpressionNode
                    {
                        Record:
                        {
                            IsLineRecord: true
                        } lineRecord,
                    } recordCreationExpression,
                },
            } outputStatement:
                return RefactorLineOutputStatement(lineRecord, recordCreationExpression, outputStatement);
            case SwitchStatementNode switchStatement:
                return switchStatement with
                {
                    Options = switchStatement.Options.Select(o => o with { Body = RefactorBody(o.Body) }).ToImmutableArray(),
                };
            case BoundBranchOnStatementNode branchonStatement:
                return branchonStatement with
                {
                    Options = branchonStatement.Options.Select(o => o with { Body = RefactorBody(o.Body) }).ToImmutableArray(),
                };
            case IfStatementNode ifStatement:
                return ifStatement with
                {
                    ThenBlock = RefactorBody(ifStatement.ThenBlock),
                    ElseBlock = ifStatement.ElseBlock is not null ? RefactorBody(ifStatement.ElseBlock) : null,
                };
            case BoundChooseStatementNode chooseStatement:
                return chooseStatement with
                {
                    Options = chooseStatement.Options.Select(o => o with { Body = RefactorBody(o.Body) }).ToImmutableArray(),
                };
            default:
                return statement;
        }
    }

    private StatementNode RefactorLineOutputStatement(RecordTypeSymbol lineRecord, BoundRecordCreationExpressionNode recordCreationExpression, OutputStatementNode outputStatement)
    {
        string trivia = outputStatement.OutputKeywordToken.PrecedingTrivia;

        ExpressionNode characterExpression;

        if (recordCreationExpression.Original.Arguments[0].Expression is EnumOptionExpressionNode { OptionName: string characterName })
        {
            characterExpression = new IdentifierExpressionNode
            {
                IdentifierToken = new Token
                {
                    Kind = TokenKind.Identifier,
                    PrecedingTrivia = trivia,
                    Text = characterName,
                    Index = recordCreationExpression.Original.Arguments[0].Index,
                },
                Index = recordCreationExpression.Original.Arguments[0].Index,
                PrecedingTokens = [],
            };
        }
        else
        {
            characterExpression = SetTrivia(recordCreationExpression.Original.Arguments[0].Expression, trivia);
        }

        ImmutableArray<BoundArgumentNode> additionalArguments = PrepareAdditionalArguments(recordCreationExpression.BoundArguments);

        ExpressionNode textExpression = recordCreationExpression.Original.Arguments[^1].Expression;

        if (additionalArguments.Length > 0)
        {
            BoundLineStatementNode boundLineStatement = new()
            {
                LineCreationExpression = new TypedExpressionNode
                {
                    Original = recordCreationExpression,
                    Index = recordCreationExpression.Index,
                    SourceType = lineRecord,
                    PrecedingTokens = [],
                },
                LineStatement = new LineStatementNode
                {
                    CharacterExpression = characterExpression,
                    OpenSquareBracketToken = new Token
                    {
                        Kind = TokenKind.OpenSquareBracket,
                        Text = "[",
                        PrecedingTrivia = " ",
                        Index = characterExpression.Index + 1,
                    },
                    AdditionalArguments = additionalArguments.Cast<ArgumentNode>().ToImmutableArray(),
                    ClosedSquareBracketToken = new Token
                    {
                        Kind = TokenKind.ClosedSquareBracket,
                        Text = "]",
                        PrecedingTrivia = "",
                        Index = characterExpression.Index + 2,
                    },
                    ColonToken = new Token
                    {
                        Kind = TokenKind.Colon,
                        Text = ":",
                        PrecedingTrivia = "",
                        Index = characterExpression.Index + 4,
                    },
                    TextExpression = textExpression,
                    SemicolonToken = outputStatement.SemicolonToken,
                    Index = characterExpression.Index,
                    PrecedingTokens = [],
                },
                Index = characterExpression.Index,
                PrecedingTokens = [],
            };

            return boundLineStatement;
        }
        else
        {
            BoundLineStatementNode boundLineStatement = new()
            {
                LineCreationExpression = new TypedExpressionNode
                {
                    Original = recordCreationExpression,
                    Index = recordCreationExpression.Index,
                    SourceType = lineRecord,
                    PrecedingTokens = [],
                },
                LineStatement = new LineStatementNode
                {
                    CharacterExpression = characterExpression,
                    OpenSquareBracketToken = null,
                    AdditionalArguments = null,
                    ClosedSquareBracketToken = null,
                    ColonToken = new Token
                    {
                        Kind = TokenKind.Colon,
                        Text = ":",
                        PrecedingTrivia = "",
                        Index = characterExpression.Index + 4,
                    },
                    TextExpression = textExpression,
                    SemicolonToken = outputStatement.SemicolonToken,
                    Index = characterExpression.Index,
                    PrecedingTokens = [],
                },
                Index = characterExpression.Index,
                PrecedingTokens = [],
            };

            return boundLineStatement;
        }
    }

    private ImmutableArray<BoundArgumentNode> PrepareAdditionalArguments(ImmutableArray<BoundArgumentNode> arguments)
    {
        if (arguments.Length <= 2)
        {
            return [];
        }

        if (arguments.Length == 3)
        {
            return [arguments[1] with {
                CommaToken = null,
                Expression = RemoveTrivia(arguments[1].Expression),
            }];
        }

        return arguments.Skip(2)
                        .Take(arguments.Length - 3)
                        .Append(arguments[^2] with
                        {
                            CommaToken = null,
                        })
                        .Prepend(arguments[1] with
                        {
                            Expression = RemoveTrivia(arguments[1].Expression),
                        })
                        .ToImmutableArray();
    }

    private static ExpressionNode SetTrivia(ExpressionNode expression, string trivia)
    {
        switch (expression)
        {
            case EnumOptionExpressionNode enumOptionExpression:
                return enumOptionExpression with
                {
                    EnumNameToken = enumOptionExpression.EnumNameToken with
                    {
                        PrecedingTrivia = trivia,
                    },
                };
            case IdentifierExpressionNode identifierExpression:
                return identifierExpression with
                {
                    IdentifierToken = identifierExpression.IdentifierToken with
                    {
                        PrecedingTrivia = trivia,
                    },
                };
            case IntegerLiteralExpressionNode integerLiteralExpression:
                return integerLiteralExpression with
                {
                    LiteralToken = integerLiteralExpression.LiteralToken with
                    {
                        PrecedingTrivia = trivia,
                    },
                };
            case IsExpressionNode isExpression:
                return isExpression with
                {
                    OutcomeNameToken = isExpression.OutcomeNameToken with
                    {
                        PrecedingTrivia = trivia,
                    },
                };
            case LogicExpressionNode logicExpression:
                return logicExpression with
                {
                    LeftExpression = SetTrivia(logicExpression.LeftExpression, trivia),
                };
            case NotExpressionNode notExpression:
                return notExpression with
                {
                    NotKeywordToken = notExpression.NotKeywordToken with
                    {
                        PrecedingTrivia = trivia,
                    },
                };
            case ParenthesizedExpressionNode parenthesizedExpression:
                return parenthesizedExpression with
                {
                    OpenParenthesisToken = parenthesizedExpression.OpenParenthesisToken with
                    {
                        PrecedingTrivia = trivia,
                    },
                };
            case RecordCreationExpressionNode recordCreationExpression:
                return recordCreationExpression with
                {
                    RecordNameToken = recordCreationExpression.RecordNameToken with
                    {
                        PrecedingTrivia = trivia,
                    },
                };
            case StringLiteralExpressionNode stringLiteralExpression:
                return stringLiteralExpression with
                {
                    LiteralToken = stringLiteralExpression.LiteralToken with
                    {
                        PrecedingTrivia = trivia,
                    },
                };
            case TypedExpressionNode typedExpression:
                return typedExpression with
                {
                    Original = SetTrivia(typedExpression.Original, trivia),
                };
            default:
                throw new NotImplementedException();
        }
    }

    private static ExpressionNode RemoveTrivia(ExpressionNode expression)
    {
        return SetTrivia(expression, "");    
    }
}
