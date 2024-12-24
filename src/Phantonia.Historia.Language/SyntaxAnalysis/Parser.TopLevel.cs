﻿using Phantonia.Historia.Language.LexicalAnalysis;
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
            case { Kind: TokenKind.EnumKeyword }:
                return ParseEnumSymbolDeclaration(ref index);
            case { Kind: TokenKind.SettingKeyword }:
                return ParseSettingDirective(ref index);
            case { Kind: TokenKind.PublicKeyword }:
                return ParsePublicTopLevelNode(ref index);
            case { Kind: TokenKind.OutcomeKeyword }:
                {
                    (string name, ImmutableArray<string> options, string? defaultOption, int nodeIndex) = ParseOutcomeDeclaration(ref index);

                    return new OutcomeSymbolDeclarationNode
                    {
                        Name = name,
                        Options = options,
                        DefaultOption = defaultOption,
                        IsPublic = false,
                        Index = nodeIndex,
                    };
                }
            case { Kind: TokenKind.SpectrumKeyword }:
                {
                    (string name, ImmutableArray<SpectrumOptionNode> options, string? defaultOption, int nodeIndex) = ParseSpectrumDeclaration(ref index);

                    return new SpectrumSymbolDeclarationNode
                    {
                        Name = name,
                        Options = options,
                        DefaultOption = defaultOption,
                        IsPublic = false,
                        Index = nodeIndex,
                    };
                }
            case { Kind: TokenKind.InterfaceKeyword }:
                return ParseInterfaceDeclaration(ref index);
            case { Kind: TokenKind.ReferenceKeyword }:
                return ParseReferenceDeclaration(ref index);
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

    private UnionSymbolDeclarationNode? ParseUnionSymbolDeclaration(ref int index)
    {
        Debug.Assert(tokens[index] is { Kind: TokenKind.UnionKeyword });

        int nodeIndex = tokens[index].Index;
        index++;

        string name = Expect(TokenKind.Identifier, ref index).Text;

        _ = Expect(TokenKind.OpenParenthesis, ref index);

        ImmutableArray<TypeNode>.Builder subtypeBuilder = ImmutableArray.CreateBuilder<TypeNode>();

        while (tokens[index] is not { Kind: TokenKind.ClosedParenthesis })
        {
            TypeNode? subtype = ParseType(ref index);

            if (subtype is null)
            {
                return null;
            }

            subtypeBuilder.Add(subtype);

            if (tokens[index] is not { Kind: TokenKind.Comma })
            {
                break;
            }

            index++;
        }

        // in case of no trailing comma, just expect a closed parenthesis
        // else this is redundant and just does index++
        _ = Expect(TokenKind.ClosedParenthesis, ref index);

        _ = Expect(TokenKind.Semicolon, ref index);

        return new UnionSymbolDeclarationNode
        {
            Name = name,
            Subtypes = subtypeBuilder.ToImmutable(),
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

        Debug.Assert(tokens[index] is { Kind: TokenKind.RecordKeyword });

        int nodeIndex = tokens[index].Index;

        index++;

        Token identifierToken = Expect(TokenKind.Identifier, ref index);

        ImmutableArray<PropertyDeclarationNode>? propertyDeclarations = ParsePropertyDeclarationList(ref index);

        if (propertyDeclarations is null)
        {
            return null;
        }

        _ = Expect(TokenKind.Semicolon, ref index);

        return new RecordSymbolDeclarationNode
        {
            Name = identifierToken.Text,
            Properties = (ImmutableArray<PropertyDeclarationNode>)propertyDeclarations,
            Index = nodeIndex,
        };
    }

    private ImmutableArray<PropertyDeclarationNode>? ParsePropertyDeclarationList(ref int index)
    {
        _ = Expect(TokenKind.OpenParenthesis, ref index);

        ImmutableArray<PropertyDeclarationNode>.Builder propertyDeclarations = ImmutableArray.CreateBuilder<PropertyDeclarationNode>();

        while (tokens[index] is not { Kind: TokenKind.ClosedParenthesis })
        {
            Token propertyIdentifierToken = Expect(TokenKind.Identifier, ref index);
            _ = Expect(TokenKind.Colon, ref index);

            TypeNode? type = ParseType(ref index);
            if (type is null)
            {
                return null;
            }

            propertyDeclarations.Add(new PropertyDeclarationNode
            {
                Name = propertyIdentifierToken.Text,
                Type = type,
                Index = propertyIdentifierToken.Index,
            });

            if (tokens[index] is not { Kind: TokenKind.Comma })
            {
                break;
            }

            index++;
        }

        // in case of no trailing comma, just expect a closed parenthesis
        // else this is redundant and just does index++
        _ = Expect(TokenKind.ClosedParenthesis, ref index);

        return propertyDeclarations.ToImmutable();
    }

    private EnumSymbolDeclarationNode? ParseEnumSymbolDeclaration(ref int index)
    {
        // spec 1.3.1.2:
        // EnumDeclaration : 'enum' identifier '(' (identifier (',' identifier)* ','?)? ')' ';';
        Debug.Assert(tokens[index] is { Kind: TokenKind.EnumKeyword });

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
        _ = Expect(TokenKind.Semicolon, ref index);

        return new EnumSymbolDeclarationNode
        {
            Name = name,
            Options = optionsBuilder.ToImmutable(),
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

    private TopLevelNode? ParsePublicTopLevelNode(ref int index)
    {
        Debug.Assert(tokens[index] is { Kind: TokenKind.PublicKeyword });

        int nodeIndex = tokens[index].Index;
        index++;

        switch (tokens[index])
        {
            case { Kind: TokenKind.SpectrumKeyword }:
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
            case { Kind: TokenKind.OutcomeKeyword }:
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

        int nodeIndex = tokens[index].Index;
        index++;

        string name = Expect(TokenKind.Identifier, ref index).Text;

        _ = Expect(TokenKind.OpenParenthesis, ref index);

        ImmutableArray<InterfaceMethodDeclarationNode>.Builder methods = ImmutableArray.CreateBuilder<InterfaceMethodDeclarationNode>();

        while (tokens[index] is not { Kind: TokenKind.ClosedParenthesis })
        {
            var nextMethod = ParseInterfaceMethodDeclaration(ref index);

            if (nextMethod is null)
            {
                return null;
            }

            methods.Add(nextMethod);

            if (tokens[index] is not { Kind: TokenKind.Comma })
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
        int nodeIndex = tokens[index].Index;

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

        ImmutableArray<PropertyDeclarationNode>? parameterList = ParsePropertyDeclarationList(ref index);

        if (parameterList is null)
        {
            return null;
        }

        return new InterfaceMethodDeclarationNode
        {
            Name = name,
            Kind = kind,
            Parameters = (ImmutableArray<PropertyDeclarationNode>)parameterList,
            Index = nodeIndex,
        };
    }

    private ReferenceSymbolDeclarationNode? ParseReferenceDeclaration(ref int index)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.ReferenceKeyword);

        int nodeIndex = tokens[index].Index;
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
