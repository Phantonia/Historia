using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;
using Phantonia.Historia.Language.GrammaticalAnalysis.Statements;
using Phantonia.Historia.Language.GrammaticalAnalysis.TopLevel;
using Phantonia.Historia.Language.GrammaticalAnalysis.Types;
using Phantonia.Historia.Language.LexicalAnalysis;
using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Phantonia.Historia.Language.GrammaticalAnalysis;

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
            Index = 0,
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

    private TopLevelNode? ParseTopLevelNode(ref int index)
    {
        switch (tokens[index])
        {
            case { Kind: TokenKind.SceneKeyword }:
                // if parse scene symbol returns null
                // we have an eof too early
                // however this still means we can return null here
                // which means we are done parsing
                return ParseSceneSymbolDeclaration(ref index);
            case { Kind: TokenKind.RecordKeyword }:
                return ParseRecordSymbolDeclaration(ref index);
            case { Kind: TokenKind.UnionKeyword }:
                return ParseUnionSymbolDeclaration(ref index);
            case { Kind: TokenKind.SettingKeyword }:
                return ParseSettingDirective(ref index);
            case { Kind: TokenKind.EndOfFile }:
                return null;
            default:
                {
                    ErrorFound?.Invoke(Errors.UnexpectedToken(tokens[index]));
                    index++;
                    return ParseTopLevelNode(ref index);
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

    private UnionTypeSymbolDeclarationNode? ParseUnionSymbolDeclaration(ref int index)
    {
        Debug.Assert(tokens[index] is { Kind: TokenKind.UnionKeyword });

        int nodeIndex = tokens[index].Index;
        index++;

        string name = Expect(TokenKind.Identifier, ref index).Text;

        _ = Expect(TokenKind.Colon, ref index);

        ImmutableArray<TypeNode>.Builder subtypeBuilder = ImmutableArray.CreateBuilder<TypeNode>();

        while (true)
        {
            TypeNode? subtype = ParseType(ref index);

            if (subtype is null)
            {
                return null;
            }

            subtypeBuilder.Add(subtype);

            if (tokens[index] is { Kind: TokenKind.Comma })
            {
                index++;
            }
            else if (tokens[index] is { Kind: TokenKind.Semicolon })
            {
                index++;
                break;
            }
            else
            {
                ErrorFound?.Invoke(Errors.ExpectedToken(tokens[index], TokenKind.Semicolon));
            }
        }

        return new UnionTypeSymbolDeclarationNode
        {
            Name = name,
            Subtypes = subtypeBuilder.ToImmutable(),
            Index = nodeIndex,
        };
    }

    private RecordSymbolDeclarationNode? ParseRecordSymbolDeclaration(ref int index)
    {
        Debug.Assert(tokens[index] is { Kind: TokenKind.RecordKeyword });

        int nodeIndex = tokens[index].Index;

        index++;

        Token identifierToken = Expect(TokenKind.Identifier, ref index);

        _ = Expect(TokenKind.OpenBrace, ref index);

        ImmutableArray<PropertyDeclarationNode>.Builder propertyDeclarations = ImmutableArray.CreateBuilder<PropertyDeclarationNode>();

        while (tokens[index] is not { Kind: TokenKind.ClosedBrace })
        {
            Token propertyIdentifierToken = Expect(TokenKind.Identifier, ref index);
            _ = Expect(TokenKind.Colon, ref index);

            TypeNode? type = ParseType(ref index);
            if (type is null)
            {
                return null;
            }

            _ = Expect(TokenKind.Semicolon, ref index);

            propertyDeclarations.Add(new PropertyDeclarationNode { Name = propertyIdentifierToken.Text, Type = type, Index = propertyIdentifierToken.Index });
        }

        // this is a closed brace
        index++;

        return new RecordSymbolDeclarationNode
        {
            Name = identifierToken.Text,
            Properties = propertyDeclarations.ToImmutable(),
            Index = nodeIndex,
        };
    }

    private SettingDirectiveNode? ParseSettingDirective(ref int index)
    {
        Debug.Assert(tokens[index].Kind == TokenKind.SettingKeyword);

        int nodeIndex = tokens[index].Index;
        index++;

        Token identifier = Expect(TokenKind.Identifier, ref index);

        _ = Expect(TokenKind.Colon, ref index);

        if (!Settings.AllSettings.Contains(identifier.Text))
        {
            ErrorFound?.Invoke(Errors.SettingDoesNotExist(identifier));
            return null;
        }

        if (Settings.TypeSettings.Contains(identifier.Text))
        {
            TypeNode? type = ParseType(ref index);
            if (type is null)
            {
                return null;
            }

            _ = Expect(TokenKind.Semicolon, ref index);

            return new TypeSettingDirectiveNode
            {
                Type = type,
                SettingName = identifier.Text,
                Index = nodeIndex,
            };
        }
        else if (Settings.ExpressionSettings.Contains(identifier.Text))
        {
            ExpressionNode? expression = ParseExpression(ref index);
            if (expression is null)
            {
                return null;
            }

            _ = Expect(TokenKind.Semicolon, ref index);

            return new ExpressionSettingDirectiveNode
            {
                Expression = expression,
                SettingName = identifier.Text,
                Index = nodeIndex,
            };
        }

        Debug.Assert(false);
        return null;
    }

    // can be reused for top level declarations
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
            case { Kind: TokenKind.BranchOnKeyword }:
                return ParseBranchOnStatement(ref index);
            case { Kind: TokenKind.OutcomeKeyword }:
                {
                    (string name, ImmutableArray<string> options, string? defaultOption, int nodeIndex) = ParseOutcomeDeclaration(ref index);
                    return new OutcomeDeclarationStatementNode
                    {
                        Name = name,
                        Options = options,
                        DefaultOption = defaultOption,
                        Index = nodeIndex,
                    };
                }
            case { Kind: TokenKind.SpectrumKeyword }:
                {
                    (string name, ImmutableArray<SpectrumOptionNode> options, string? defaultOption, int nodeIndex) = ParseSpectrumDeclaration(ref index);
                    return new SpectrumDeclarationStatementNode
                    {
                        Name = name,
                        Options = options,
                        DefaultOption = defaultOption,
                        Index = nodeIndex,
                    };
                }
            case { Kind: TokenKind.Identifier }:
                return ParseIdentifierLeadStatement(ref index);
            case { Kind: TokenKind.EndOfFile }:
                ErrorFound?.Invoke(Errors.UnexpectedEndOfFile(tokens[index]));
                return null;
            default:
                {
                    ErrorFound?.Invoke(Errors.UnexpectedToken(tokens[index]));
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

        string? name = null;

        if (tokens[index].Kind == TokenKind.Identifier)
        {
            name = tokens[index].Text;
            index++;
        }

        _ = Expect(TokenKind.OpenParenthesis, ref index);

        ExpressionNode? expression = ParseExpression(ref index);
        if (expression is null)
        {
            return null;
        }

        _ = Expect(TokenKind.ClosedParenthesis, ref index);

        _ = Expect(TokenKind.OpenBrace, ref index);

        ImmutableArray<SwitchOptionNode>? optionNodes = ParseSwitchOptions(ref index);
        if (optionNodes is null)
        {
            return null;
        }

        _ = Expect(TokenKind.ClosedBrace, ref index);

        return new SwitchStatementNode
        {
            Name = name,
            OutputExpression = expression,
            Options = (ImmutableArray<SwitchOptionNode>)optionNodes,
            Index = nodeIndex,
        };
    }

    private ImmutableArray<SwitchOptionNode>? ParseSwitchOptions(ref int index)
    {
        ImmutableArray<SwitchOptionNode>.Builder optionBuilder = ImmutableArray.CreateBuilder<SwitchOptionNode>();

        while (tokens[index] is { Kind: TokenKind.OptionKeyword })
        {
            int nodeIndex = tokens[index].Index;

            _ = Expect(TokenKind.OptionKeyword, ref index);

            string? name = null;

            if (tokens[index].Kind == TokenKind.Identifier)
            {
                name = tokens[index].Text;
                index++;
            }

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

            SwitchOptionNode optionNode = new()
            {
                Name = name,
                Expression = expression,
                Body = body,
                Index = nodeIndex,
            };

            optionBuilder.Add(optionNode);
        }

        return optionBuilder.ToImmutable();
    }

    private BranchOnStatementNode? ParseBranchOnStatement(ref int index)
    {
        Debug.Assert(tokens[index].Kind == TokenKind.BranchOnKeyword);

        int nodeIndex = tokens[index].Index;
        index++;

        Token outcomeIdentifier = Expect(TokenKind.Identifier, ref index);

        _ = Expect(TokenKind.OpenBrace, ref index);

        ImmutableArray<BranchOnOptionNode>? options = ParseBranchOnOptions(ref index);
        if (options is null)
        {
            return null;
        }

        _ = Expect(TokenKind.ClosedBrace, ref index);

        return new BranchOnStatementNode
        {
            OutcomeName = outcomeIdentifier.Text,
            Options = (ImmutableArray<BranchOnOptionNode>)options,
            Index = nodeIndex,
        };
    }

    private ImmutableArray<BranchOnOptionNode>? ParseBranchOnOptions(ref int index)
    {
        ImmutableArray<BranchOnOptionNode>.Builder optionBuilder = ImmutableArray.CreateBuilder<BranchOnOptionNode>();

        while (tokens[index] is { Kind: TokenKind.OptionKeyword })
        {
            int nodeIndex = tokens[index].Index;

            index++;

            string name = Expect(TokenKind.Identifier, ref index).Text;

            StatementBodyNode? body = ParseStatementBody(ref index);

            if (body is null)
            {
                return null;
            }

            BranchOnOptionNode optionNode = new NamedBranchOnOptionNode()
            {
                OptionName = name,
                Body = body,
                Index = nodeIndex,
            };

            optionBuilder.Add(optionNode);
        }

        if (tokens[index] is { Kind: TokenKind.OtherKeyword })
        {
            int nodeIndex = tokens[index].Index;

            index++;

            StatementBodyNode? body = ParseStatementBody(ref index);

            if (body is null)
            {
                return null;
            }

            BranchOnOptionNode optionNode = new OtherBranchOnOptionNode()
            {
                Body = body,
                Index = nodeIndex,
            };

            optionBuilder.Add(optionNode);
        }

        if (tokens[index] is { Kind: TokenKind.OptionKeyword or TokenKind.OtherKeyword })
        {
            ErrorFound?.Invoke(Errors.BranchOnOnlyOneOtherLast(tokens[index].Index));
        }

        return optionBuilder.ToImmutable();
    }

    private StatementNode? ParseIdentifierLeadStatement(ref int index)
    {
        Debug.Assert(tokens[index] is { Kind: TokenKind.Identifier });
        Token identifier = tokens[index];

        int nodeIndex = tokens[index].Index;
        index++;

        // we might have more statements that begin with an identifier later
        // rewrite this method then
        _ = Expect(TokenKind.Equals, ref index);

        ExpressionNode? assignedExpression = ParseExpression(ref index);
        if (assignedExpression is null)
        {
            return null;
        }

        _ = Expect(TokenKind.Semicolon, ref index);

        return new AssignmentStatementNode
        {
            VariableName = identifier.Text,
            AssignedExpression = assignedExpression,
            Index = nodeIndex,
        };
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
