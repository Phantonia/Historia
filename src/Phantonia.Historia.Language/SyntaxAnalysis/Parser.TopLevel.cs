using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using Phantonia.Historia.Language.SyntaxAnalysis.Types;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Phantonia.Historia.Language.SyntaxAnalysis;

public sealed partial class Parser
{
    private TopLevelNode ParseTopLevelNode(ref int index, ImmutableList<Token> precedingTokens)
    {
        switch (tokens[index].Kind)
        {
            case TokenKind.SceneKeyword or TokenKind.ChapterKeyword:
                return ParseSceneSymbolDeclaration(ref index, precedingTokens);
            case TokenKind.RecordKeyword:
                return ParseRecordSymbolDeclaration(ref index, precedingTokens);
            case TokenKind.UnionKeyword:
                return ParseUnionSymbolDeclaration(ref index, precedingTokens);
            case TokenKind.EnumKeyword:
                return ParseEnumSymbolDeclaration(ref index, precedingTokens);
            case TokenKind.SettingKeyword:
                return ParseSettingDirective(ref index, precedingTokens);
            case TokenKind.PublicKeyword:
                return ParsePublicTopLevelNode(ref index, precedingTokens);
            case TokenKind.OutcomeKeyword:
                {
                    OutcomeDeclarationInfo outcomeDeclaration = ParseOutcomeDeclaration(ref index);

                    return new OutcomeSymbolDeclarationNode
                    {
                        PublicKeywordToken = null,
                        OutcomeKeywordToken = outcomeDeclaration.OutcomeKeyword,
                        NameToken = outcomeDeclaration.Name,
                        OpenParenthesisToken = outcomeDeclaration.OpenParenthesis,
                        OptionNameTokens = outcomeDeclaration.Options,
                        CommaTokens = outcomeDeclaration.Commas,
                        ClosedParenthesisToken = outcomeDeclaration.ClosedParenthesis,
                        DefaultKeywordToken = outcomeDeclaration.DefaultKeyword,
                        DefaultOptionToken = outcomeDeclaration.DefaultOption,
                        SemicolonToken = outcomeDeclaration.Semicolon,
                        Index = outcomeDeclaration.Index,
                        PrecedingTokens = precedingTokens,
                    };
                }
            case TokenKind.SpectrumKeyword:
                {
                    SpectrumDeclarationInfo spectrumDeclaration = ParseSpectrumDeclaration(ref index);

                    return new SpectrumSymbolDeclarationNode
                    {
                        PublicKeywordToken = null,
                        SpectrumKeywordToken = spectrumDeclaration.SpectrumKeyword,
                        NameToken = spectrumDeclaration.Name,
                        OpenParenthesisToken = spectrumDeclaration.OpenParenthesis,
                        Options = spectrumDeclaration.Options,
                        ClosedParenthesisToken = spectrumDeclaration.ClosedParenthesis,
                        DefaultKeywordToken = spectrumDeclaration.DefaultKeyword,
                        DefaultOptionToken = spectrumDeclaration.DefaultOption,
                        SemicolonToken = spectrumDeclaration.Semicolon,
                        Index = spectrumDeclaration.Index,
                        PrecedingTokens = precedingTokens,
                    };
                }
            case TokenKind.InterfaceKeyword:
                return ParseInterfaceDeclaration(ref index, precedingTokens);
            case TokenKind.ReferenceKeyword:
                return ParseReferenceDeclaration(ref index, precedingTokens);
            case TokenKind.EndOfFile:
                return new MissingTopLevelNode
                {
                    Index = tokens[index++].Index,
                    PrecedingTokens = precedingTokens,
                };
            default:
                {
                    Token unexpectedToken = tokens[index];
                    ErrorFound?.Invoke(Errors.UnexpectedToken(unexpectedToken));
                    index++;
                    return ParseTopLevelNode(ref index, precedingTokens.Add(unexpectedToken));
                }
        }
    }

    private SceneSymbolDeclarationNode ParseSceneSymbolDeclaration(ref int index, ImmutableList<Token> precedingTokens)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.SceneKeyword or TokenKind.ChapterKeyword);
        Token sceneKeyword = tokens[index];
        long nodeIndex = tokens[index].Index;

        index++;

        Token name = Expect(TokenKind.Identifier, ref index);

        StatementBodyNode body = ParseStatementBody(ref index, []);

        return new SceneSymbolDeclarationNode
        {
            SceneOrChapterKeywordToken = sceneKeyword,
            NameToken = name,
            Body = body,
            Index = nodeIndex,
            PrecedingTokens = precedingTokens,
        };
    }

    private UnionSymbolDeclarationNode ParseUnionSymbolDeclaration(ref int index, ImmutableList<Token> precedingTokens)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.UnionKeyword);
        Token unionKeyword = tokens[index];
        long nodeIndex = unionKeyword.Index;
        index++;

        Token name = Expect(TokenKind.Identifier, ref index);

        Token openParenthesis = Expect(TokenKind.OpenParenthesis, ref index);

        ImmutableArray<TypeNode>.Builder subtypeBuilder = ImmutableArray.CreateBuilder<TypeNode>();
        ImmutableArray<Token>.Builder commaBuilder = ImmutableArray.CreateBuilder<Token>();

        while (tokens[index].Kind is not TokenKind.ClosedParenthesis)
        {
            TypeNode subtype = ParseType(ref index, []);

            subtypeBuilder.Add(subtype);

            if (tokens[index].Kind is not TokenKind.Comma)
            {
                break;
            }

            commaBuilder.Add(tokens[index]);

            index++;
        }

        // in case of no trailing comma, just expect a closed parenthesis
        // else this is redundant and just does index++
        Token closedParenthesis = Expect(TokenKind.ClosedParenthesis, ref index);
        Token semicolon = Expect(TokenKind.Semicolon, ref index);

        return new UnionSymbolDeclarationNode
        {
            UnionKeywordToken = unionKeyword,
            NameToken = name,
            OpenParenthesisToken = openParenthesis,
            Subtypes = subtypeBuilder.ToImmutable(),
            CommaTokens = commaBuilder.ToImmutable(),
            ClosedParenthesisToken = closedParenthesis,
            SemicolonToken = semicolon,
            Index = nodeIndex,
            PrecedingTokens = precedingTokens,
        };
    }

    private RecordSymbolDeclarationNode ParseRecordSymbolDeclaration(ref int index, ImmutableList<Token> precedingTokens)
    {
        // spec 1.3.1.1:
        /*
            RecordDeclaration : 'record' identifier '(' (PropertyDeclaration (',' PropertyDeclaration)* ','?)? ')' ';';
            PropertyDeclaration: identifier ':' Type;
         */

        Debug.Assert(tokens[index].Kind is TokenKind.RecordKeyword);
        Token recordKeyword = tokens[index];
        long nodeIndex = recordKeyword.Index;

        index++;

        Token name = Expect(TokenKind.Identifier, ref index);

        (Token openParenthesis, ImmutableArray<ParameterDeclarationNode> propertyDeclarations, Token closedParenthesis) = ParseParameterList(ref index, []);

        Token semicolon = Expect(TokenKind.Semicolon, ref index);

        return new RecordSymbolDeclarationNode
        {
            RecordKeywordToken = recordKeyword,
            NameToken = name,
            OpenParenthesisToken = openParenthesis,
            Properties = propertyDeclarations,
            ClosedParenthesisToken = closedParenthesis,
            SemicolonToken = semicolon,
            Index = nodeIndex,
            PrecedingTokens = precedingTokens,
        };
    }

    private (Token openParenthesis, ImmutableArray<ParameterDeclarationNode> parameter, Token closedParenthesis) ParseParameterList(ref int index, ImmutableList<Token> precedingTokens)
    {
        Token openParenthesis = Expect(TokenKind.OpenParenthesis, ref index);

        ImmutableArray<ParameterDeclarationNode>.Builder propertyDeclarations = ImmutableArray.CreateBuilder<ParameterDeclarationNode>();

        while (tokens[index].Kind is not TokenKind.ClosedParenthesis)
        {
            Token parameterName = Expect(TokenKind.Identifier, ref index);
            Token colon = Expect(TokenKind.Colon, ref index);

            TypeNode type = ParseType(ref index, []);

            if (tokens[index].Kind is not TokenKind.Comma)
            {
                propertyDeclarations.Add(new ParameterDeclarationNode
                {
                    NameToken = parameterName,
                    ColonToken = colon,
                    Type = type,
                    CommaToken = null,
                    Index = parameterName.Index,
                    PrecedingTokens = precedingTokens,
                });

                break;
            }

            propertyDeclarations.Add(new ParameterDeclarationNode
            {
                NameToken = parameterName,
                ColonToken = colon,
                Type = type,
                CommaToken = tokens[index],
                Index = parameterName.Index,
                PrecedingTokens = precedingTokens,
            });

            index++;
        }

        // in case of no trailing comma, just expect a closed parenthesis
        // else this is redundant and just does index++
        Token closedParenthesis = Expect(TokenKind.ClosedParenthesis, ref index);

        return (openParenthesis, propertyDeclarations.ToImmutable(), closedParenthesis);
    }

    private EnumSymbolDeclarationNode ParseEnumSymbolDeclaration(ref int index, ImmutableList<Token> precedingTokens)
    {
        // spec 1.3.1.2:
        // EnumDeclaration : 'enum' identifier '(' (identifier (',' identifier)* ','?)? ')' ';';
        Debug.Assert(tokens[index].Kind is TokenKind.EnumKeyword);
        Token enumKeyword = tokens[index];
        long nodeIndex = enumKeyword.Index;
        index++;

        Token name = Expect(TokenKind.Identifier, ref index);

        Token openParenthesis = Expect(TokenKind.OpenParenthesis, ref index);

        ImmutableArray<Token>.Builder optionBuilder = ImmutableArray.CreateBuilder<Token>();
        ImmutableArray<Token>.Builder commaBuilder = ImmutableArray.CreateBuilder<Token>();

        while (tokens[index].Kind is TokenKind.Identifier)
        {
            optionBuilder.Add(tokens[index]);

            index++;

            if (tokens[index].Kind is not TokenKind.Comma)
            {
                break;
            }
            else
            {
                commaBuilder.Add(tokens[index]);
                index++;
            }
        }

        Token closedParenthesis = Expect(TokenKind.ClosedParenthesis, ref index);
        Token semicolon = Expect(TokenKind.Semicolon, ref index);

        return new EnumSymbolDeclarationNode
        {
            EnumKeywordToken = enumKeyword,
            NameToken = name,
            OpenParenthesisToken = openParenthesis,
            OptionTokens = optionBuilder.ToImmutable(),
            CommaTokens = commaBuilder.ToImmutable(),
            ClosedParenthesisToken = closedParenthesis,
            SemicolonToken = semicolon,
            Index = nodeIndex,
            PrecedingTokens = precedingTokens,
        };
    }

    private SettingDirectiveNode ParseSettingDirective(ref int index, ImmutableList<Token> precedingTokens)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.SettingKeyword);
        Token settingKeyword = tokens[index];
        long nodeIndex = settingKeyword.Index;
        index++;

        Token identifier = Expect(TokenKind.Identifier, ref index);
        Token colon = Expect(TokenKind.Colon, ref index);

        if (!Settings.AllSettings.Contains(identifier.Text))
        {
            ErrorFound?.Invoke(Errors.SettingDoesNotExist(identifier));

            // just pretend this is an expression setting
            return ParseExpressionSetting(ref index);
        }

        if (Settings.TypeSettings.Contains(identifier.Text))
        {
            TypeNode type = ParseType(ref index, []);

            Token semicolon = Expect(TokenKind.Semicolon, ref index);

            return new TypeSettingDirectiveNode
            {
                SettingKeywordToken = settingKeyword,
                SettingNameToken = identifier,
                ColonToken = colon,
                Type = type,
                SemicolonToken = semicolon,
                Index = nodeIndex,
                PrecedingTokens = precedingTokens,
            };
        }
        else if (Settings.ExpressionSettings.Contains(identifier.Text))
        {
            return ParseExpressionSetting(ref index);
        }

        Debug.Assert(false);
        return null;

        SettingDirectiveNode ParseExpressionSetting(ref int index)
        {
            ExpressionNode expression = ParseExpression(ref index, []);

            Token semicolon = Expect(TokenKind.Semicolon, ref index);

            return new ExpressionSettingDirectiveNode
            {
                SettingKeywordToken = settingKeyword,
                SettingNameToken = identifier,
                ColonToken = colon,
                Expression = expression,
                SemicolonToken = semicolon,
                Index = nodeIndex,
                PrecedingTokens = precedingTokens,
            };
        }
    }

    private TopLevelNode ParsePublicTopLevelNode(ref int index, ImmutableList<Token> precedingTokens)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.PublicKeyword);
        Token publicKeyword = tokens[index];
        long nodeIndex = tokens[index].Index;
        index++;

        switch (tokens[index].Kind)
        {
            case TokenKind.SpectrumKeyword:
                {
                    SpectrumDeclarationInfo spectrumDeclaration = ParseSpectrumDeclaration(ref index);

                    return new SpectrumSymbolDeclarationNode
                    {
                        PublicKeywordToken = publicKeyword,
                        SpectrumKeywordToken = spectrumDeclaration.SpectrumKeyword,
                        NameToken = spectrumDeclaration.Name,
                        OpenParenthesisToken = spectrumDeclaration.OpenParenthesis,
                        Options = spectrumDeclaration.Options,
                        ClosedParenthesisToken = spectrumDeclaration.ClosedParenthesis,
                        DefaultKeywordToken = spectrumDeclaration.DefaultKeyword,
                        DefaultOptionToken = spectrumDeclaration.DefaultOption,
                        SemicolonToken = spectrumDeclaration.Semicolon,
                        Index = spectrumDeclaration.Index,
                        PrecedingTokens = precedingTokens,
                    };
                }
            case TokenKind.OutcomeKeyword:
                {
                    OutcomeDeclarationInfo outcomeDeclaration = ParseOutcomeDeclaration(ref index);

                    return new OutcomeSymbolDeclarationNode
                    {
                        PublicKeywordToken = publicKeyword,
                        OutcomeKeywordToken = outcomeDeclaration.OutcomeKeyword,
                        NameToken = outcomeDeclaration.Name,
                        OpenParenthesisToken = outcomeDeclaration.OpenParenthesis,
                        OptionNameTokens = outcomeDeclaration.Options,
                        CommaTokens = outcomeDeclaration.Commas,
                        ClosedParenthesisToken = outcomeDeclaration.ClosedParenthesis,
                        DefaultKeywordToken = outcomeDeclaration.DefaultKeyword,
                        DefaultOptionToken = outcomeDeclaration.DefaultOption,
                        SemicolonToken = outcomeDeclaration.Semicolon,
                        Index = nodeIndex,
                        PrecedingTokens = precedingTokens,
                    };
                }
            default:
                ErrorFound?.Invoke(Errors.UnexpectedToken(tokens[index - 1]));
                return ParseTopLevelNode(ref index, precedingTokens.Add(tokens[index - 1]));
        }
    }

    private InterfaceSymbolDeclarationNode ParseInterfaceDeclaration(ref int index, ImmutableList<Token> precedingTokens)
    {
        Debug.Assert(tokens[index].Kind == TokenKind.InterfaceKeyword);
        Token interfaceKeyword = tokens[index];
        long nodeIndex = interfaceKeyword.Index;
        index++;

        Token name = Expect(TokenKind.Identifier, ref index);

        Token openParenthesis = Expect(TokenKind.OpenParenthesis, ref index);

        ImmutableArray<InterfaceMethodDeclarationNode>.Builder methods = ImmutableArray.CreateBuilder<InterfaceMethodDeclarationNode>();

        while (tokens[index].Kind is not TokenKind.ClosedParenthesis)
        {
            InterfaceMethodDeclarationNode nextMethod = ParseInterfaceMethodDeclaration(ref index, []);

            methods.Add(nextMethod);

            if (nextMethod.CommaToken is null)
            {
                break;
            }
        }

        Token closedParenthesis = Expect(TokenKind.ClosedParenthesis, ref index);
        Token semicolon = Expect(TokenKind.Semicolon, ref index);

        return new InterfaceSymbolDeclarationNode
        {
            InterfaceKeywordToken = interfaceKeyword,
            NameToken = name,
            OpenParenthesisToken = openParenthesis,
            Methods = methods.ToImmutable(),
            ClosedParenthesisToken = closedParenthesis,
            SemicolonToken = semicolon,
            Index = nodeIndex,
            PrecedingTokens = precedingTokens,
        };
    }

    private InterfaceMethodDeclarationNode ParseInterfaceMethodDeclaration(ref int index, ImmutableList<Token> precedingTokens)
    {
        long nodeIndex = tokens[index].Index;

        Token kind = ExpectOneOf(ref index, TokenKind.ActionKeyword, TokenKind.ChoiceKeyword);
        Token name = Expect(TokenKind.Identifier, ref index);
        (Token openParenthesis, ImmutableArray<ParameterDeclarationNode> parameterList, Token closedParenthesis) = ParseParameterList(ref index, []);

        Token? comma = null;

        if (tokens[index].Kind is TokenKind.Comma)
        {
            comma = tokens[index];
            index++;
        }

        return new InterfaceMethodDeclarationNode
        {
            KindToken = kind,
            NameToken = name,
            OpenParenthesisToken = openParenthesis,
            Parameters = parameterList,
            ClosedParenthesisToken = closedParenthesis,
            CommaToken = comma,
            Index = nodeIndex,
            PrecedingTokens = precedingTokens,
        };
    }

    private ReferenceSymbolDeclarationNode ParseReferenceDeclaration(ref int index, ImmutableList<Token> precedingTokens)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.ReferenceKeyword);
        Token referenceKeyword = tokens[index];
        long nodeIndex = referenceKeyword.Index;
        index++;

        Token name = Expect(TokenKind.Identifier, ref index);
        Token colon = Expect(TokenKind.Colon, ref index);
        Token interfaceName = Expect(TokenKind.Identifier, ref index);
        Token semicolon = Expect(TokenKind.Semicolon, ref index);

        return new ReferenceSymbolDeclarationNode
        {
            ReferenceKeywordToken = referenceKeyword,
            NameToken = name,
            ColonToken = colon,
            InterfaceNameToken = interfaceName,
            SemicolonToken = semicolon,
            Index = nodeIndex,
            PrecedingTokens = precedingTokens,
        };
    }
}
