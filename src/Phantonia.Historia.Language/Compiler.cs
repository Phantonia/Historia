using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis;
using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;

namespace Phantonia.Historia.Language;

public sealed class Compiler
{
    public Compiler(string historiaText)
    {
        HistoriaText = historiaText;
    }

    public string HistoriaText { get; }

    public CompilationResult CompileToCSharpText()
    {
        List<Error> errors = new();

        void HandleError(Error error)
        {
            Debug.WriteLine(Errors.GenerateFullMessage(HistoriaText, error));
            Debug.WriteLine("");

            errors.Add(error);
        }

        Lexer lexer = new(HistoriaText);
        ImmutableArray<Token> tokens = lexer.Lex();

        Parser parser = new(tokens);
        parser.ErrorFound += HandleError;
        StoryNode story = parser.Parse();
        parser.ErrorFound -= HandleError;

        Binder binder = new(story);
        binder.ErrorFound += HandleError;
        BindingResult result = binder.Bind();
        binder.ErrorFound -= HandleError;

        if (!result.IsValid || errors.Count > 0)
        {
            return new CompilationResult
            {
                Errors = errors.ToImmutableArray(),
            };
        }

        (StoryNode? boundStory, Settings? settings, SymbolTable? symbolTable) = result;

        Debug.Assert(boundStory is not null);
        Debug.Assert(settings is not null);
        Debug.Assert(symbolTable is not null);

        FlowAnalyzer flowAnalyzer = new(boundStory, symbolTable);
        flowAnalyzer.ErrorFound += errors.Add;

        if (errors.Count > 0)
        {
            return new CompilationResult
            {
                Errors = errors.ToImmutableArray(),
            };
        }

        FlowGraph mainGraph = flowAnalyzer.GenerateMainFlowGraph();

        Emitter emitter = new(boundStory, settings, mainGraph);

        string csharpText = emitter.GenerateCSharpText();

        return new CompilationResult
        {
            CSharpText = csharpText,
        };
    }
}
