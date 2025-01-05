using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Phantonia.Historia.Language.SyntaxAnalysis;

public sealed partial class Parser
{
    private ExpressionNode ParseExpression(ref int index)
    {
        long nodeIndex = tokens[index].Index;

        ExpressionNode leftHandSide = ParseConjunctiveExpression(ref index);

        if (tokens[index].Kind is not TokenKind.OrKeyword)
        {
            return leftHandSide;
        }

        Token operatorToken = tokens[index];

        index++;
        ExpressionNode rightHandSide = ParseExpression(ref index);

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
        };
    }

    private ExpressionNode ParseConjunctiveExpression(ref int index)
    {
        long nodeIndex = tokens[index].Index;

        ExpressionNode leftHandSide = ParseSimpleExpression(ref index);

        if (tokens[index].Kind is not TokenKind.AndKeyword)
        {
            return leftHandSide;
        }

        Token operatorToken = tokens[index];

        index++;
        ExpressionNode rightHandSide = ParseConjunctiveExpression(ref index);

        // see comment about associativity in ParseExpression method

        return new LogicExpressionNode
        {
            LeftExpression = leftHandSide,
            OperatorToken = operatorToken,
            RightExpression = rightHandSide,
            Index = nodeIndex,
        };
    }

    private ExpressionNode ParseSimpleExpression(ref int index)
    {
        switch (tokens[index].Kind)
        {
            case TokenKind.IntegerLiteral:
                return new IntegerLiteralExpressionNode
                {
                    LiteralToken = tokens[index],
                    Index = tokens[index++].Index,
                };
            case TokenKind.StringLiteral:
                return new StringLiteralExpressionNode
                {
                    LiteralToken = tokens[index],
                    Index = tokens[index++].Index,
                };
            case TokenKind.Identifier:
                return ParseIdentifierExpression(ref index);
            case TokenKind.OpenParenthesis:
                {
                    Token openParenthesis = tokens[index];
                    index++;

                    ExpressionNode? expression = ParseExpression(ref index);
                    
                    Token closedParenthesis = Expect(TokenKind.ClosedParenthesis, ref index);

                    return new ParenthesizedExpressionNode
                    {
                        OpenParenthesisToken = openParenthesis,
                        InnerExpression = expression,
                        ClosedParenthesisToken = closedParenthesis,
                        Index = openParenthesis.Index,
                    };
                }
            case TokenKind.NotKeyword:
                return ParseNotExpression(ref index);
            case TokenKind.EndOfFile:
                ErrorFound?.Invoke(Errors.UnexpectedEndOfFile(tokens[index]));
                return new MissingExpressionNode
                {
                    Index = tokens[index].Index,
                };
            default:
                {
                    ErrorFound?.Invoke(Errors.UnexpectedToken(tokens[index]));
                    index++;
                    return ParseExpression(ref index);
                }
        }
    }

    private ExpressionNode ParseIdentifierExpression(ref int index)
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
                    };
                }
            case TokenKind.OpenParenthesis:
                (Token openParenthesis, ImmutableArray<ArgumentNode> arguments, Token closedParenthesis) = ParseArgumentList(ref index);

                return new RecordCreationExpressionNode
                {
                    RecordNameToken = name,
                    OpenParenthesisToken = openParenthesis,
                    Arguments = arguments,
                    ClosedParenthesisToken = closedParenthesis,
                    Index = nodeIndex,
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
                    };
                }
            default:
                return new IdentifierExpressionNode
                {
                    IdentifierToken = name,
                    Index = nodeIndex,
                };
        }
    }

    private (Token openParenthesis, ImmutableArray<ArgumentNode>, Token closedParenthesis) ParseArgumentList(ref int index)
    {
        Token openParenthesis = Expect(TokenKind.OpenParenthesis, ref index);

        ImmutableArray<ArgumentNode>.Builder arguments = ImmutableArray.CreateBuilder<ArgumentNode>();

        while (tokens[index].Kind is not TokenKind.ClosedParenthesis)
        {
            long argumentIndex = tokens[index].Index;

            if (index < tokens.Length - 1 && tokens[index].Kind == TokenKind.Identifier && tokens[index + 1].Kind == TokenKind.Equals)
            {
                // named argument
                Token argumentName = tokens[index];
                Token equals = tokens[index + 1];
                index += 2;

                ExpressionNode expression = ParseExpression(ref index);

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
                });
            }
            else
            {
                ExpressionNode expression = ParseExpression(ref index);

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
                });
            }

            if (tokens[index].Kind is not TokenKind.Comma)
            {
                break;
            }

            index++;
        }

        // in case of no trailing comma, just expect a closed parenthesis
        // else this is redundant and just does index++
        Token closedParenthesis = Expect(TokenKind.ClosedParenthesis, ref index);

        return (openParenthesis, arguments.ToImmutable(), closedParenthesis);
    }

    private NotExpressionNode ParseNotExpression(ref int index)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.NotKeyword);
        Token notKeyword = tokens[index];
        long nodeIndex = notKeyword.Index;
        index++;

        ExpressionNode innerExpression = ParseSimpleExpression(ref index);

        return new NotExpressionNode
        {
            NotKeywordToken = notKeyword,
            InnerExpression = innerExpression,
            Index = nodeIndex,
        };
    }
}
