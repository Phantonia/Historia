using Phantonia.Historia.Language.Ast;
using Phantonia.Historia.Language.Ast.Expressions;
using Phantonia.Historia.Language.Ast.Statements;
using Phantonia.Historia.Language.Ast.Symbols;
using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Phantonia.Historia.Language;

public sealed class Parser
{
    public Parser(ImmutableArray<Token> tokens)
    {
        this.tokens = tokens;
    }

    private readonly ImmutableArray<Token> tokens;

    public event Action<Error>? ErrorFound;

    public StoryNode Parse()
    {
        int index = 0;
        ImmutableArray<SymbolDeclarationNode>.Builder symbolBuilder = ImmutableArray.CreateBuilder<SymbolDeclarationNode>();

        SymbolDeclarationNode? nextSymbol = ParseSymbol(ref index);

        while (nextSymbol is not null)
        {
            symbolBuilder.Add(nextSymbol);

            nextSymbol = ParseSymbol(ref index);
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

    private SymbolDeclarationNode? ParseSymbol(ref int index)
    {
        switch (tokens[index])
        {
            case { Kind: TokenKind.SceneKeyword }: return ParseSceneSymbol(ref index);
            case { Kind: TokenKind.EndOfFile }: return null;
            default:
                {
                    ErrorFound?.Invoke(new Error { ErrorMessage = $"Unexpected token '{tokens[index].Text}'", Index = tokens[index].Index });
                    index++;
                    return ParseSymbol(ref index);
                }
        }
    }

    private SceneSymbolDeclarationNode ParseSceneSymbol(ref int index)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.SceneKeyword);
        int nodeIndex = tokens[index].Index;

        index++;

        Token nameToken = Expect(TokenKind.Identifier, ref index);

        SceneBodyNode body = ParseSceneBody(ref index);

        return new SceneSymbolDeclarationNode
        {
            Body = body,
            Name = nameToken.Text,
            Index = nodeIndex,
        };
    }

    private SceneBodyNode ParseSceneBody(ref int index)
    {
        ImmutableArray<StatementNode>.Builder statementBuilder = ImmutableArray.CreateBuilder<StatementNode>();

        int nodeIndex = tokens[index].Index;

        _ = Expect(TokenKind.OpenBrace, ref index);

        while (tokens[index].Kind is not TokenKind.ClosedBrace)
        {
            StatementNode nextStatement = ParseStatement(ref index);
            statementBuilder.Add(nextStatement);
        }

        // now we have the }
        index++;

        return new SceneBodyNode
        {
            Statements = statementBuilder.ToImmutable(),
            Index = nodeIndex,
        };
    }

    private StatementNode ParseStatement(ref int index)
    {
        switch (tokens[index])
        {
            case { Kind: TokenKind.OutputKeyword }: return ParseOutputStatement(ref index);
            default:
                {
                    ErrorFound?.Invoke(new Error
                    {
                        ErrorMessage = tokens[index].Kind is TokenKind.EndOfFile ? "Unexpected end of file" : $"Unexpected token '{tokens[index].Text}'",
                        Index = tokens[index].Index
                    });
                    index++;
                    return ParseStatement(ref index);
                }
        }
    }
    
    private OutputStatementNode ParseOutputStatement(ref int index)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.OutputKeyword);

        int nodeIndex = tokens[index].Index;

        index++;

        ExpressionNode outputExpression = ParseExpression(ref index);

        _ = Expect(TokenKind.Semicolon, ref index);

        return new OutputStatementNode
        {
            Expression = outputExpression,
            Index = nodeIndex,
        };
    }

    private ExpressionNode ParseExpression(ref int index)
    {
        switch (tokens[index])
        {
            case { Kind: TokenKind.IntegerLiteral, IntegerValue: int value }: return new IntegerLiteralExpressionNode { Value = value, Index = tokens[index++].Index };
            case { Kind: TokenKind.OpenParenthesis }:
                {
                    index++;

                    ExpressionNode expression = ParseExpression(ref index);

                    _ = Expect(TokenKind.ClosedParenthesis, ref index);

                    return expression;
                }
            default:
                {
                    ErrorFound?.Invoke(new Error
                    {
                        ErrorMessage = tokens[index].Kind is TokenKind.EndOfFile ? "Unexpected end of file" : $"Unexpected token '{tokens[index].Text}'",
                        Index = tokens[index].Index
                    });
                    index++;
                    return ParseExpression(ref index);
                }
        }
    }
}
