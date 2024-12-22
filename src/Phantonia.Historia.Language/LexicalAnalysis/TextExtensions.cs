using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.LexicalAnalysis;

public static class TextExtensions
{
    public static int ReadInto(this TextReader reader, IList<char> buffer)
    {
        int c = reader.Read();

        if (c >= 0)
        {
            buffer.Add((char)c);
        }

        return c;
    }

    public static string ClearToString(this IList<char> buffer)
    {
        string str = string.Concat(buffer);
        buffer.Clear();
        return str;
    }
}
