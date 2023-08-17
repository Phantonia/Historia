using Phantonia.Historia.Language.CodeGeneration;
using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis;
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
        List<Error> errors = new();

        void HandleError(Error error)
        {
            errors.Add(error);
        }

        Lexer lexer = new(inputReader);
        ImmutableArray<Token> tokens = lexer.Lex();

        Parser parser = new(tokens);
        parser.ErrorFound += HandleError;
        StoryNode story = parser.Parse();
        parser.ErrorFound -= HandleError;

        Binder binder = new(story);
        binder.ErrorFound += HandleError;
        BindingResult bindingResult = binder.Bind();
        binder.ErrorFound -= HandleError;

        if (!bindingResult.IsValid || errors.Count > 0)
        {
            return new CompilationResult
            {
                Errors = errors.ToImmutableArray(),
            };
        }

        (StoryNode? boundStory, Settings? settings, SymbolTable? symbolTable) = bindingResult;

        Debug.Assert(boundStory is not null);
        Debug.Assert(settings is not null);
        Debug.Assert(symbolTable is not null);

        FlowAnalyzer flowAnalyzer = new(boundStory, symbolTable);
        flowAnalyzer.ErrorFound += errors.Add;

        FlowAnalysisResult flowAnalysisResult = flowAnalyzer.PerformFlowAnalysis();

        if (errors.Count > 0)
        {
            return new CompilationResult
            {
                Errors = errors.ToImmutableArray(),
            };
        }

        Debug.Assert(flowAnalysisResult.IsValid);

        Emitter emitter = new(boundStory, settings, flowAnalysisResult.MainFlowGraph, flowAnalysisResult.SymbolTable, outputWriter);

        emitter.GenerateOutputCode();

        return new CompilationResult();
    }
}
