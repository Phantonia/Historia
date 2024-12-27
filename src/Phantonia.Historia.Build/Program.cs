using Phantonia.Historia.Language;
using System.Diagnostics;

namespace Phantonia.Historia.Build;

internal static class Program
{
    private static void Main(string[] args)
    {
        const string InputPath = @"C:\Users\User\Documents\Projects\Stories\AntiFairytale\Historia\Ch1_FreedomBreaksFree.hstr";
        const string OutputPath = @"C:\Users\User\Documents\Godot\AntiFairytaleTextAdventure\Historia.cs";

        using TextReader inputReader = new StreamReader(InputPath);
        using TextWriter outputWriter = new StreamWriter(OutputPath);

        Stopwatch sw = Stopwatch.StartNew();
        Compiler compiler = new(inputReader, outputWriter);
        CompilationResult result = compiler.Compile();
        sw.Stop();

        if (result.IsValid)
        {
            Console.WriteLine($"Compilation successful after {sw.Elapsed.TotalSeconds} seconds!");
            return;
        }

        Console.WriteLine($"Compilation did not succeed after {sw.Elapsed.TotalSeconds} seconds.");

        string code = File.ReadAllText(InputPath);

        foreach (Error error in result.Errors)
        {
            Console.WriteLine(Errors.GenerateFullMessage(code, error));
            Console.WriteLine();
        }
    }
}