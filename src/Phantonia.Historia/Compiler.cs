using Phantonia.Historia.Language.Ast;
using System;
using System.Collections.Immutable;
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

    public string CompileToCSharpText()
    {
        Lexer lexer = new(HistoriaText);
        ImmutableArray<Token> tokens = lexer.Lex();

        Parser parser = new(tokens);
        parser.ErrorFound += HandleError;
        StoryNode story = parser.Parse();

        Binder binder = new(story);
        StoryNode boundStory = binder.Bind();

        throw new NotImplementedException();
    }

    private void HandleError(Error error)
    {
        string fullMessage = Errors.GenerateFullMessage(HistoriaText, error);
        errorOutput.WriteLine(fullMessage);
    }
}
