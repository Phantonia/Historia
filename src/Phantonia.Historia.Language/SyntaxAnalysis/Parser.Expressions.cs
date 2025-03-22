using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Phantonia.Historia.Language.SyntaxAnalysis;

public sealed partial class Parser
{
    private ExpressionNode ParseExpression(ref int index, ImmutableList<Token> precedingTokens)
    {
        long nodeIndex = tokens[index].Index;

        ExpressionNode leftHandSide = ParseConjunctiveExpression(ref index, precedingTokens);

        if (tokens[index].Kind is not TokenKind.OrKeyword)
        {
            return leftHandSide;
        }

        Token operatorToken = tokens[index];

        index++;
        ExpressionNode rightHandSide = ParseExpression(ref index, []);

        // this way all expressions are right associative
        // no problem as both AND and OR are associative
        // for a clean tree we might still prefer left associative expressions
        // if so we could rewire the tree here

        return new LogicExpressionNode
        {
            LeftExpression = leftHandSide,
            OperatorToken = operatorToken,
            RightExpression = rightHandSide,
            Index = nodeIndex,
            PrecedingTokens = precedingTokens,
        };
    }

    private ExpressionNode ParseConjunctiveExpression(ref int index, ImmutableList<Token> precedingTokens)
    {
        long nodeIndex = tokens[index].Index;

        ExpressionNode leftHandSide = ParseSimpleExpression(ref index, precedingTokens);

        if (tokens[index].Kind is not TokenKind.AndKeyword)
        {
            return leftHandSide;
        }

        Token operatorToken = tokens[index];

        index++;
        ExpressionNode rightHandSide = ParseConjunctiveExpression(ref index, []);

        // see comment about associativity in ParseExpression method

        return new LogicExpressionNode
        {
            LeftExpression = leftHandSide,
            OperatorToken = operatorToken,
            RightExpression = rightHandSide,
            Index = nodeIndex,
            PrecedingTokens = precedingTokens,
        };
    }

    private ExpressionNode ParseSimpleExpression(ref int index, ImmutableList<Token> precedingTokens)
    {
        switch (tokens[index].Kind)
        {
            case TokenKind.IntegerLiteral:
                return new IntegerLiteralExpressionNode
                {
                    LiteralToken = tokens[index],
                    Index = tokens[index++].Index,
                    PrecedingTokens = precedingTokens,
                };
            case TokenKind.StringLiteral:
                return new StringLiteralExpressionNode
                {
                    LiteralToken = tokens[index],
                    Index = tokens[index++].Index,
                    PrecedingTokens = precedingTokens,
                };
            case TokenKind.TrueKeyword or TokenKind.FalseKeyword:
                return new BooleanLiteralExpressionNode
                {
                    TrueOrFalseKeywordToken = tokens[index],
                    Index = tokens[index++].Index,
                    PrecedingTokens = precedingTokens,
                };
            case TokenKind.Identifier:
                return ParseIdentifierExpression(ref index, precedingTokens);
            case TokenKind.OpenParenthesis:
                {
                    Token openParenthesis = tokens[index];
                    index++;

                    ExpressionNode? expression = ParseExpression(ref index, []);

                    Token closedParenthesis = Expect(TokenKind.ClosedParenthesis, ref index);

                    return new ParenthesizedExpressionNode
                    {
                        OpenParenthesisToken = openParenthesis,
                        InnerExpression = expression,
                        ClosedParenthesisToken = closedParenthesis,
                        Index = openParenthesis.Index,
                        PrecedingTokens = precedingTokens,
                    };
                }
            case TokenKind.NotKeyword:
                return ParseNotExpression(ref index, precedingTokens);
            case TokenKind.Minus:
                return ParseNegationExpression(ref index, precedingTokens);
            case TokenKind.EndOfFile:
                ErrorFound?.Invoke(Errors.UnexpectedEndOfFile(tokens[index]));
                return new MissingExpressionNode
                {
                    Index = tokens[index].Index,
                    PrecedingTokens = precedingTokens,
                };
            default:
                {
                    Token unexpectedToken = tokens[index];
                    ErrorFound?.Invoke(Errors.UnexpectedToken(unexpectedToken));
                    index++;
                    return ParseExpression(ref index, precedingTokens.Add(unexpectedToken));
                }
        }
    }

    private ExpressionNode ParseIdentifierExpression(ref int index, ImmutableList<Token> precedingTokens)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.Identifier);

        Token name = tokens[index];
        long nodeIndex = tokens[index].Index;
        index++;

        switch (tokens[index].Kind)
        {
            case TokenKind.Dot:
                {
                    Token dot = tokens[index];

                    index++;

                    Token optionName = Expect(TokenKind.Identifier, ref index);

                    return new EnumOptionExpressionNode
                    {
                        EnumNameToken = name,
                        DotToken = dot,
                        OptionNameToken = optionName,
                        Index = nodeIndex,
                        PrecedingTokens = precedingTokens,
                    };
                }
            case TokenKind.OpenParenthesis:
                (Token openParenthesis, ImmutableArray<ArgumentNode> arguments, Token closedParenthesis) = ParseArgumentList(ref index, TokenKind.OpenParenthesis, TokenKind.ClosedParenthesis, []);

                return new RecordCreationExpressionNode
                {
                    RecordNameToken = name,
                    OpenParenthesisToken = openParenthesis,
                    Arguments = arguments,
                    ClosedParenthesisToken = closedParenthesis,
                    Index = nodeIndex,
                    PrecedingTokens = precedingTokens,
                };
            case TokenKind.IsKeyword:
                {
                    Token isKeyword = tokens[index];

                    index++;
                    Token optionName = Expect(TokenKind.Identifier, ref index);

                    return new IsExpressionNode
                    {
                        OutcomeNameToken = name,
                        IsKeywordToken = isKeyword,
                        OptionNameToken = optionName,
                        Index = nodeIndex,
                        PrecedingTokens = precedingTokens,
                    };
                }
            default:
                return new IdentifierExpressionNode
                {
                    IdentifierToken = name,
                    Index = nodeIndex,
                    PrecedingTokens = precedingTokens,
                };
        }
    }

    private (Token openBracket, ImmutableArray<ArgumentNode>, Token closedBracket) ParseArgumentList(ref int index, TokenKind openBracketKind, TokenKind closedBracketKind, ImmutableList<Token> precedingTokens)
    {
        Token openBracket = Expect(openBracketKind, ref index);

        ImmutableArray<ArgumentNode>.Builder arguments = ImmutableArray.CreateBuilder<ArgumentNode>();

        while (tokens[index].Kind != closedBracketKind)
        {
            long argumentIndex = tokens[index].Index;

            if (index < tokens.Length - 1 && tokens[index].Kind == TokenKind.Identifier && tokens[index + 1].Kind == TokenKind.Equals)
            {
                // named argument
                Token argumentName = tokens[index];
                Token equals = tokens[index + 1];
                index += 2;

                ExpressionNode expression = ParseExpression(ref index, []);

                Token? comma = null;

                if (tokens[index].Kind is TokenKind.Comma)
                {
                    comma = tokens[index];
                }

                arguments.Add(new ArgumentNode
                {
                    ParameterNameToken = argumentName,
                    EqualsToken = equals,
                    Expression = expression,
                    CommaToken = comma,
                    Index = argumentIndex,
                    PrecedingTokens = precedingTokens,
                });
            }
            else
            {
                ExpressionNode expression = ParseExpression(ref index, []);

                Token? comma = null;

                if (tokens[index].Kind is TokenKind.Comma)
                {
                    comma = tokens[index];
                }

                arguments.Add(new ArgumentNode
                {
                    ParameterNameToken = null,
                    EqualsToken = null,
                    Expression = expression,
                    CommaToken = comma,
                    Index = expression.Index,
                    PrecedingTokens = precedingTokens,
                });
            }

            if (tokens[index].Kind is not TokenKind.Comma)
            {
                break;
            }

            index++;
        }

        // in case of no trailing comma, just expect a closed bracket
        // else this is redundant and just does index++
        Token closedBracket = Expect(closedBracketKind, ref index);

        return (openBracket, arguments.ToImmutable(), closedBracket);
    }

    private NotExpressionNode ParseNotExpression(ref int index, ImmutableList<Token> precedingTokens)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.NotKeyword);
        Token notKeyword = tokens[index];
        long nodeIndex = notKeyword.Index;
        index++;

        ExpressionNode innerExpression = ParseSimpleExpression(ref index, []);

        return new NotExpressionNode
        {
            NotKeywordToken = notKeyword,
            InnerExpression = innerExpression,
            Index = nodeIndex,
            PrecedingTokens = precedingTokens,
        };
    }

    private IntegerNegationExpressionNode ParseNegationExpression(ref int index, ImmutableList<Token> precedingTokens)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.Minus);
        Token minus = tokens[index];
        long nodeIndex = minus.Index;
        index++;

        ExpressionNode innerExpression = ParseSimpleExpression(ref index, []);

        return new IntegerNegationExpressionNode
        {
            MinusToken = minus,
            InnerExpression = innerExpression,
            Index = nodeIndex,
            PrecedingTokens = precedingTokens,
        };
    }
}
