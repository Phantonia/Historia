using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;

namespace Phantonia.Historia.Language.LexicalAnalysis;

public sealed class Lexer
{
    private const int TheEnd = -1;

    public Lexer(string code, int indexOffset = 0)
    {
        inputReader = new StringReader(code);
        currentIndex = indexOffset;
    }

    public Lexer(TextReader inputReader, int indexOffset = 0)
    {
        this.inputReader = inputReader;
        currentIndex = indexOffset;
    }

    private readonly TextReader inputReader;
    private int currentIndex;
    private readonly List<char> triviaBuffer = [];

    public event Action<Error>? ErrorFound;

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
                return new Token
                {
                    Kind = TokenKind.EndOfFile,
                    Index = currentIndex,
                    Text = "",
                    PrecedingTrivia = triviaBuffer.ClearToString(),
                };
            }

            if (char.IsWhiteSpace((char)inputReader.Peek()))
            {
                while (inputReader.Peek() != TheEnd)
                {
                    if (!char.IsWhiteSpace((char)inputReader.Peek()))
                    {
                        break;
                    }

                    _ = inputReader.ReadInto(triviaBuffer);
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
                    triviaBuffer.Add('/');
                    _ = inputReader.ReadInto(triviaBuffer);
                    currentIndex++;

                    do
                    {
                        currentIndex++;
                    } while (inputReader.ReadInto(triviaBuffer) is not ('\r' or '\n' or TheEnd));

                    continue; // start again from the beginning
                }
                else
                {
                    return new Token
                    {
                        Kind = TokenKind.Slash,
                        Text = "/",
                        Index = currentIndex,
                        PrecedingTrivia = triviaBuffer.ClearToString(),
                    };
                }
            }

            switch (inputReader.Peek())
            {
                case '{':
                    _ = inputReader.Read();
                    return new Token
                    {
                        Kind = TokenKind.OpenBrace,
                        Text = "{",
                        Index = currentIndex++,
                        PrecedingTrivia = triviaBuffer.ClearToString(),
                    };
                case '}':
                    _ = inputReader.Read();
                    return new Token
                    {
                        Kind = TokenKind.ClosedBrace,
                        Text = "}",
                        Index = currentIndex++,
                        PrecedingTrivia = triviaBuffer.ClearToString(),
                    };
                case '(':
                    _ = inputReader.Read();
                    return new Token
                    {
                        Kind = TokenKind.OpenParenthesis,
                        Text = "(",
                        Index = currentIndex++,
                        PrecedingTrivia = triviaBuffer.ClearToString(),
                    };
                case ')':
                    _ = inputReader.Read();
                    return new Token
                    {
                        Kind = TokenKind.ClosedParenthesis,
                        Text = ")",
                        Index = currentIndex++,
                        PrecedingTrivia = triviaBuffer.ClearToString(),
                    };
                case ';':
                    _ = inputReader.Read();
                    return new Token
                    {
                        Kind = TokenKind.Semicolon,
                        Text = ";",
                        Index = currentIndex++,
                        PrecedingTrivia = triviaBuffer.ClearToString(),
                    };
                case ':':
                    _ = inputReader.Read();
                    return new Token
                    {
                        Kind = TokenKind.Colon,
                        Text = ":",
                        Index = currentIndex++,
                        PrecedingTrivia = triviaBuffer.ClearToString(),
                    };
                case ',':
                    _ = inputReader.Read();
                    return new Token
                    {
                        Kind = TokenKind.Comma,
                        Text = ",",
                        Index = currentIndex++,
                        PrecedingTrivia = triviaBuffer.ClearToString(),
                    };
                case '=':
                    _ = inputReader.Read();
                    return new Token
                    {
                        Kind = TokenKind.Equals,
                        Text = "=",
                        Index = currentIndex++,
                        PrecedingTrivia = triviaBuffer.ClearToString(),
                    };
                case '.':
                    _ = inputReader.Read();
                    return new Token
                    {
                        Kind = TokenKind.Dot,
                        Text = ".",
                        Index = currentIndex++,
                        PrecedingTrivia = triviaBuffer.ClearToString(),
                    };
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
                        return new Token
                        {
                            Kind = TokenKind.Unknown,
                            Text = c.ToString(),
                            Index = currentIndex++,
                            PrecedingTrivia = triviaBuffer.ClearToString(),
                        };
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
                PrecedingTrivia = triviaBuffer.ClearToString(),
            };
        }
        else
        {
            return new Token
            {
                Kind = TokenKind.LessThan,
                Text = "<",
                Index = currentIndex - 1,
                PrecedingTrivia = triviaBuffer.ClearToString(),
            };
        }
    }

    private Token LexIntegerLiteral()
    {
        int startIndex = currentIndex;

        List<char> characters = [];

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
            PrecedingTrivia = triviaBuffer.ClearToString(),
        };
    }

    private Token LexStringLiteral()
    {
        int startIndex = currentIndex;
        Debug.Assert(inputReader.Peek() is '"' or '\'');

        List<char> verbatimCharacters = [];

        // delimiter is " or '
        char delimiter = (char)inputReader.ReadInto(verbatimCharacters);
        currentIndex++;

        // how many instances of the delimiter character are used to delimit the string?
        int delimiterCount = 1;

        while (inputReader.Peek() == delimiter)
        {
            delimiterCount++;
            _ = inputReader.ReadInto(verbatimCharacters);
            currentIndex++;
        }

        if (delimiterCount == 2) // represents string "" or ''
        {
            return new Token
            {
                Index = startIndex,
                Kind = TokenKind.StringLiteral,
                Text = new string(delimiter, 2),
                StringValue = "",
                PrecedingTrivia = triviaBuffer.ClearToString(),
            };
        }

        // delimiterCount must be 1 or >=3

        // whenever the delimiter is seen again (unless escaped), we reduce this countdown to see if it reaches 0 (i.e. the string is terminated)
        // else we reset it for possible later termination
        int delimiterCountdown = delimiterCount;
        List<char> stringCharacters = [];

        // stays false if the loop exits normally (i.e. the string is not terminated)
        // becomes true if the loop is exited by a break (i.e. the string terminated)
        bool terminated = false;

        while (inputReader.Peek() != TheEnd)
        {
            if (inputReader.Peek() == delimiter)
            {
                delimiterCountdown--;
                _ = inputReader.ReadInto(verbatimCharacters);
                currentIndex++;

                if (delimiterCountdown == 0)
                {
                    terminated = true;
                    break;
                }
            }
            else
            {
                if (delimiterCountdown != delimiterCount)
                {
                    // the string is not terminated here, reset the countdown

                    // delimiterCount - delimiterCountDown is the amount of characters we missed
                    for (int i = 0; i < delimiterCount - delimiterCountdown; i++)
                    {
                        stringCharacters.Add(delimiter);
                    }

                    delimiterCountdown = delimiterCount;
                }

                if (!TryParseNextCharInStringLiteral(out char next, startIndex, verbatimCharacters))
                {
                    return new Token
                    {
                        Kind = TokenKind.BrokenStringLiteral,
                        Index = startIndex,
                        Text = "",
                        PrecedingTrivia = triviaBuffer.ClearToString(),
                    };
                }

                stringCharacters.Add(next);
            }
        }

        if (!terminated)
        {
            ErrorFound?.Invoke(Errors.UnterminatedStringLiteral(startIndex));

            return new Token
            {
                Kind = TokenKind.BrokenStringLiteral,
                Index = startIndex,
                Text = "",
                PrecedingTrivia = triviaBuffer.ClearToString(),
            };
        }

        string verbatimText = string.Concat(verbatimCharacters);
        string literal = string.Concat(stringCharacters);

        return new Token
        {
            Kind = TokenKind.StringLiteral,
            Index = startIndex,
            Text = verbatimText,
            StringValue = literal,
            PrecedingTrivia = triviaBuffer.ClearToString(),
        };
    }

    private bool TryParseNextCharInStringLiteral(out char result, int startIndex, List<char> verbatimCharacters)
    {
        int nextChar = inputReader.ReadInto(verbatimCharacters);
        currentIndex++;

        if (nextChar == TheEnd)
        {
            result = default;
            ErrorFound?.Invoke(Errors.UnterminatedStringLiteral(startIndex));
            return false;
        }

        if (nextChar == '\\')
        {
            nextChar = inputReader.ReadInto(verbatimCharacters);
            currentIndex++;

            if (nextChar == TheEnd)
            {
                result = default;
                ErrorFound?.Invoke(Errors.UnterminatedStringLiteral(startIndex));
                return false;
            }

            switch (nextChar)
            {
                case '\'':
                case '"':
                case '\\':
                    result = (char)nextChar;
                    return true;
                case '0':
                    result = '\0';
                    return true;
                case 'a':
                    result = '\a';
                    return true;
                case 'b':
                    result = '\b';
                    return true;
                case 'f':
                    result = '\f';
                    return true;
                case 'n':
                    result = '\n';
                    return true;
                case 'r':
                    result = '\r';
                    return true;
                case 't':
                    result = '\t';
                    return true;
                case 'v':
                    result = '\v';
                    return true;
                case 'u':
                case 'U':
                    {
                        Span<char> hexDigits = stackalloc char[4];

                        for (int i = 0; i < 4; i++)
                        {
                            nextChar = inputReader.ReadInto(verbatimCharacters);
                            currentIndex++;

                            if (nextChar == TheEnd)
                            {
                                result = default;
                                ErrorFound?.Invoke(Errors.UnterminatedStringLiteral(startIndex));
                                return false;
                            }

                            if (nextChar is not ((>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F')))
                            {
                                result = default;
                                ErrorFound?.Invoke(Errors.InvalidEscapeSequence(startIndex));
                                return false;
                            }

                            hexDigits[i] = (char)nextChar;
                        }

                        static int GetValue(char hexDigit)
                        {
                            if (hexDigit is >= '0' and <= '9')
                            {
                                return hexDigit - '0';
                            }

                            if (hexDigit is >= 'a' and <= 'f')
                            {
                                return hexDigit - 'a' + 10;
                            }

                            // if (hexDigit is >= 'A' and <= 'F')
                            {
                                return hexDigit - 'A' + 10;
                            }
                        }

                        // each digit (0-9-f) has a value in [0,15]
                        // let the digits be a, b, c, d
                        // the value of the character is then d * 16^0 + c * 16^1 + b * 16^2 + a * 16^3
                        // 1 << 4n = 2^4n = 16^n
                        int value = GetValue(hexDigits[3]) * (1 << 0)
                                  + GetValue(hexDigits[2]) * (1 << 4)
                                  + GetValue(hexDigits[1]) * (1 << 8)
                                  + GetValue(hexDigits[0]) * (1 << 12)
                                  ;

                        result = (char)value;
                        return true;
                    }
                default:
                    result = default;
                    ErrorFound?.Invoke(Errors.InvalidEscapeSequence(startIndex));
                    return false;
            }
        }

        if (nextChar is '\n' or '\r')
        {
            result = default;
            ErrorFound?.Invoke(Errors.UnterminatedStringLiteral(startIndex));
            return false;
        }

        // invalid characters according to the C# spec
        if (nextChar is '\u0085' or '\u2028' or '\u2029')
        {
            result = default;
            ErrorFound?.Invoke(Errors.InvalidCharacter(startIndex));
            return false;
        }

        result = (char)nextChar;
        return true;
    }

    private Token LexIdentifierOrKeyword()
    {
        int startIndex = currentIndex;

        List<char> characters = [];

        while (inputReader.Peek() is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_')
        {
            characters.Add((char)inputReader.Read());
            currentIndex++;
        }

        string text = new(characters.ToArray()); // upper bound is exclusive

        TokenKind kind = text switch
        {
            "scene" => TokenKind.SceneKeyword,
            "chapter" => TokenKind.ChapterKeyword,
            "setting" => TokenKind.SettingKeyword,
            "record" => TokenKind.RecordKeyword,
            "union" => TokenKind.UnionKeyword,
            "enum" => TokenKind.EnumKeyword,
            "output" => TokenKind.OutputKeyword,
            "switch" => TokenKind.SwitchKeyword,
            "option" => TokenKind.OptionKeyword,
            "final" => TokenKind.FinalKeyword,
            "loop" => TokenKind.LoopKeyword,
            "branchon" => TokenKind.BranchOnKeyword,
            "other" => TokenKind.OtherKeyword,
            "public" => TokenKind.PublicKeyword,
            "outcome" => TokenKind.OutcomeKeyword,
            "default" => TokenKind.DefaultKeyword,
            "spectrum" => TokenKind.SpectrumKeyword,
            "strengthen" => TokenKind.StrengthenKeyword,
            "weaken" => TokenKind.WeakenKeyword,
            "by" => TokenKind.ByKeyword,
            "call" => TokenKind.CallKeyword,
            //"checkpoint" => TokenKind.CheckpointKeyword,
            "interface" => TokenKind.InterfaceKeyword,
            "reference" => TokenKind.ReferenceKeyword,
            "action" => TokenKind.ActionKeyword,
            "choice" => TokenKind.ChoiceKeyword,
            "run" => TokenKind.RunKeyword,
            "choose" => TokenKind.ChooseKeyword,
            "is" => TokenKind.IsKeyword,
            "and" => TokenKind.AndKeyword,
            "or" => TokenKind.OrKeyword,
            "not" => TokenKind.NotKeyword,
            "if" => TokenKind.IfKeyword,
            "else" => TokenKind.ElseKeyword,
            _ => TokenKind.Identifier,
        };

        return new Token
        {
            Kind = kind,
            Text = text,
            Index = startIndex,
            PrecedingTrivia = triviaBuffer.ClearToString(),
        };
    }
}
