using Phantonia.Historia.Language;
using System.Diagnostics;

namespace Phantonia.Historia.Build;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Error: no Historia code file was provided");
            _ = Console.ReadKey();
            return 2;
        }

        CompilationResult result;

        try
        {
            using StreamReader streamReader = new(args[0]);

            Compiler compiler = new(streamReader);
            result = compiler.CompileToCSharpText();
        }
        catch (IOException)
        {
            Console.Error.WriteLine($"Something went wrong with loading the file {args[0]}");
            _ = Console.ReadKey();
            return 2;
        }

        if (result.Errors.Length > 0)
        {
            // i should probably rework how errors work

            using StreamReader streamReader = new(args[0]);
            string code = streamReader.ReadToEnd();

            foreach (Error error in result.Errors)
            {
                Console.Error.WriteLine(Errors.GenerateFullMessage(code, error));
            }

            _ = Console.ReadKey();

            return 1;
        }
        else
        {
            string? path = Path.GetDirectoryName(args[0]);
            Debug.Assert(path is not null);

            string outputPath = Path.Combine(path, "HistoriaStory.cs");

            using StreamWriter streamWriter = new(outputPath);

            // the emitter should probably immediately write there
            streamWriter.Write(result.CSharpText);

            Console.WriteLine($"Emitted {outputPath}");
            _ = Console.ReadKey();

            return 0;
        }
    }
}