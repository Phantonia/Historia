// uncomment this to see the example story class below
#define EXAMPLE_STORY

using System;
using System.CodeDom.Compiler;

namespace Phantonia.Historia.Language;

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
