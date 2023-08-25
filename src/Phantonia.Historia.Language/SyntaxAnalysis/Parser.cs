using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using Phantonia.Historia.Language.SyntaxAnalysis.Types;
using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Phantonia.Historia.Language.SyntaxAnalysis;

public sealed partial class Parser
{
    public Parser(ImmutableArray<Token> tokens)
    {
        this.tokens = tokens;
    }

    private readonly ImmutableArray<Token> tokens;

    public event Action<Error>? ErrorFound;

    public StoryNode Parse()
    {
        ScanForBrokenTokens();

        int index = 0;
        ImmutableArray<TopLevelNode>.Builder symbolBuilder = ImmutableArray.CreateBuilder<TopLevelNode>();

        TopLevelNode? nextSymbol = ParseTopLevelNode(ref index);

        while (nextSymbol is not null)
        {
            symbolBuilder.Add(nextSymbol);

            nextSymbol = ParseTopLevelNode(ref index);
        }

        return new StoryNode
        {
            TopLevelNodes = symbolBuilder.ToImmutable(),
            Index = tokens.Length > 0 ? tokens[0].Index : 0,
        };
    }

    private void ScanForBrokenTokens()
    {
        foreach (Token token in tokens)
        {
            switch (token.Kind)
            {
                case TokenKind.BrokenStringLiteral:
                    ErrorFound?.Invoke(Errors.BrokenStringLiteral(token));
                    continue;
                default:
                    if (token.Kind == TokenKind.Empty || !Enum.IsDefined(token.Kind))
                    {
                        throw new UnreachableException();
                    }
                    continue;
            }
        }
    }

    private Token Expect(TokenKind kind, ref int index)
    {
        if (tokens[index].Kind == kind)
        {
            return tokens[index++];
        }
        else
        {
            ErrorFound?.Invoke(Errors.ExpectedToken(tokens[index], kind));
            return new Token { Kind = TokenKind.Empty, Index = tokens[index].Index, Text = "" };
        }
    }

    private (string name, ImmutableArray<string> options, string? defaultOption, int nodeIndex) ParseOutcomeDeclaration(ref int index)
    {
        Debug.Assert(tokens[index] is { Kind: TokenKind.OutcomeKeyword });

        int nodeIndex = tokens[index].Index;
        index++;

        string name = Expect(TokenKind.Identifier, ref index).Text;

        _ = Expect(TokenKind.OpenParenthesis, ref index);

        ImmutableArray<string>.Builder optionsBuilder = ImmutableArray.CreateBuilder<string>();

        while (tokens[index] is { Kind: TokenKind.Identifier, Text: string option })
        {
            optionsBuilder.Add(option);

            index++;

            if (tokens[index] is not { Kind: TokenKind.Comma })
            {
                break;
            }
            else
            {
                index++;
            }
        }

        _ = Expect(TokenKind.ClosedParenthesis, ref index);

        string? defaultOption = null;

        if (tokens[index] is { Kind: TokenKind.DefaultKeyword })
        {
            index++;

            defaultOption = Expect(TokenKind.Identifier, ref index).Text;
        }

        _ = Expect(TokenKind.Semicolon, ref index);

        return (name, optionsBuilder.ToImmutable(), defaultOption, nodeIndex);
    }

    private (string name, ImmutableArray<SpectrumOptionNode> options, string? defaultOption, int nodeIndex) ParseSpectrumDeclaration(ref int index)
    {
        Debug.Assert(tokens[index] is { Kind: TokenKind.SpectrumKeyword });

        int nodeIndex = tokens[index].Index;
        index++;

        string name = Expect(TokenKind.Identifier, ref index).Text;

        _ = Expect(TokenKind.OpenParenthesis, ref index);

        ImmutableArray<SpectrumOptionNode>.Builder optionBuilder = ImmutableArray.CreateBuilder<SpectrumOptionNode>();

        while (tokens[index] is { Kind: TokenKind.Identifier })
        {
            string optionName = tokens[index].Text;
            int optionIndex = tokens[index].Index;
            index++;

            if (tokens[index] is not { Kind: TokenKind.LessThan or TokenKind.LessThanOrEquals })
            {
                optionBuilder.Add(new SpectrumOptionNode
                {
                    Name = optionName,
                    Numerator = 1,
                    Denominator = 1,
                    Inclusive = true,
                    Index = optionIndex,
                });

                break;
            }

            bool inclusive = tokens[index] is { Kind: TokenKind.LessThanOrEquals };
            index++;

            int? numerator = Expect(TokenKind.IntegerLiteral, ref index).IntegerValue;
            _ = Expect(TokenKind.Slash, ref index);
            int? denominator = Expect(TokenKind.IntegerLiteral, ref index).IntegerValue;

            _ = Expect(TokenKind.Comma, ref index);

            if (numerator is null || denominator is null)
            {
                continue;
            }

            optionBuilder.Add(new SpectrumOptionNode
            {
                Name = optionName,
                Inclusive = inclusive,
                Numerator = (int)numerator,
                Denominator = (int)denominator,
                Index = optionIndex,
            });
        }

        _ = Expect(TokenKind.ClosedParenthesis, ref index);

        string? defaultOption = null;

        if (tokens[index] is { Kind: TokenKind.DefaultKeyword })
        {
            index++;
            defaultOption = Expect(TokenKind.Identifier, ref index).Text;
        }

        _ = Expect(TokenKind.Semicolon, ref index);

        return (name, optionBuilder.ToImmutable(), defaultOption, nodeIndex);
    }

    private ExpressionNode? ParseExpression(ref int index)
    {
        switch (tokens[index])
        {
            case { Kind: TokenKind.IntegerLiteral, IntegerValue: int value }:
                return new IntegerLiteralExpressionNode
                {
                    Value = value,
                    Index = tokens[index++].Index,
                };
            case { Kind: TokenKind.StringLiteral, Text: string literal }:
                return new StringLiteralExpressionNode
                {
                    StringLiteral = literal,
                    Index = tokens[index++].Index,
                };
            case { Kind: TokenKind.Identifier, Text: string identifier }:
                return ParseIdentifierExpression(ref index);
            case { Kind: TokenKind.OpenParenthesis }:
                {
                    index++;

                    ExpressionNode? expression = ParseExpression(ref index);
                    if (expression is null)
                    {
                        return null;
                    }

                    _ = Expect(TokenKind.ClosedParenthesis, ref index);

                    return expression;
                }
            case { Kind: TokenKind.EndOfFile }:
                ErrorFound?.Invoke(Errors.UnexpectedEndOfFile(tokens[index]));
                return null;
            default:
                {
                    ErrorFound?.Invoke(Errors.UnexpectedToken(tokens[index]));
                    index++;
                    return ParseExpression(ref index);
                }
        }
    }

    private ExpressionNode? ParseIdentifierExpression(ref int index)
    {
        Debug.Assert(tokens[index] is { Kind: TokenKind.Identifier });

        string name = tokens[index].Text;
        int nodeIndex = tokens[index].Index;
        index++;

        if (tokens[index] is not { Kind: TokenKind.OpenParenthesis })
        {
            if (tokens[index] is { Kind: TokenKind.Dot })
            {
                string enumName = name;

                index++;
                string optionName = Expect(TokenKind.Identifier, ref index).Text;

                return new EnumOptionExpressionNode
                {
                    EnumName = enumName,
                    OptionName = optionName,
                    Index = nodeIndex,
                };
            }

            return new IdentifierExpressionNode
            {
                Identifier = name,
                Index = nodeIndex,
            };
        }

        index++;

        ImmutableArray<ArgumentNode>.Builder arguments = ImmutableArray.CreateBuilder<ArgumentNode>();

        while (index < tokens.Length)
        {
            int argumentIndex = tokens[index].Index;

            if (index < tokens.Length - 1 && tokens[index].Kind == TokenKind.Identifier && tokens[index + 1].Kind == TokenKind.Equals)
            {
                // named argument
                string argumentName = tokens[index].Text;
                index += 2;

                ExpressionNode? expression = ParseExpression(ref index);
                if (expression is null)
                {
                    return null;
                }

                arguments.Add(new ArgumentNode
                {
                    Expression = expression,
                    PropertyName = argumentName,
                    Index = argumentIndex,
                });
            }
            else if (tokens[index].Kind != TokenKind.ClosedParenthesis)
            {
                ExpressionNode? expression = ParseExpression(ref index);
                if (expression is null)
                {
                    return null;
                }

                arguments.Add(new ArgumentNode
                {
                    Expression = expression,
                    Index = expression.Index,
                });
            }

            if (tokens[index].Kind == TokenKind.ClosedParenthesis)
            {
                index++;
                break;
            }

            _ = Expect(TokenKind.Comma, ref index);
        }

        return new RecordCreationExpressionNode
        {
            Arguments = arguments.ToImmutable(),
            RecordName = name,
            Index = nodeIndex,
        };
    }

    private TypeNode? ParseType(ref int index)
    {
        switch (tokens[index])
        {
            case { Kind: TokenKind.Identifier, Text: string identifier }:
                return new IdentifierTypeNode { Identifier = identifier, Index = tokens[index++].Index };
            case { Kind: TokenKind.EndOfFile }:
                ErrorFound?.Invoke(Errors.UnexpectedToken(tokens[index]));
                return null;
            default:
                ErrorFound?.Invoke(Errors.UnexpectedToken(tokens[index]));
                return null;
        }
    }
}
