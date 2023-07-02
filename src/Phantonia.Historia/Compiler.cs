using Phantonia.Historia.Language.Ast;
using System;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language;

public sealed class Compiler
{
    public Compiler(string historiaText)
    {
        HistoriaText = historiaText;
    }

    public string HistoriaText { get; }

    public string CompileToCSharpText()
    {
        Lexer lexer = new(HistoriaText);

        ImmutableArray<Token> tokens = lexer.Lex();

        throw new NotImplementedException();
    }
}
