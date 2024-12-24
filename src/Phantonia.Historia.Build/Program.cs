using Phantonia.Historia.Language;

namespace Phantonia.Historia.Build;

internal static class Program
{
    private static void Main(string[] args)
    {
        const string InputPath = @"C:\Users\User\Documents\Projects\Stories\AntiFairytale\Historia\Ch1_FreedomBreaksFree.hstr";
        const string OutputPath = @"C:\Users\User\Documents\Godot\AntiFairytaleTextAdventure\Historia.cs";

        using TextReader inputReader = new StreamReader(InputPath);
        using TextWriter outputWriter = new StreamWriter(OutputPath);

        Compiler compiler = new(inputReader, outputWriter);
        CompilationResult result = compiler.Compile();

        if (result.IsValid)
        {
            Console.WriteLine("Compilation successful!");
            return;
        }

        string code = File.ReadAllText(InputPath);

        foreach (Error error in result.Errors)
        {
            Console.WriteLine(Errors.GenerateFullMessage(code, error));
            Console.WriteLine();
        }
    }
}