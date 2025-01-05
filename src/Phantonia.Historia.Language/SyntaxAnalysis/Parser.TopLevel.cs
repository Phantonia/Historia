using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using Phantonia.Historia.Language.SyntaxAnalysis.Types;
using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Phantonia.Historia.Language.SyntaxAnalysis;

public sealed partial class Parser
{
    private TopLevelNode ParseTopLevelNode(ref int index)
    {
        switch (tokens[index].Kind)
        {
            case TokenKind.SceneKeyword:
                return ParseSceneSymbolDeclaration(ref index);
            case TokenKind.RecordKeyword:
                return ParseRecordSymbolDeclaration(ref index);
            case TokenKind.UnionKeyword:
                return ParseUnionSymbolDeclaration(ref index);
            case TokenKind.EnumKeyword:
                return ParseEnumSymbolDeclaration(ref index);
            case TokenKind.SettingKeyword:
                return ParseSettingDirective(ref index);
            case TokenKind.PublicKeyword:
                return ParsePublicTopLevelNode(ref index);
            case TokenKind.OutcomeKeyword:
                {
                    (string name, ImmutableArray<string> options, string? defaultOption, long nodeIndex) = ParseOutcomeDeclaration(ref index);

                    return new OutcomeSymbolDeclarationNode
                    {
                        Name = name,
                        Options = options,
                        DefaultOption = defaultOption,
                        IsPublic = false,
                        Index = nodeIndex,
                    };
                }
            case TokenKind.SpectrumKeyword:
                {
                    (string name, ImmutableArray<SpectrumOptionNode> options, string? defaultOption, long nodeIndex) = ParseSpectrumDeclaration(ref index);

                    return new SpectrumSymbolDeclarationNode
                    {
                        Name = name,
                        Options = options,
                        DefaultOption = defaultOption,
                        IsPublic = false,
                        Index = nodeIndex,
                    };
                }
            case TokenKind.InterfaceKeyword:
                return ParseInterfaceDeclaration(ref index);
            case TokenKind.ReferenceKeyword:
                return ParseReferenceDeclaration(ref index);
            case TokenKind.EndOfFile:
                Debug.Assert(false);
                return null;
            default:
                {
                    ErrorFound?.Invoke(Errors.UnexpectedToken(tokens[index]));
                    index++;
                    return ParseTopLevelNode(ref index);
                }
        }
    }

    private SceneSymbolDeclarationNode ParseSceneSymbolDeclaration(ref int index)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.SceneKeyword);
        Token sceneKeyword = tokens[index];
        long nodeIndex = tokens[index].Index;

        index++;

        Token name = Expect(TokenKind.Identifier, ref index);

        StatementBodyNode body = ParseStatementBody(ref index);
        
        return new SceneSymbolDeclarationNode
        {
            SceneKeywordToken = sceneKeyword,
            NameToken = name,
            Body = body,
            Index = nodeIndex,
        };
    }

    private UnionSymbolDeclarationNode ParseUnionSymbolDeclaration(ref int index)
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
            TypeNode subtype = ParseType(ref index);

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
        };
    }

    private RecordSymbolDeclarationNode? ParseRecordSymbolDeclaration(ref int index)
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

        (Token openParenthesis, ImmutableArray<ParameterDeclarationNode> propertyDeclarations, Token closedParenthesis) = ParseParameterList(ref index);

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
        };
    }

    private (Token openParenthesis, ImmutableArray<ParameterDeclarationNode> parameter, Token closedParenthesis) ParseParameterList(ref int index)
    {
        Token openParenthesis = Expect(TokenKind.OpenParenthesis, ref index);

        ImmutableArray<ParameterDeclarationNode>.Builder propertyDeclarations = ImmutableArray.CreateBuilder<ParameterDeclarationNode>();

        while (tokens[index].Kind is not TokenKind.ClosedParenthesis)
        {
            Token parameterName = Expect(TokenKind.Identifier, ref index);
            Token colon = Expect(TokenKind.Colon, ref index);

            TypeNode type = ParseType(ref index);
            
            if (tokens[index].Kind is not TokenKind.Comma)
            {
                propertyDeclarations.Add(new ParameterDeclarationNode
                {
                    NameToken = parameterName,
                    ColonToken = colon,
                    Type = type,
                    CommaToken = null,
                    Index = parameterName.Index,
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
            });

            index++;
        }

        // in case of no trailing comma, just expect a closed parenthesis
        // else this is redundant and just does index++
        Token closedParenthesis = Expect(TokenKind.ClosedParenthesis, ref index);

        return (openParenthesis, propertyDeclarations.ToImmutable(), closedParenthesis);
    }

    private EnumSymbolDeclarationNode ParseEnumSymbolDeclaration(ref int index)
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
        };
    }

    private SettingDirectiveNode ParseSettingDirective(ref int index)
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
            TypeNode type = ParseType(ref index);
            
            Token semicolon = Expect(TokenKind.Semicolon, ref index);

            return new TypeSettingDirectiveNode
            {
                SettingKeywordToken = settingKeyword,
                SettingNameToken = identifier,
                ColonToken = colon,
                Type = type,
                SemicolonToken = semicolon,
                Index = nodeIndex,
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
            ExpressionNode expression = ParseExpression(ref index);
            
            Token semicolon = Expect(TokenKind.Semicolon, ref index);

            return new ExpressionSettingDirectiveNode
            {
                SettingKeywordToken = settingKeyword,
                SettingNameToken = identifier,
                ColonToken = colon,
                Expression = expression,
                SemicolonToken = semicolon,
                Index = nodeIndex,
            };
        }
    }

    private TopLevelNode? ParsePublicTopLevelNode(ref int index)
    {
        Debug.Assert(tokens[index] is TokenKind.PublicKeyword);

        long nodeIndex = tokens[index].Index;
        index++;

        switch (tokens[index])
        {
            case TokenKind.SpectrumKeyword:
                {
                    (string name, ImmutableArray<SpectrumOptionNode> options, string? defaultOption, _) = ParseSpectrumDeclaration(ref index);

                    return new SpectrumSymbolDeclarationNode
                    {
                        Name = name,
                        Options = options,
                        DefaultOption = defaultOption,
                        IsPublic = true,
                        Index = nodeIndex,
                    };
                }
            case TokenKind.OutcomeKeyword:
                {
                    (string name, ImmutableArray<string> options, string? defaultOption, _) = ParseOutcomeDeclaration(ref index);

                    return new OutcomeSymbolDeclarationNode
                    {
                        Name = name,
                        Options = options,
                        DefaultOption = defaultOption,
                        IsPublic = true,
                        Index = nodeIndex,
                    };
                }
            default:
                ErrorFound?.Invoke(Errors.UnexpectedToken(tokens[index - 1]));
                return ParseTopLevelNode(ref index);
        }
    }

    private InterfaceSymbolDeclarationNode? ParseInterfaceDeclaration(ref int index)
    {
        Debug.Assert(tokens[index].Kind == TokenKind.InterfaceKeyword);

        long nodeIndex = tokens[index].Index;
        index++;

        string name = Expect(TokenKind.Identifier, ref index).Text;

        _ = Expect(TokenKind.OpenParenthesis, ref index);

        ImmutableArray<InterfaceMethodDeclarationNode>.Builder methods = ImmutableArray.CreateBuilder<InterfaceMethodDeclarationNode>();

        while (tokens[index].Kind is not TokenKind.ClosedParenthesis)
        {
            var nextMethod = ParseInterfaceMethodDeclaration(ref index);

            if (nextMethod is null)
            {
                return null;
            }

            methods.Add(nextMethod);

            if (tokens[index].Kind is not TokenKind.Comma)
            {
                break;
            }

            index++;
        }

        _ = Expect(TokenKind.ClosedParenthesis, ref index);
        _ = Expect(TokenKind.Semicolon, ref index);

        return new InterfaceSymbolDeclarationNode
        {
            Name = name,
            Methods = methods.ToImmutable(),
            Index = nodeIndex,
        };
    }

    private InterfaceMethodDeclarationNode? ParseInterfaceMethodDeclaration(ref int index)
    {
        long nodeIndex = tokens[index].Index;

        InterfaceMethodKind kind;

        switch (tokens[index].Kind)
        {
            case TokenKind.ActionKeyword:
                kind = InterfaceMethodKind.Action;
                index++;
                break;
            case TokenKind.ChoiceKeyword:
                kind = InterfaceMethodKind.Choice;
                index++;
                break;
            default:
                ErrorFound?.Invoke(Errors.ExpectedToken(tokens[index], TokenKind.ActionKeyword));
                return null;
        }

        string name = Expect(TokenKind.Identifier, ref index).Text;

        ImmutableArray<ParameterDeclarationNode>? parameterList = ParseParameterList(ref index);

        if (parameterList is null)
        {
            return null;
        }

        return new InterfaceMethodDeclarationNode
        {
            Name = name,
            Kind = kind,
            Parameters = (ImmutableArray<ParameterDeclarationNode>)parameterList,
            Index = nodeIndex,
        };
    }

    private ReferenceSymbolDeclarationNode? ParseReferenceDeclaration(ref int index)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.ReferenceKeyword);

        long nodeIndex = tokens[index].Index;
        index++;

        string name = Expect(TokenKind.Identifier, ref index).Text;

        _ = Expect(TokenKind.Colon, ref index);

        string interfaceName = Expect(TokenKind.Identifier, ref index).Text;

        _ = Expect(TokenKind.Semicolon, ref index);

        return new ReferenceSymbolDeclarationNode
        {
            Name = name,
            InterfaceName = interfaceName,
            Index = nodeIndex,
        };
    }
}
