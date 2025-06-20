using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis;
using System.CodeDom.Compiler;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed class ConstantsEmitter(StoryNode boundStory, SymbolTable symbolTable, Settings settings, ulong fingerprint, IndentedTextWriter writer)
{
    public void GenerateConstantsClass()
    {
        GeneralEmission.GenerateGeneratedCodeAttribute(writer);
        writer.Write("public static class ");
        writer.Write(settings.StoryName);
        writer.WriteLine("Constants");
        writer.BeginBlock();

        writer.Write("public const ulong Fingerprint = 0x");
        writer.Write(fingerprint.ToString("x"));
        writer.WriteLine(';');

        writer.Write("public const int SaveDataLength = ");
        writer.Write(SaveDataEmitter.GetByteCount(boundStory, symbolTable));
        writer.WriteLine(';');

        writer.EndBlock(); // class
    }
}
