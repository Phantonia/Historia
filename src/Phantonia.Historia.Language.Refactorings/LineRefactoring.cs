using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.Refactorings;

public sealed class LineRefactoring : IRefactoring
{
    public StoryNode Refactor(StoryNode original)
    {
        throw new NotImplementedException();
    }

    private StatementNode RefactorStatement(StatementNode statement)
    {
        if (statement is not OutputStatementNode
            {
                OutputExpression: BoundRecordCreationExpressionNode
                {
                    Record:
                    {
                        IsLineRecord: true
                    } lineRecord
                } recordCreationExpression,
            } outputStatement)
        {
            return statement;
        }

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
            characterExpression = recordCreationExpression.Original.Arguments[0].Expression;
        }

        ImmutableArray<BoundArgumentNode> additionalArguments =
            recordCreationExpression.BoundArguments
                                    .Skip(1)
                                    .Take(recordCreationExpression.BoundArguments.Length - 2)
                                    .ToImmutableArray();

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
                    AdditionalArguments = additionalArguments,

                    ColonToken = new Token
                    {
                        Kind = TokenKind.Colon,
                        Text = ":",
                        PrecedingTrivia = "",
                        Index = characterExpression.Index + 4,
                    },
                }
            };
        }

        
    }

    private ExpressionNode SetTrivia(ExpressionNode expression) => throw new NotImplementedException();
}
