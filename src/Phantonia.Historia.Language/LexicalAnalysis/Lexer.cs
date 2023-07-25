using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Phantonia.Historia.Language.LexicalAnalysis;

public sealed class Lexer
{
    public Lexer(string historiaText)
    {
        this.historiaText = historiaText;
    }

    private readonly string historiaText;

    // reLex, take it easy
    public ImmutableArray<Token> Lex()
    {
        ImmutableArray<Token>.Builder tokenBuilder = ImmutableArray.CreateBuilder<Token>();

        int index = 0;
        Token nextToken = LexSingleToken(ref index);

        while (nextToken.Kind != TokenKind.EndOfFile)
        {
            tokenBuilder.Add(nextToken);

            nextToken = LexSingleToken(ref index);
        }

        tokenBuilder.Add(nextToken);

        return tokenBuilder.ToImmutable();
    }

    private Token LexSingleToken(ref int index)
    {
        while (true)
        {
            if (index >= historiaText.Length)
            {
                return new Token { Kind = TokenKind.EndOfFile, Index = historiaText.Length, Text = "" };
            }

            if (char.IsWhiteSpace(historiaText[index]))
            {
                for (; index < historiaText.Length; index++)
                {
                    if (!char.IsWhiteSpace(historiaText[index]))
                    {
                        break;
                    }
                }

                continue; // start again from the beginning
            }

            if (historiaText[index] == '/' && index < historiaText.Length - 1 && historiaText[index + 1] == '/')
            {
                index += 2;

                for (; index < historiaText.Length; index++)
                {
                    if (historiaText[index] == '\r' || historiaText[index] == '\n')
                    {
                        index++;
                        break; ;
                    }
                }

                continue; // start again from the beginning
            }

            return historiaText[index] switch
            {
                '{' => new Token { Kind = TokenKind.OpenBrace, Text = "{", Index = index++ },
                '}' => new Token { Kind = TokenKind.ClosedBrace, Text = "}", Index = index++ },
                '(' => new Token { Kind = TokenKind.OpenParenthesis, Text = "(", Index = index++ },
                ')' => new Token { Kind = TokenKind.ClosedParenthesis, Text = ")", Index = index++ },
                ';' => new Token { Kind = TokenKind.Semicolon, Text = ";", Index = index++ },
                ':' => new Token { Kind = TokenKind.Colon, Text = ":", Index = index++ },
                ',' => new Token { Kind = TokenKind.Comma, Text = ",", Index = index++ },
                '=' => new Token { Kind = TokenKind.Equals, Text = "=", Index = index++ },
                '"' or '\'' => LexStringLiteral(ref index),
                >= '0' and <= '9' => LexIntegerLiteral(ref index),
                >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_' => LexIdentifierOrKeyword(ref index),
                _ => new Token { Kind = TokenKind.Unknown, Text = historiaText[index].ToString(), Index = index++ },
            };
        }
    }

    private Token LexIntegerLiteral(ref int index)
    {
        int startIndex = index;

        while (historiaText[index] is >= '0' and <= '9')
        {
            index++;

            if (index >= historiaText.Length)
            {
                break;
            }
        }

        string text = historiaText[startIndex..index]; // upper bound is exclusive
        int value = int.Parse(text);
        return new Token { Kind = TokenKind.IntegerLiteral, Text = text, IntegerValue = value, Index = startIndex };
    }

    private Token LexStringLiteral(ref int index)
    {
        int startIndex = index;
        Debug.Assert(historiaText[index] is '"' or '\'');

        char delimiter = historiaText[index];
        int delimiterCount = 0;

        for (; index < historiaText.Length; index++)
        {
            if (historiaText[index] == delimiter)
            {
                delimiterCount++;
            }
            else
            {
                break;
            }
        }

        while (index < historiaText.Length && historiaText[index] != delimiter)
        {
            if (historiaText[index] is '\r' or '\n')
            {
                return new Token
                {
                    Kind = TokenKind.BrokenStringLiteral,
                    Text = historiaText[startIndex..index],
                    Index = startIndex,
                };
            }

            index++;
        }

        int delimiterCountdown = delimiterCount;

        while (index < historiaText.Length && delimiterCountdown > 0)
        {
            if (historiaText[index] == delimiter)
            {
                delimiterCountdown--;
            }
            else
            {
                delimiterCountdown = delimiterCount;
            }

            index++;
        }

        return new Token
        {
            Kind = delimiterCountdown == 0 ? TokenKind.StringLiteral : TokenKind.BrokenStringLiteral,
            Text = historiaText[startIndex..index],
            Index = startIndex,
        };
    }

    private Token LexIdentifierOrKeyword(ref int index)
    {
        int startIndex = index;

        while (historiaText[index] is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_')
        {
            index++;

            if (index >= historiaText.Length)
            {
                break;
            }
        }

        string text = historiaText[startIndex..index]; // upper bound is exclusive

        TokenKind kind = text switch
        {
            "scene" => TokenKind.SceneKeyword,
            "setting" => TokenKind.SettingKeyword,
            "record" => TokenKind.RecordKeyword,
            "output" => TokenKind.OutputKeyword,
            "switch" => TokenKind.SwitchKeyword,
            "option" => TokenKind.OptionKeyword,
            "branchon" => TokenKind.BranchOnKeyword,
            "other" => TokenKind.OtherKeyword,
            _ => TokenKind.Identifier,
        };

        return new Token { Kind = kind, Text = text, Index = startIndex };
    }
}
