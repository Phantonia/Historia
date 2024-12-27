using Phantonia.Historia.Language.CodeGeneration;
using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;

namespace Phantonia.Historia.Language;

public sealed class Compiler
{
    public Compiler(string code, TextWriter outputWriter)
    {
        inputReader = new StringReader(code);
        this.outputWriter = outputWriter;
    }

    public Compiler(TextReader inputReader, TextWriter outputWriter)
    {
        this.inputReader = inputReader;
        this.outputWriter = outputWriter;
    }

    private readonly TextReader inputReader;
    private readonly TextWriter outputWriter;

    public CompilationResult Compile()
    {
        List<Error> errors = [];

        void HandleError(Error error)
        {
            errors.Add(error);
        }

        Stopwatch sw = Stopwatch.StartNew();
        Lexer lexer = new(inputReader);
        lexer.ErrorFound += HandleError;
        ImmutableArray<Token> tokens = lexer.Lex();
        lexer.ErrorFound -= HandleError;
        sw.Stop();
        Console.WriteLine($"Lexer took {sw.Elapsed.TotalSeconds} seconds.");

        sw.Restart();
        Parser parser = new(tokens);
        parser.ErrorFound += HandleError;
        StoryNode story = parser.Parse();
        parser.ErrorFound -= HandleError;
        sw.Stop();
        Console.WriteLine($"Parser took {sw.Elapsed.TotalSeconds} seconds.");

        sw.Restart();
        Binder binder = new(story);
        binder.ErrorFound += HandleError;
        BindingResult bindingResult = binder.Bind();
        binder.ErrorFound -= HandleError;
        sw.Stop();
        Console.WriteLine($"Binder took {sw.Elapsed.TotalSeconds} seconds.");

        if (!bindingResult.IsValid || errors.Count > 0)
        {
            return new CompilationResult
            {
                Errors = [.. errors],
            };
        }

        (StoryNode? boundStory, Settings? settings, SymbolTable? symbolTable) = bindingResult;

        Debug.Assert(boundStory is not null);
        Debug.Assert(settings is not null);
        Debug.Assert(symbolTable is not null);

        sw.Restart();
        FlowAnalyzer flowAnalyzer = new(boundStory, symbolTable);
        flowAnalyzer.ErrorFound += errors.Add;

        FlowAnalysisResult flowAnalysisResult = flowAnalyzer.PerformFlowAnalysis();
        sw.Stop();
        Console.WriteLine($"Flow analyzer took {sw.Elapsed.TotalSeconds}");

        if (errors.Count > 0)
        {
            return new CompilationResult
            {
                Errors = [.. errors],
            };
        }

        Debug.Assert(flowAnalysisResult.IsValid);

        sw.Restart();
        Emitter emitter = new(
            boundStory,
            settings,
            flowAnalysisResult.MainFlowGraph,
            flowAnalysisResult.SymbolTable,
            flowAnalysisResult.DefinitelyAssignedOutcomesAtCheckpoints,
            outputWriter);

        emitter.GenerateOutputCode();
        sw.Stop();
        Console.WriteLine($"Emitter took {sw.Elapsed.TotalSeconds} seconds.");

        return new CompilationResult();
    }
}
