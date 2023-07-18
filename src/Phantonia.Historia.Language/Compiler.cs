using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis;
using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;

namespace Phantonia.Historia.Language;

public sealed class Compiler
{
    public Compiler(string historiaText)
    {
        HistoriaText = historiaText;
        errorOutput = Console.Error;
    }

    public string HistoriaText { get; }

    private readonly TextWriter errorOutput;

    public string? CompileToCSharpText()
    {
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

        if (!result.IsValid)
        {
            throw new NotImplementedException("Invalid binding result");
        }

        (StoryNode? boundStory, SymbolTable? symbolTable) = result;

        Debug.Assert(boundStory is not null);
        Debug.Assert(symbolTable is not null);

        FlowAnalyzer flowAnalyzer = new(boundStory);
        FlowGraph mainGraph = flowAnalyzer.GenerateMainFlowGraph();

        Emitter emitter = new(mainGraph);

        // we need to not do this when we get any error (or only fatal errors?)
        return emitter.GenerateCSharpText();
    }

    private void HandleError(Error error)
    {
        string fullMessage = Errors.GenerateFullMessage(HistoriaText, error);
        errorOutput.WriteLine(fullMessage);
    }
}
