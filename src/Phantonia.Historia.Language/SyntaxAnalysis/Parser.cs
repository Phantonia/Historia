using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using Phantonia.Historia.Language.SyntaxAnalysis.Types;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis;

public sealed partial class Parser(ImmutableArray<Token> tokens)
{
    public event Action<Error>? ErrorFound;

    public StoryNode Parse()
    {
        int index = 0;
        ImmutableArray<TopLevelNode>.Builder topLevelBuilder = ImmutableArray.CreateBuilder<TopLevelNode>();

        TopLevelNode nextTopLevelNode = ParseTopLevelNode(ref index, []);

        while (nextTopLevelNode is not MissingTopLevelNode)
        {
            topLevelBuilder.Add(nextTopLevelNode);

            nextTopLevelNode = ParseTopLevelNode(ref index, []);
        }

        // add missing tln anyway if it has preceding tokens
        if (nextTopLevelNode.PrecedingTokens.Count > 0)
        {
            topLevelBuilder.Add(nextTopLevelNode);
        }

        return new StoryNode
        {
            TopLevelNodes = topLevelBuilder.ToImmutable(),
            Index = tokens.Length > 0 ? tokens[0].Index : 0,
            Length = tokens[^1].Index,
            PrecedingTokens = [],
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
            ErrorFound?.Invoke(Errors.ExpectedToken(tokens[index], kind));
            return new Token
            {
                Kind = TokenKind.Missing,
                Index = tokens[index].Index,
                Text = "",
                PrecedingTrivia = "",
            };
        }
    }

    private Token ExpectOneOf(ref int index, params TokenKind[] kinds)
    {
        if (kinds.Contains(tokens[index].Kind))
        {
            return tokens[index++];
        }
        else
        {
            ErrorFound?.Invoke(Errors.ExpectedToken(tokens[index], kinds[0])); // TODO: better error here
            return new Token
            {
                Kind = TokenKind.Missing,
                Index = tokens[index].Index,
                Text = "",
                PrecedingTrivia = "",
            };
        }
    }

    private OutcomeDeclarationInfo ParseOutcomeDeclaration(ref int index)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.OutcomeKeyword);
        Token outcomeKeyword = tokens[index];

        long nodeIndex = tokens[index].Index;
        index++;

        Token name = Expect(TokenKind.Identifier, ref index);
        Token openParenthesis = Expect(TokenKind.OpenParenthesis, ref index);

        ImmutableArray<Token>.Builder optionsBuilder = ImmutableArray.CreateBuilder<Token>();
        ImmutableArray<Token>.Builder commaBuilder = ImmutableArray.CreateBuilder<Token>();

        while (tokens[index].Kind is TokenKind.Identifier)
        {
            optionsBuilder.Add(tokens[index]);

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

        Token? defaultKeyword = null;
        Token? defaultOption = null;

        if (tokens[index].Kind is TokenKind.DefaultKeyword)
        {
            defaultKeyword = tokens[index];

            index++;

            defaultOption = Expect(TokenKind.Identifier, ref index);
        }

        Token semicolon = Expect(TokenKind.Semicolon, ref index);

        return new OutcomeDeclarationInfo(outcomeKeyword, name, openParenthesis, optionsBuilder.ToImmutable(), commaBuilder.ToImmutable(), closedParenthesis, defaultKeyword, defaultOption, semicolon, nodeIndex);
    }

    private SpectrumDeclarationInfo ParseSpectrumDeclaration(ref int index)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.SpectrumKeyword);
        Token spectrumKeyword = tokens[index];
        long nodeIndex = spectrumKeyword.Index;
        index++;

        Token name = Expect(TokenKind.Identifier, ref index);

        Token openParenthesis = Expect(TokenKind.OpenParenthesis, ref index);

        ImmutableArray<SpectrumOptionNode>.Builder optionBuilder = ImmutableArray.CreateBuilder<SpectrumOptionNode>();

        while (tokens[index].Kind is TokenKind.Identifier)
        {
            Token optionName = tokens[index];
            long optionIndex = tokens[index].Index;
            index++;

            if (tokens[index].Kind is not (TokenKind.LessThan or TokenKind.LessThanOrEquals))
            {
                optionBuilder.Add(new SpectrumOptionNode
                {
                    NameToken = optionName,
                    InequalitySignToken = null,
                    NumeratorToken = null,
                    SlashToken = null,
                    DenominatorToken = null,
                    CommaToken = null,
                    Index = optionIndex,
                    PrecedingTokens = [],
                });

                break;
            }

            Token inequalitySign = tokens[index];
            index++;

            Token numerator = Expect(TokenKind.IntegerLiteral, ref index);
            Token slash = Expect(TokenKind.Slash, ref index);
            Token denominator = Expect(TokenKind.IntegerLiteral, ref index);
            Token comma = Expect(TokenKind.Comma, ref index);

            optionBuilder.Add(new SpectrumOptionNode
            {
                NameToken = optionName,
                InequalitySignToken = inequalitySign,
                NumeratorToken = numerator,
                SlashToken = slash,
                DenominatorToken = denominator,
                CommaToken = comma,
                Index = optionIndex,
                PrecedingTokens = [],
            });
        }

        Token closedParenthesis = Expect(TokenKind.ClosedParenthesis, ref index);

        Token? defaultKeyword = null;
        Token? defaultOption = null;

        if (tokens[index].Kind is TokenKind.DefaultKeyword)
        {
            defaultKeyword = tokens[index];
            index++;
            defaultOption = Expect(TokenKind.Identifier, ref index);
        }

        Token semicolon = Expect(TokenKind.Semicolon, ref index);

        return new SpectrumDeclarationInfo(spectrumKeyword, name, openParenthesis, optionBuilder.ToImmutable(), closedParenthesis, defaultKeyword, defaultOption, semicolon, nodeIndex);
    }

    private TypeNode ParseType(ref int index, ImmutableList<Token> precedingTokens)
    {
        switch (tokens[index].Kind)
        {
            case TokenKind.Identifier:
                return new IdentifierTypeNode
                {
                    IdentifierToken = tokens[index],
                    Index = tokens[index++].Index,
                    PrecedingTokens = precedingTokens,
                };
            case TokenKind.EndOfFile:
                ErrorFound?.Invoke(Errors.UnexpectedEndOfFile(tokens[index]));
                return new MissingTypeNode
                {
                    Index = tokens[index].Index,
                    PrecedingTokens = precedingTokens,
                };
            default:
                Token unexpectedToken = tokens[index];
                ErrorFound?.Invoke(Errors.UnexpectedToken(unexpectedToken));
                index++;
                return ParseType(ref index, precedingTokens.Add(unexpectedToken));
        }
    }
}
