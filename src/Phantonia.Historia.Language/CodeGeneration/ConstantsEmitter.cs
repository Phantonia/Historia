using System.CodeDom.Compiler;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed class ConstantsEmitter(Settings settings, ulong fingerprint, IndentedTextWriter writer)
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

        writer.EndBlock(); // class
    }
}
