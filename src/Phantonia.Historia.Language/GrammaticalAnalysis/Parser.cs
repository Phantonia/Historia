using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;
using Phantonia.Historia.Language.GrammaticalAnalysis.Statements;
using Phantonia.Historia.Language.GrammaticalAnalysis.Symbols;
using Phantonia.Historia.Language.GrammaticalAnalysis.Types;
using Phantonia.Historia.Language.LexicalAnalysis;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language.GrammaticalAnalysis;

public sealed class Parser
{
    public Parser(ImmutableArray<Token> tokens, ImmutableArray<Setting>? settings = null)
    {
        this.tokens = tokens;
        this.settings = settings ?? ImmutableArray<Setting>.Empty;
    }

    private readonly ImmutableArray<Token> tokens;
    private readonly ImmutableArray<Setting> settings;

    public event Action<Error>? ErrorFound;

    public StoryNode Parse()
    {
        int index = 0;
        ImmutableArray<SymbolDeclarationNode>.Builder symbolBuilder = ImmutableArray.CreateBuilder<SymbolDeclarationNode>();

        SymbolDeclarationNode? nextSymbol = ParseSymbolDeclaration(ref index);

        while (nextSymbol is not null)
        {
            symbolBuilder.Add(nextSymbol);

            nextSymbol = ParseSymbolDeclaration(ref index);
        }

        return new StoryNode
        {
            Symbols = symbolBuilder.ToImmutable(),
        };
    }

    private Token Expect(TokenKind kind, ref int index)
    {
        if (tokens[index].Kind == kind)
        {
            return tokens[index++];
        }
        else
        {
            ErrorFound?.Invoke(new Error { ErrorMessage = $"Expected a {kind}", Index = tokens[index].Index });
            return new Token { Kind = TokenKind.Empty, Index = tokens[index].Index, Text = "" };
        }
    }

    private SymbolDeclarationNode? ParseSymbolDeclaration(ref int index)
    {
        switch (tokens[index])
        {
            case { Kind: TokenKind.SceneKeyword }:
                // if parse scene symbol returns null
                // we have an eof too early
                // however this still means we can return null here
                // which means we are done parsing
                return ParseSceneSymbolDeclaration(ref index);
            case { Kind: TokenKind.SettingKeyword }:
                return ParseSettingSymbolDeclaration(ref index);
            case { Kind: TokenKind.EndOfFile }:
                return null;
            default:
                {
                    ErrorFound?.Invoke(new Error { ErrorMessage = $"Unexpected token '{tokens[index].Text}'", Index = tokens[index].Index });
                    index++;
                    return ParseSymbolDeclaration(ref index);
                }
        }
    }

    private SceneSymbolDeclarationNode? ParseSceneSymbolDeclaration(ref int index)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.SceneKeyword);
        int nodeIndex = tokens[index].Index;

        index++;

        Token nameToken = Expect(TokenKind.Identifier, ref index);

        StatementBodyNode? body = ParseStatementBody(ref index);
        if (body is null)
        {
            return null;
        }

        return new SceneSymbolDeclarationNode
        {
            Body = body,
            Name = nameToken.Text,
            Index = nodeIndex,
        };
    }

    private SettingSymbolDeclarationNode? ParseSettingSymbolDeclaration(ref int index)
    {
        Debug.Assert(tokens[index].Kind == TokenKind.SettingKeyword);

        int nodeIndex = tokens[index].Index;
        index++;

        Token identifier = Expect(TokenKind.Identifier, ref index);

        _ = Expect(TokenKind.Colon, ref index);

        if (!settings.Any(s => s.Name.ToString() == identifier.Text))
        {
            ErrorFound?.Invoke(new Error { ErrorMessage = $"Setting '{identifier.Text}' does not exist", Index = identifier.Index });
            return null;
        }

        switch (settings.First(s => s.Name.ToString() == identifier.Text))
        {
            case { Kind: SettingKind.TypeArgument, Name: SettingName name }:
                {
                    TypeNode? type = ParseType(ref index);
                    if (type is null)
                    {
                        return null;
                    }

                    _ = Expect(TokenKind.Semicolon, ref index);

                    return new TypeSettingDeclarationNode
                    {
                        Type = type,
                        SettingName = name,
                        Index = nodeIndex,
                    };
                }
            case { Kind: SettingKind.ExpressionArgument, Name: SettingName name }:
                {
                    ExpressionNode? expression = ParseExpression(ref index);
                    if (expression is null)
                    {
                        return null;
                    }

                    _ = Expect(TokenKind.Semicolon, ref index);

                    return new ExpressionSettingDeclarationNode
                    {
                        Expression = expression,
                        SettingName = name,
                        Index = nodeIndex,
                    };
                }
            default:
                Debug.Assert(false);
                return null;
        }
    }

    private StatementBodyNode? ParseStatementBody(ref int index)
    {
        ImmutableArray<StatementNode>.Builder statementBuilder = ImmutableArray.CreateBuilder<StatementNode>();

        int nodeIndex = tokens[index].Index;

        _ = Expect(TokenKind.OpenBrace, ref index);

        while (tokens[index].Kind is not TokenKind.ClosedBrace)
        {
            StatementNode? nextStatement = ParseStatement(ref index);
            if (nextStatement is null)
            {
                return null;
            }

            statementBuilder.Add(nextStatement);
        }

        // now we have the }
        index++;

        return new StatementBodyNode
        {
            Statements = statementBuilder.ToImmutable(),
            Index = nodeIndex,
        };
    }

    private StatementNode? ParseStatement(ref int index)
    {
        switch (tokens[index])
        {
            case { Kind: TokenKind.OutputKeyword }:
                return ParseOutputStatement(ref index);
            case { Kind: TokenKind.SwitchKeyword }:
                return ParseSwitchStatement(ref index);
            case { Kind: TokenKind.EndOfFile }:
                ErrorFound?.Invoke(new Error
                {
                    ErrorMessage = "Unexpected end of file",
                    Index = tokens[index].Index
                });
                return null;
            default:
                {
                    ErrorFound?.Invoke(new Error
                    {
                        ErrorMessage = $"Unexpected token '{tokens[index].Text}'",
                        Index = tokens[index].Index
                    });
                    index++;
                    return ParseStatement(ref index);
                }
        }
    }

    private OutputStatementNode? ParseOutputStatement(ref int index)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.OutputKeyword);

        int nodeIndex = tokens[index].Index;

        index++;

        ExpressionNode? outputExpression = ParseExpression(ref index);
        if (outputExpression is null)
        {
            return null;
        }

        _ = Expect(TokenKind.Semicolon, ref index);

        return new OutputStatementNode
        {
            OutputExpression = outputExpression,
            Index = nodeIndex,
        };
    }

    private SwitchStatementNode? ParseSwitchStatement(ref int index)
    {
        int nodeIndex = tokens[index].Index;

        Debug.Assert(tokens[index] is { Kind: TokenKind.SwitchKeyword });

        index++;

        _ = Expect(TokenKind.OpenParenthesis, ref index);

        ExpressionNode? expression = ParseExpression(ref index);
        if (expression is null)
        {
            return null;
        }

        _ = Expect(TokenKind.ClosedParenthesis, ref index);

        _ = Expect(TokenKind.OpenBrace, ref index);

        ImmutableArray<OptionNode>? optionNodes = ParseOptions(ref index);
        if (optionNodes is null)
        {
            return null;
        }

        _ = Expect(TokenKind.ClosedBrace, ref index);

        return new SwitchStatementNode
        {
            OutputExpression = expression,
            Options = (ImmutableArray<OptionNode>)optionNodes,
            Index = nodeIndex,
        };
    }

    private ImmutableArray<OptionNode>? ParseOptions(ref int index)
    {
        ImmutableArray<OptionNode>.Builder optionBuilder = ImmutableArray.CreateBuilder<OptionNode>();

        while (tokens[index] is { Kind: TokenKind.OptionKeyword })
        {
            int nodeIndex = tokens[index].Index;

            _ = Expect(TokenKind.OptionKeyword, ref index);

            _ = Expect(TokenKind.OpenParenthesis, ref index);

            ExpressionNode? expression = ParseExpression(ref index);

            if (expression is null)
            {
                return null;
            }

            _ = Expect(TokenKind.ClosedParenthesis, ref index);

            StatementBodyNode? body = ParseStatementBody(ref index);

            if (body is null)
            {
                return null;
            }

            OptionNode optionNode = new()
            {
                Index = nodeIndex,
                Body = body,
                Expression = expression,
            };

            optionBuilder.Add(optionNode);
        }

        return optionBuilder.ToImmutable();
    }

    private ExpressionNode? ParseExpression(ref int index)
    {
        switch (tokens[index])
        {
            case { Kind: TokenKind.IntegerLiteral, IntegerValue: int value }:
                return new IntegerLiteralExpressionNode { Value = value, Index = tokens[index++].Index };
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
                ErrorFound?.Invoke(new Error
                {
                    ErrorMessage = "Unexpected end of file",
                    Index = tokens[index].Index
                });
                return null;
            default:
                {
                    ErrorFound?.Invoke(new Error
                    {
                        ErrorMessage = $"Unexpected token '{tokens[index].Text}'",
                        Index = tokens[index].Index
                    });
                    index++;
                    return ParseExpression(ref index);
                }
        }
    }

    private TypeNode? ParseType(ref int index)
    {
        switch (tokens[index])
        {
            case { Kind: TokenKind.Identifier, Text: string identifier }:
                return new IdentifierTypeNode { Identifier = identifier, Index = tokens[index++].Index };
            case { Kind: TokenKind.EndOfFile }:
                ErrorFound?.Invoke(new Error { ErrorMessage = "Unexpected end of file", Index = tokens[index].Index });
                return null;
            default:
                ErrorFound?.Invoke(new Error { ErrorMessage = $"Unexpected token '{tokens[index].Text}'", Index = tokens[index].Index });
                return null;
        }
    }
}
