using Phantonia.Historia.Language.Ast;
using System.Collections.Immutable;
using System.Threading;

namespace Phantonia.Historia.Language;

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
        if (index >= historiaText.Length)
        {
            return new Token { Kind = TokenKind.EndOfFile, Index = historiaText.Length, Text = "" };
        }

        while (char.IsWhiteSpace(historiaText[index]))
        {
            index++;

            if (index >= historiaText.Length)
            {
                return new Token { Kind = TokenKind.EndOfFile, Index = historiaText.Length, Text = "" };
            }
        }

        return historiaText[index] switch
        {
            '{' => new Token { Kind = TokenKind.OpenBrace, Text = "{", Index = index++ },
            '}' => new Token { Kind = TokenKind.ClosedBrace, Text = "}", Index = index++ },
            '(' => new Token { Kind = TokenKind.OpenParenthesis, Text = "(", Index = index++ },
            ')' => new Token { Kind = TokenKind.ClosedParenthesis, Text = ")", Index = index++ },
            ';' => new Token { Kind = TokenKind.Semicolon, Text = ";", Index = index++ },
            >= '0' and <= '9' => LexIntegerLiteral(ref index),
            (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_' => LexIdentifierOrKeyword(ref index),
            _ => new Token { Kind = TokenKind.Unknown, Text = historiaText[index].ToString(), Index = index++ },
        };
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

    private Token LexIdentifierOrKeyword(ref int index)
    {
        int startIndex = index;

        while (historiaText[index] is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_')
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
            "output" => TokenKind.OutputKeyword,
            "switch" => TokenKind.SwitchKeyword,
            "option" => TokenKind.OptionKeyword,
            _ => TokenKind.Identifier,
        };

        return new Token { Kind = kind, Text = text, Index = startIndex };
    }
}
