using Phantonia.Historia.Language.CodeGeneration;
using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Phantonia.Historia.Language;

public static class Compiler
{
    public static (CompilationResult result, string csharpCode) CompileString(string code)
    {
        List<Error> errors = [];
        using StringReader reader = new(code);

        Lexer lexer = new(reader);
        lexer.ErrorFound += errors.Add;
        ImmutableArray<Token> tokens = lexer.Lex(out IEnumerable<long> lineIndices);
        lexer.ErrorFound -= errors.Add;

        Parser parser = new(tokens, "");
        parser.ErrorFound += errors.Add;
        CompilationUnitNode unit = parser.Parse();
        parser.ErrorFound -= errors.Add;

        Debug.Assert(unit.Reconstruct() == code);

        StoryNode story = new()
        {
            CompilationUnits = [unit],
            Index = 0,
            Length = unit.Length,
            PrecedingTokens = [],
        };

        using StringWriter writer = new();

        LineIndexing lineIndexing = new(ImmutableDictionary<string, ImmutableArray<long>>.Empty.Add("", [.. lineIndices]));

        CompilationResult result = ProceedWithStory(story, writer, errors, lineIndexing);
        return (result, writer.ToString());
    }

    public static CompilationResult CompileFiles(string directory, IEnumerable<string> inputPaths, string outputPath)
    {
        List<Error> errors = [];

        List<CompilationUnitNode> compilationUnits = [];
        long previousLength = 0;

        Dictionary<string, ImmutableArray<long>> pathLines = [];

        foreach (string path in inputPaths)
        {
            string absolutePath = Path.Combine(directory, path);
            using StreamReader inputReader = new(absolutePath);

            Lexer lexer = new(inputReader, indexOffset: previousLength);
            lexer.ErrorFound += errors.Add;
            ImmutableArray<Token> tokens = lexer.Lex(out IEnumerable<long> lineIndices);
            lexer.ErrorFound -= errors.Add;

            pathLines[path] = [.. lineIndices];

            Parser parser = new(tokens, path);
            parser.ErrorFound += errors.Add;
            CompilationUnitNode unit = parser.Parse();
            parser.ErrorFound -= errors.Add;

            compilationUnits.Add(unit);
            previousLength += unit.Length;
        }

        StoryNode story = new()
        {
            CompilationUnits = [.. compilationUnits],
            Index = 0,
            Length = previousLength,
            PrecedingTokens = [],
        };

        LineIndexing lineIndexing = new(pathLines.ToImmutableDictionary());

        string absoluteOutputPath = Path.GetFullPath(Path.Combine(directory, outputPath));

        using FileStream outputStream = new(absoluteOutputPath, FileMode.Create, FileAccess.Write);
        using StreamWriter outputWriter = new(outputStream);

        return ProceedWithStory(story, outputWriter, errors, lineIndexing);
    }

    private static CompilationResult ProceedWithStory(StoryNode story, TextWriter outputWriter, List<Error> errors, LineIndexing lineIndexing)
    {
        ulong fingerprint = FingerprintCalculator.GetStoryFingerprint(story);

        Binder binder = new(story);
        binder.ErrorFound += errors.Add;
        BindingResult bindingResult = binder.Bind();
        binder.ErrorFound -= errors.Add;

        if (!bindingResult.IsValid || errors.Count > 0)
        {
            return new CompilationResult
            {
                Errors = [.. errors.OrderBy(e => e.Index)],
                LineIndexing = lineIndexing,
                Fingerprint = 0,
            };
        }

        (StoryNode? boundStory, Settings? settings, SymbolTable? symbolTable) = bindingResult;

        Debug.Assert(boundStory is not null);
        Debug.Assert(settings is not null);
        Debug.Assert(symbolTable is not null);

        FlowAnalyzer flowAnalyzer = new(boundStory, symbolTable);
        flowAnalyzer.ErrorFound += errors.Add;
        FlowAnalysisResult flowAnalysisResult = flowAnalyzer.PerformFlowAnalysis();
        flowAnalyzer.ErrorFound -= errors.Add;

        if (errors.Count > 0)
        {
            return new CompilationResult
            {
                Errors = [.. errors.OrderBy(e => e.Index)],
                LineIndexing = lineIndexing,
                Fingerprint = 0,
            };
        }

        Debug.Assert(flowAnalysisResult.IsValid);

        Emitter emitter = new(
            boundStory,
            settings,
            flowAnalysisResult.MainFlowGraph,
            flowAnalysisResult.SymbolTable,
            flowAnalysisResult.ChapterData,
            outputWriter);

        emitter.GenerateOutputCode();

        return new CompilationResult
        {
            LineIndexing = lineIndexing,
            Fingerprint = fingerprint,
        };
    }
}
