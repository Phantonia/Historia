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
            Console.Write("Press any key to exit...");
            _ = Console.ReadKey();
            return 2;
        }

        CompilationResult result;

        try
        {
            using StreamReader streamReader = new(args[0]);

            string? path = Path.GetDirectoryName(args[0]);
            Debug.Assert(path is not null);

            string outputPath = Path.Combine(path, "HistoriaStory.cs");

            using StreamWriter streamWriter = new(outputPath);

            Compiler compiler = new(streamReader, streamWriter);

            result = compiler.Compile();
        }
        catch (IOException)
        {
            Console.Error.WriteLine($"Something went wrong with loading the file {args[0]}");
            Console.Write("Press any key to exit...");
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

            Console.Write("Press any key to exit...");
            _ = Console.ReadKey();

            return 1;
        }

        return 0;
    }
}