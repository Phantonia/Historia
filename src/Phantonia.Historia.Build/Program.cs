using Phantonia.Historia.Language;
using System.Diagnostics;

namespace Phantonia.Historia.Build;

internal static class Program
{
    private const string InputPath = @"C:\Users\User\Documents\Projects\Stories\AntiFairytale\Historia";
    private const string OutputPath = @"C:\Users\User\Documents\Godot\AntiFairytaleTextAdventure\Historia.cs";
    
    private static void Main(string[] args)
    {
        using TextReader inputReader = new StreamReader(GetInputStream());
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

        string code = new StreamReader(GetInputStream()).ReadToEnd();

        foreach (Error error in result.Errors)
        {
            Console.WriteLine(Errors.GenerateFullMessage(code, error));
            Console.WriteLine();
        }
    }

    private static Stream GetInputStream()
    {
        IEnumerable<string> files = Directory.GetFiles(InputPath, "*.hstr");

        List<Stream> streams = [];

        foreach (string hstrFile in files)
        {
            streams.Add(new FileStream(hstrFile, FileMode.Open, FileAccess.Read));
        }

        return new ConcatenatedStream(streams);
    }
}
