using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;

namespace Phantonia.Historia.Language.LexicalAnalysis;

public sealed class Lexer
{
    private const int TheEnd = -1;

    public Lexer(string code)
    {
        inputReader = new StringReader(code);
    }

    public Lexer(TextReader inputReader)
    {
        this.inputReader = inputReader;
    }

    private readonly TextReader inputReader;
    private int currentIndex = 0;

    // reLex, take it easy
    public ImmutableArray<Token> Lex()
    {
        ImmutableArray<Token>.Builder tokenBuilder = ImmutableArray.CreateBuilder<Token>();

        Token nextToken = LexSingleToken();

        while (nextToken.Kind != TokenKind.EndOfFile)
        {
            tokenBuilder.Add(nextToken);

            nextToken = LexSingleToken();
        }

        tokenBuilder.Add(nextToken);

        return tokenBuilder.ToImmutable();
    }

    private Token LexSingleToken()
    {
        while (true)
        {
            if (inputReader.Peek() == TheEnd)
            {
                return new Token { Kind = TokenKind.EndOfFile, Index = currentIndex, Text = "" };
            }

            if (char.IsWhiteSpace((char)inputReader.Peek()))
            {
                while (inputReader.Peek() != TheEnd)
                {
                    if (!char.IsWhiteSpace((char)inputReader.Peek()))
                    {
                        break;
                    }

                    _ = inputReader.Read();
                    currentIndex++;
                }

                continue; // start again from the beginning
            }

            if (inputReader.Peek() == '/')
            {
                _ = inputReader.Read();
                currentIndex++;

                if (inputReader.Peek() == '/')
                {
                    _ = inputReader.Read();
                    currentIndex++;

                    do
                    {
                        currentIndex++;
                    } while (inputReader.Read() is not ('\r' or '\n' or TheEnd));

                    continue; // start again from the beginning
                }
                else
                {
                    return new Token
                    {
                        Kind = TokenKind.Slash,
                        Text = "/",
                        Index = currentIndex,
                    };
                }
            }

            switch (inputReader.Peek())
            {
                case '{':
                    _ = inputReader.Read();
                    return new Token { Kind = TokenKind.OpenBrace, Text = "{", Index = currentIndex++, };
                case '}':
                    _ = inputReader.Read();
                    return new Token { Kind = TokenKind.ClosedBrace, Text = "}", Index = currentIndex++, };
                case '(':
                    _ = inputReader.Read();
                    return new Token { Kind = TokenKind.OpenParenthesis, Text = "(", Index = currentIndex++, };
                case ')':
                    _ = inputReader.Read();
                    return new Token { Kind = TokenKind.ClosedParenthesis, Text = ")", Index = currentIndex++, };
                case ';':
                    _ = inputReader.Read();
                    return new Token { Kind = TokenKind.Semicolon, Text = ";", Index = currentIndex++, };
                case ':':
                    _ = inputReader.Read();
                    return new Token { Kind = TokenKind.Colon, Text = ":", Index = currentIndex++, };
                case ',':
                    _ = inputReader.Read();
                    return new Token { Kind = TokenKind.Comma, Text = ",", Index = currentIndex++, };
                case '=':
                    _ = inputReader.Read();
                    return new Token { Kind = TokenKind.Equals, Text = "=", Index = currentIndex++, };
                case '"' or '\'':
                    return LexStringLiteral();
                case '<':
                    return LexLessThan();
                case >= '0' and <= '9':
                    return LexIntegerLiteral();
                case >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_':
                    return LexIdentifierOrKeyword();
                default:
                    {
                        char c = (char)inputReader.Read();
                        return new Token { Kind = TokenKind.Unknown, Text = c.ToString(), Index = currentIndex++, };
                    }
            }
        }
    }

    private Token LexLessThan()
    {
        Debug.Assert(inputReader.Peek() is '<');
        _ = inputReader.Read();
        currentIndex++;

        if (inputReader.Peek() is '=')
        {
            _ = inputReader.Read();
            currentIndex++;
            return new Token
            {
                Kind = TokenKind.LessThanOrEquals,
                Text = "<=",
                Index = currentIndex - 2,
            };
        }
        else
        {
            return new Token
            {
                Kind = TokenKind.LessThan,
                Text = "<",
                Index = currentIndex - 1,
            };
        }
    }

    private Token LexIntegerLiteral()
    {
        int startIndex = currentIndex;

        List<char> characters = new();

        while (inputReader.Peek() is >= '0' and <= '9')
        {
            characters.Add((char)inputReader.Read());
            currentIndex++;

            if (inputReader.Peek() == TheEnd)
            {
                break;
            }
        }

        string text = new(characters.ToArray());
        int value = int.Parse(text);
        return new Token
        {
            Kind = TokenKind.IntegerLiteral,
            Text = text,
            IntegerValue = value,
            Index = startIndex,
        };
    }

    private Token LexStringLiteral()
    {
        int startIndex = currentIndex;
        Debug.Assert(inputReader.Peek() is '"' or '\'');

        char delimiter = (char)inputReader.Read();
        currentIndex++;
        int delimiterCount = 1;

        List<char> characters = new() { delimiter };

        while (inputReader.Peek() != TheEnd)
        {
            if (inputReader.Peek() == delimiter)
            {
                _ = inputReader.Read();
                currentIndex++;
                delimiterCount++;
                characters.Add(delimiter);
            }
            else
            {
                break;
            }
        }

        int delimiterCountdown = delimiterCount;

        while (inputReader.Peek() != TheEnd && delimiterCountdown > 0)
        {
            if (inputReader.Peek() is '\r' or '\n')
            {
                return new Token
                {
                    Kind = TokenKind.BrokenStringLiteral,
                    Text = new string(characters.ToArray()),
                    Index = startIndex,
                };
            }
            else if (inputReader.Peek() == delimiter)
            {
                delimiterCountdown--;
            }
            else
            {
                delimiterCountdown = delimiterCount;
            }

            characters.Add((char)inputReader.Read());
            currentIndex++;
        }

        string realString = StringParser.Parse(characters.ToArray().AsSpan()[delimiterCount..^delimiterCount]);

        return new Token
        {
            Kind = delimiterCountdown == 0 ? TokenKind.StringLiteral : TokenKind.BrokenStringLiteral,
            Text = delimiterCountdown == 0 ? realString : new string(characters.ToArray()),
            Index = startIndex,
        };
    }

    private Token LexIdentifierOrKeyword()
    {
        int startIndex = currentIndex;

        List<char> characters = new();

        while (inputReader.Peek() is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_')
        {
            characters.Add((char)inputReader.Read());
            currentIndex++;
        }

        string text = new(characters.ToArray()); // upper bound is exclusive

        TokenKind kind = text switch
        {
            "scene" => TokenKind.SceneKeyword,
            "setting" => TokenKind.SettingKeyword,
            "record" => TokenKind.RecordKeyword,
            "union" => TokenKind.UnionKeyword,
            "output" => TokenKind.OutputKeyword,
            "switch" => TokenKind.SwitchKeyword,
            "option" => TokenKind.OptionKeyword,
            "branchon" => TokenKind.BranchOnKeyword,
            "other" => TokenKind.OtherKeyword,
            "outcome" => TokenKind.OutcomeKeyword,
            "default" => TokenKind.DefaultKeyword,
            "spectrum" => TokenKind.SpectrumKeyword,
            "strengthen" => TokenKind.StrengthenKeyword,
            "weaken" => TokenKind.WeakenKeyword,
            "by" => TokenKind.ByKeyword,
            _ => TokenKind.Identifier,
        };

        return new Token { Kind = kind, Text = text, Index = startIndex };
    }
}
