using System;
using System.CodeDom.Compiler;

namespace Phantonia.Historia.Language.CodeGeneration;

internal static class IndentedTextWriterExtensions
{
    public static void WriteManyLines(this IndentedTextWriter writer, string text)
    {
        foreach (string line in text.Split(Environment.NewLine))
        {
            writer.WriteLine(line);
        }
    }
}
