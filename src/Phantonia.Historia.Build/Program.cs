using Phantonia.Historia.Language;
using System.Diagnostics;

namespace Phantonia.Historia.Build;

internal static class Program
{
    private const string PathFile = @"..\..\..\Paths.txt";
    
    private static void Main(string[] args)
    {
        if (!File.Exists(PathFile))
        {
            Console.WriteLine("Define a path file for both input and output path");
            return;
        }

        string[] paths = File.ReadAllLines(PathFile);
        string inputPath = paths[0];
        string outputPath = paths[1];

        using TextReader inputReader = new StreamReader(GetInputStream(inputPath));
        using TextWriter outputWriter = new StreamWriter(outputPath);

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

        string code = new StreamReader(GetInputStream(inputPath)).ReadToEnd();

        foreach (Error error in result.Errors)
        {
            Console.WriteLine(Errors.GenerateFullMessage(code, error));
            Console.WriteLine();
        }
    }

    private static Stream GetInputStream(string inputPath)
    {
        IEnumerable<string> files = Directory.GetFiles(inputPath, "*.hstr");

        List<Stream> streams = [];

        foreach (string hstrFile in files)
        {
            streams.Add(new FileStream(hstrFile, FileMode.Open, FileAccess.Read));
        }

        return new ConcatenatedStream(streams);
    }
}
