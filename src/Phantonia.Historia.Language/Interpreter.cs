using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;

namespace Phantonia.Historia.Language;

public sealed class Interpreter
{
    public Interpreter(string code)
    {
        inputReader = new StringReader(code);
    }

    public Interpreter(TextReader inputReader)
    {
        this.inputReader = inputReader;
    }

    private readonly TextReader inputReader;

    public InterpretationResult Interpret()
    {
        List<Error> errors = new();

        void HandleError(Error error)
        {
            errors.Add(error);
        }

        Lexer lexer = new(inputReader);
        lexer.ErrorFound += HandleError;
        ImmutableArray<Token> tokens = lexer.Lex();
        lexer.ErrorFound -= HandleError;

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
            return new InterpretationResult
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
            return new InterpretationResult
            {
                Errors = errors.ToImmutableArray(),
            };
        }

        Debug.Assert(flowAnalysisResult.IsValid);
        return new InterpretationResult
        {
            StateMachine = new InterpreterStateMachine(flowAnalysisResult.MainFlowGraph, flowAnalysisResult.SymbolTable),
        };
    }
}
