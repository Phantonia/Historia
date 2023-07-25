using System;

namespace Phantonia.Historia.Language.LexicalAnalysis;

public static class StringParser
{
    public static string Parse(ReadOnlySpan<char> stringLiteralWithoutQuotes)
    {
        Span<char> newString = stackalloc char[stringLiteralWithoutQuotes.Length];
        int newStringIndex = 0;

        bool escaped = false;

        for (int i = 0; i < stringLiteralWithoutQuotes.Length; i++)
        {
            if (stringLiteralWithoutQuotes[i] == '\\')
            {
                escaped = !escaped;

                if (!escaped)
                {
                    newString[newStringIndex++] = '\\';
                }
            }
            else if (escaped)
            {
                newString[newStringIndex++] = stringLiteralWithoutQuotes[i] switch
                {
                    'a' => '\a',
                    'b' => '\b',
                    'f' => '\f',
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    'v' => '\v',
                    _ => stringLiteralWithoutQuotes[i],
                };

                escaped = false;
            }
            else
            {
                newString[newStringIndex++] = stringLiteralWithoutQuotes[i];
            }
        }

        return new string(newString[..newStringIndex]);
    }
}
